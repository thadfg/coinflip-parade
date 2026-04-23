using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PersistenceService.Domain.Entities;
using PersistenceService.Infrastructure;
using PersistenceService.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistenceService.Tests.Repositories;

public class ComicCollectionRepositoryTests
{
    private ComicCollectionDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ComicCollectionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ComicCollectionDbContext(options);
    }

    private ComicRecordEntity CreateComic(Guid id)
    {
        return new ComicRecordEntity
        {
            Id = id,
            Publisher = "Marvel",
            Series = "Spider-Man",
            Issue = "1",
            FullTitle = "Amazing Spider-Man #1",
            ReleaseDate = new DateTime(1963, 3, 1),
            Format = "Comic",
            Barcode = "1234567890",
            ImportedAt = DateTime.UtcNow,
            KeyStatus = "New"
        };
    }

    [Fact]
    public async Task UpsertBatchAsync_InsertsNewComic_WhenNotExists()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var logger = new Mock<ILogger<ComicCollectionRepository>>();
        var repo = new ComicCollectionRepository(dbContext, logger.Object);

        var comic = CreateComic(Guid.NewGuid());
        var eventId = Guid.NewGuid();

        // Act
        await repo.UpsertBatchAsync(new[] { (comic, eventId) }, CancellationToken.None);

        // Assert
        var persisted = await dbContext.ComicRecords.FindAsync(comic.Id);
        Assert.NotNull(persisted);
        Assert.Equal("Marvel", persisted.Publisher);

        var processed = await dbContext.ProcessedEvents
            .SingleOrDefaultAsync(p => p.EventId == eventId);
        Assert.NotNull(processed);
    }

    [Fact]
    public async Task UpsertBatchAsync_UpdatesComic_WhenExists()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var logger = new Mock<ILogger<ComicCollectionRepository>>();
        var repo = new ComicCollectionRepository(dbContext, logger.Object);

        var comicId = Guid.NewGuid();
        var original = CreateComic(comicId);
        dbContext.ComicRecords.Add(original);
        await dbContext.SaveChangesAsync();

        var updated = CreateComic(comicId);
        updated.Publisher = "DC Comics";
        var eventId = Guid.NewGuid();

        // Act
        await repo.UpsertBatchAsync(new[] { (updated, eventId) }, CancellationToken.None);

        // Assert
        var persisted = await dbContext.ComicRecords.FindAsync(comicId);
        Assert.Equal("DC Comics", persisted.Publisher);
    }

    [Fact]
    public async Task UpsertBatchAsync_SkipsAlreadyProcessedEvent()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var logger = new Mock<ILogger<ComicCollectionRepository>>();
        var repo = new ComicCollectionRepository(dbContext, logger.Object);

        var comic = CreateComic(Guid.NewGuid());
        var eventId = Guid.NewGuid();

        dbContext.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = eventId,
            ProcessedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        // Act
        await repo.UpsertBatchAsync(new[] { (comic, eventId) }, CancellationToken.None);

        // Assert
        var count = await dbContext.ComicRecords.CountAsync();
        Assert.Equal(0, count); // Should not insert
    }

    [Fact]
    public async Task UpsertBatchAsync_LogsOutcome()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var logger = new Mock<ILogger<ComicCollectionRepository>>();
        var repo = new ComicCollectionRepository(dbContext, logger.Object);

        var comic = CreateComic(Guid.NewGuid());
        var eventId = Guid.NewGuid();

        // Act
        await repo.UpsertBatchAsync(new[] { (comic, eventId) }, CancellationToken.None);

        // Assert
        logger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully processed batch")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

