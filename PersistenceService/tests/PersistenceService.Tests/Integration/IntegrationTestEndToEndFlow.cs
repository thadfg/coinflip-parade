using Microsoft.Extensions.Logging;
using Moq;
using PersistenceService.Application.Mappers;
using PersistenceService.Infrastructure.Repositories;
using PersistenceService.Tests.TestContexts;
using PersistenceService.Tests.TestDataGenerators;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistenceService.Tests.Integration;

public class IntegrationTestEndToEndFlow
{
    [Fact]
    public async Task EndToEndFlow_PersistsEventAndComic_AndEnforcesIdempotency()
    {
        // Arrange
        var comicDb = InMemoryComicDbContext.CreateComicDbContext();
        var eventDb = InMemoryComicDbContext.CreateEventDbContext();

        var comicLogger = new Mock<ILogger<ComicCollectionRepository>>();
        var eventLogger = new Mock<ILogger<EventRepository>>();

        var comicRepo = new ComicCollectionRepository(comicDb, comicLogger.Object);
        var eventRepo = new EventRepository(eventDb, eventLogger.Object);

        var envelope = ComicEnvelopeBuilder.Build(Guid.NewGuid());
        var comic = ComicRecordMapper.ToEntity(envelope);
        var eventEntity = EventEntityMapper.FromPayload(
            envelope.Payload,
            Guid.Parse(envelope.ImportId),
            "ComicCsvRecordReceived"
        );

        // Act
        await comicRepo.UpsertBatchAsync(new[] { (comic, Guid.Parse(envelope.ImportId)) }, CancellationToken.None);
        await eventRepo.SaveAsync(eventEntity, CancellationToken.None);

        // Assert
        var persistedComic = await comicDb.ComicRecords.FindAsync(comic.Id);
        var processedEvent = await comicDb.ProcessedEvents
            .SingleOrDefaultAsync(p => p.EventId == Guid.Parse(envelope.ImportId));
        var persistedEvent = await eventDb.Events.FindAsync(eventEntity.Id);

        Assert.NotNull(persistedComic);
        Assert.Equal("Hellboy: Seed of Destruction", persistedComic.FullTitle);
        Assert.NotNull(processedEvent);
        Assert.NotNull(persistedEvent);

        // Act again with same event (idempotency check)
        await comicRepo.UpsertBatchAsync(new[] { (comic, Guid.Parse(envelope.ImportId)) }, CancellationToken.None);

        // Assert no duplicate comic
        var comicCount = await comicDb.ComicRecords.CountAsync();
        var processedCount = await comicDb.ProcessedEvents.CountAsync();
        Assert.Equal(1, comicCount);
        Assert.Equal(1, processedCount);
    }

}
