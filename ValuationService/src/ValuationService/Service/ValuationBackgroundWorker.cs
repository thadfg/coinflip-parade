using Microsoft.EntityFrameworkCore;
using SharedLibrary.Models;
using ValuationService.Infrastructure;
using System.Text.Json;
using PersistenceService.Infrastructure;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ValuationService.Service;

public class ValuationBackgroundWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ValuationBackgroundWorker> _logger;
    private readonly IMcpClientWrapper _mcpClient;
    private readonly ValuationControlService _controlService;

    private static readonly ActivitySource ActivitySource = new ActivitySource("ValuationService");
    private readonly Counter<long> _processedCount;
    private readonly Counter<long> _failureCount;

    public ValuationBackgroundWorker(
        IServiceProvider serviceProvider,
        ILogger<ValuationBackgroundWorker> logger,
        IMcpClientWrapper mcpClient,
        ValuationControlService controlService,
        IMeterFactory meterFactory)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _mcpClient = mcpClient;
        _controlService = controlService;

        var meter = meterFactory.Create("ValuationService");
        _processedCount = meter.CreateCounter<long>("valuation_processed_count");
        _failureCount = meter.CreateCounter<long>("valuation_failure_count");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ValuationBackgroundWorker starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_controlService.IsRunning)
            {
                try
                {
                    await ProcessValuations(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during valuation processing.");
                }
            }
            else
            {
                _logger.LogDebug("ValuationBackgroundWorker is paused.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessValuations(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ComicDbContext>();

        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        var recordsToUpdate = await dbContext.ComicRecords
            .Where(r => (r.Value == null && (r.LastUpdatedUtc == null || r.LastUpdatedUtc < cutoffDate)) || (r.LastUpdatedUtc < cutoffDate))
            .OrderBy(r => r.LastUpdatedUtc) // Process oldest first
            .Take(10) // Process in small batches
            .ToListAsync(stoppingToken);

        _logger.LogInformation("Found {Count} records to update.", recordsToUpdate.Count);

        foreach (var record in recordsToUpdate)
        {
            if (stoppingToken.IsCancellationRequested) break;

            _logger.LogInformation("Processing valuation for {FullTitle}", record.FullTitle);

            using var activity = ActivitySource.StartActivity("ebay_valuation_lookup");
            activity?.SetTag("comic.title", record.FullTitle);

            string prompt = $"I have {record.FullTitle} ({record.PublisherName}) from my database. Use Playwright to find the price on comicbookrealm.com for this book. Return only the numeric value.";

            try
            {
                string mcpResponse = await _mcpClient.ExecuteResearch(prompt);
                decimal? value = ValuationResponseParser.ParseValueFromMcpResponse(mcpResponse);

                if (value.HasValue)
                {
                    record.Value = value;
                    record.LastUpdatedUtc = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Updated {FullTitle} with value {Value}", record.FullTitle, value);
                    _processedCount.Add(1);
                    activity?.SetStatus(ActivityStatusCode.Ok);
                }
                else
                {
                    _logger.LogWarning("Could not parse value for {FullTitle}. Response: {Response}", record.FullTitle, mcpResponse);
                    // Mark as updated even if it failed so we don't try it again in a loop
                    record.LastUpdatedUtc = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync(stoppingToken);
                    _failureCount.Add(1);
                    activity?.SetStatus(ActivityStatusCode.Error, "Could not parse value");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to research {FullTitle}", record.FullTitle);
                // Mark as updated even if it failed so we don't try it again in a loop
                record.LastUpdatedUtc = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(stoppingToken);
                _failureCount.Add(1);
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            }
        }
    }
}
