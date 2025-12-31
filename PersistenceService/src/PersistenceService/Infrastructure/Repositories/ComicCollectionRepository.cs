using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PersistenceService.Application.Interfaces;
using PersistenceService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

    public async Task UpsertBatchAsync(IEnumerable<(ComicRecordEntity Comic, System.Guid EventId)> batch, CancellationToken cancellationToken)
    {
        var items = batch.ToList();

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                // ✅ Step 1: Filter out already processed events
                var processedIds = await _dbContext.ProcessedEvents
                    .Where(e => items.Select(i => i.EventId).Contains(e.EventId))
                    .Select(e => e.EventId)
                    .ToListAsync(cancellationToken);

                var unprocessedItems = items
                    .Where(i => !processedIds.Contains(i.EventId))
                    .ToList();

                if (!unprocessedItems.Any())
                {
                    _logger.LogInformation("All events in batch already processed. Skipping.");
                    return;
                }

                // ✅ Step 2: Fetch existing comics by ID
                var comicIds = unprocessedItems.Select(i => i.Comic.Id).Distinct().ToList();
                var existingComics = await _dbContext.ComicRecords
                    .Where(c => comicIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id, cancellationToken);

                // ✅ Step 3: Upsert logic
                foreach (var (comic, eventId) in unprocessedItems)
                {
                    if (existingComics.TryGetValue(comic.Id, out var existing))
                    {
                        existing.PublisherName = comic.PublisherName;
                        existing.SeriesName = comic.SeriesName;
                        existing.FullTitle = comic.FullTitle;
                        existing.ReleaseDate = comic.ReleaseDate;
                        existing.InCollection = comic.InCollection;
                        existing.Value = comic.Value;
                        existing.CoverArtPath = comic.CoverArtPath;
                        existing.LastUpdatedUtc = DateTime.UtcNow;

                        _logger.LogInformation("Updated ComicRecord {Id}", comic.Id);
                    }
                    else
                    {
                        comic.LastUpdatedUtc = DateTime.UtcNow;
                        _dbContext.ComicRecords.Add(comic);
                        _logger.LogInformation("Inserted new ComicRecord {Id}", comic.Id);
                    }

                    _dbContext.ProcessedEvents.Add(new ProcessedEvent
                    {
                        EventId = eventId,
                        ProcessedAtUtc = DateTime.UtcNow
                    });
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Batch upsert completed successfully on attempt {Attempt}", attempt);
                return;
            }
            catch (DbUpdateException ex) when (attempt < MaxRetries)
            {
                _logger.LogWarning(ex, "Attempt {Attempt} failed. Retrying...", attempt);
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

