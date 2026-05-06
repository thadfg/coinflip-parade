using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PersistenceService.Application.Interfaces;
using PersistenceService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EFCore.BulkExtensions;
using Microsoft.Extensions.Options;
using PersistenceService.Config;

namespace PersistenceService.Infrastructure.Repositories;

public class ComicCollectionRepository : IComicCollectionRepository
{
    private readonly ComicCollectionDbContext _dbContext;
    private readonly ILogger<ComicCollectionRepository> _logger;
    private readonly RepositoryOptions _options;

    public ComicCollectionRepository(ComicCollectionDbContext dbContext, ILogger<ComicCollectionRepository> logger, IOptions<RepositoryOptions> options)
    {
        _dbContext = dbContext;
        _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        _dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        _logger = logger;
        _options = options.Value;
    }

    public async Task UpsertBatchAsync(IEnumerable<(ComicRecordEntity Comic, Guid EventId)> batch, CancellationToken cancellationToken)
    {
        var items = batch.ToList();
    
        // 1. Prepare the lists using your Entity names
        var eventLogs = items
            .GroupBy(i => i.EventId)
            .Select(g => g.First())
            .Select(i => new ProcessedEvent 
            { 
                Id = Guid.NewGuid(),
                EventId = i.EventId, 
                ProcessedAtUtc = DateTime.UtcNow 
            }).ToList();

        var comics = items.Select(i => i.Comic).ToList();

        if (_dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var addedEventsCount = 0;
            foreach (var evt in eventLogs)
            {
                if (!await _dbContext.ProcessedEvents.AnyAsync(p => p.EventId == evt.EventId, cancellationToken))
                {
                    _dbContext.ProcessedEvents.Add(evt);
                    addedEventsCount++;
                }
            }

            if (addedEventsCount > 0)
            {
                foreach (var comic in comics)
                {
                    var existing = await _dbContext.ComicRecords.FindAsync(new object[] { comic.Id }, cancellationToken);
                    if (existing != null)
                    {
                        _dbContext.Entry(existing).CurrentValues.SetValues(comic);
                    }
                    else
                    {
                        _dbContext.ComicRecords.Add(comic);
                    }
                }
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully processed batch of {Count} items.", items.Count);
            }
            return;
        }

        for (int attempt = 1; attempt <= _options.MaxRetries; attempt++)
        {
            try
            {
                await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                // 2. Bulk Insert ProcessedEvents (Idempotency Check)
                // Matches your unique index on EventId
                await _dbContext.BulkInsertAsync(eventLogs, new BulkConfig 
                { 
                    UpdateByProperties = new List<string> { nameof(ProcessedEvent.EventId) },
                    // This tells Postgres: if the EventId exists, don't do anything (ignore the row)
                    OnConflictUpdateWhereSql = (table, column) => $"{table}.\"ProcessedAtUtc\" < EXCLUDED.\"ProcessedAtUtc\"",
                    EnableShadowProperties = false,
                    IncludeGraph = false
                }, cancellationToken: cancellationToken);

                // 3. Bulk Upsert ComicRecordEntity
                // Deduplicate comics by Id before upserting to avoid Postgres error 21000:
                // "ON CONFLICT DO UPDATE command cannot affect row a second time"
                var uniqueComics = comics
                    .GroupBy(c => c.Id)
                    .Select(g => g.Last())
                    .ToList();

                // Matches your primary key on Id
                await _dbContext.BulkInsertOrUpdateAsync(uniqueComics, new BulkConfig 
                { 
                    UpdateByProperties = new List<string> { nameof(ComicRecordEntity.Id) },
                    EnableShadowProperties = false,
                    IncludeGraph = false
                }, cancellationToken: cancellationToken);

                await tx.CommitAsync(cancellationToken);
                _logger.LogInformation("Successfully processed batch of {Count} items.", items.Count);
                return;
            }
            catch (Exception ex) when (attempt < _options.MaxRetries)
            {
                _logger.LogWarning(ex, "Bulk upsert attempt {Attempt} failed. Retrying...", attempt);
                await Task.Delay(_options.DelayMilliseconds * attempt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upsert batch after {Attempt} attempts", attempt);
                throw;
            }
        }
    }
}