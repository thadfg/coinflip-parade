using Microsoft.EntityFrameworkCore;
using SharedLibrary.Models;
using ValuationService.Infrastructure;
using System.Text.Json;
using PersistenceService.Infrastructure;

namespace ValuationService.Service;

public class ValuationBackgroundWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ValuationBackgroundWorker> _logger;
    private readonly IMcpClientWrapper _mcpClient;

    public ValuationBackgroundWorker(
        IServiceProvider serviceProvider,
        ILogger<ValuationBackgroundWorker> logger,
        IMcpClientWrapper mcpClient)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _mcpClient = mcpClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ValuationBackgroundWorker starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessValuations(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during valuation processing.");
            }

            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }

    private async Task ProcessValuations(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ComicDbContext>();

        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        var recordsToUpdate = await dbContext.ComicRecords
            .Where(r => r.Value == null || r.LastUpdatedUtc == null || r.LastUpdatedUtc < cutoffDate)
            .Take(10) // Process in small batches
            .ToListAsync(stoppingToken);

        _logger.LogInformation("Found {Count} records to update.", recordsToUpdate.Count);

        foreach (var record in recordsToUpdate)
        {
            if (stoppingToken.IsCancellationRequested) break;

            _logger.LogInformation("Processing valuation for {FullTitle}", record.FullTitle);

            string prompt = $"I have {record.FullTitle} ({record.PublisherName}) from my database. Use Playwright to find the last 3 'Sold' prices on eBay for this book in Raw Mid-Grade condition. Return only the average numeric value.";

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
                }
                else
                {
                    _logger.LogWarning("Could not parse value for {FullTitle}. Response: {Response}", record.FullTitle, mcpResponse);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to research {FullTitle}", record.FullTitle);
            }
        }
    }
}
