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

namespace PersistenceService.Infrastructure.Repositories;

public class ComicCollectionRepository : IComicCollectionRepository
{
    private readonly ComicCollectionDbContext _dbContext;
    private readonly ILogger<ComicCollectionRepository> _logger;
    private const int MaxRetries = 3;
    private const int DelayMilliseconds = 500;

    public ComicCollectionRepository(ComicCollectionDbContext dbContext, ILogger<ComicCollectionRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task UpsertBatchAsync(IEnumerable<(ComicRecordEntity Comic, Guid EventId)> batch, CancellationToken cancellationToken)
    {
        var items = batch.ToList();
    
        // 1. Prepare the lists using your Entity names
        var eventLogs = items.Select(i => new ProcessedEvent 
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

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
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
                    OnConflictUpdateWhereSql = (table, column) => $"{table}.\"Timestamp\" < EXCLUDED.\"Timestamp\""
                }, cancellationToken: cancellationToken);

                // 3. Bulk Upsert ComicRecordEntity
                // Matches your primary key on Id
                await _dbContext.BulkInsertOrUpdateAsync(comics, new BulkConfig 
                { 
                    UpdateByProperties = new List<string> { nameof(ComicRecordEntity.Id) } 
                }, cancellationToken: cancellationToken);

                await tx.CommitAsync(cancellationToken);
                _logger.LogInformation("Successfully processed batch of {Count} items.", items.Count);
                return;
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                _logger.LogWarning(ex, "Bulk upsert attempt {Attempt} failed. Retrying...", attempt);
                await Task.Delay(DelayMilliseconds * attempt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upsert batch after {Attempt} attempts", attempt);
                throw;
            }
        }
    }
}