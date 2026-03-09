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

    public async Task UpsertBatchAsync(IEnumerable<(ComicRecordEntity Comic, Guid EventId)> batch, CancellationToken cancellationToken)
    {
        var items = batch.ToList();

        // De-dupe within the batch to avoid double-processing the same comic id in one flush
        items = items
            .GroupBy(x => x.Comic.Id)
            .Select(g => g.First())
            .ToList();

        // Optional optimization: pre-load processed event ids in one query
        var eventIds = items.Select(i => i.EventId).Distinct().ToList();

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                // Best-effort filter to reduce work (NOT relied on for correctness)
                var alreadyProcessed = await _dbContext.ProcessedEvents
                    .Where(e => eventIds.Contains(e.EventId))
                    .Select(e => e.EventId)
                    .ToListAsync(cancellationToken);

                var unprocessedItems = items
                    .Where(i => !alreadyProcessed.Contains(i.EventId))
                    .ToList();

                foreach (var (comic, eventId) in unprocessedItems)
                {
                    // Idempotency gate (correctness): eventid unique constraint decides
                    var processedEventRowId = Guid.NewGuid();
                    var insertedProcessedEvent = await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                        INSERT INTO processedevents (id, eventid, processedatutc)
                        VALUES ({processedEventRowId}, {eventId}, {DateTime.UtcNow})
                        ON CONFLICT (eventid) DO NOTHING;
                    ", cancellationToken);

                    if (insertedProcessedEvent == 0)
                        continue;

                    // Comic upsert by deterministic id
                    await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                        INSERT INTO comiccollection
                            (id, publishername, seriesname, fulltitle, releasedate, incollection, value, coverartpath, importedat, lastupdatedutc)
                        VALUES
                            ({comic.Id}, {comic.PublisherName}, {comic.SeriesName}, {comic.FullTitle}, {comic.ReleaseDate},
                             {comic.InCollection}, {comic.Value}, {comic.CoverArtPath}, {comic.ImportedAt}, {DateTime.UtcNow})
                        ON CONFLICT (id) DO UPDATE SET
                            publishername   = EXCLUDED.publishername,
                            seriesname      = EXCLUDED.seriesname,
                            fulltitle       = EXCLUDED.fulltitle,
                            releasedate     = EXCLUDED.releasedate,
                            incollection    = EXCLUDED.incollection,
                            value           = EXCLUDED.value,
                            coverartpath    = EXCLUDED.coverartpath,
                            lastupdatedutc  = EXCLUDED.lastupdatedutc;
                    ", cancellationToken);
                }

                await tx.CommitAsync(cancellationToken);
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