using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PersistenceService.Application.Interfaces;
using PersistenceService.Domain.Entities;
using PersistenceService.Infrastructure;
using PersistenceService.Infrastructure.Repositories;
using SharedLibrary.Facet;
using SharedLibrary.Models;
using System.Text.Json;
using Xunit;

namespace PersistenceService.Tests.Repositories;

public class EventRepositoryTests
{
    [Fact]
    public async Task SaveAsync_RetriesOnTransientFailure()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EventDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new EventDbContext(options);
        var logger = new Mock<ILogger<EventRepository>>();
        var repo = new EventRepository(context, logger.Object);

        var entity = new EventEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = Guid.NewGuid(),
            EventType = "Test",
            EventData = "{}",
            OccurredAt = DateTimeOffset.UtcNow
        };

        var callCount = 0;
        context.SavingChanges += (_, _) =>
        {
            callCount++;
            if (callCount == 1)
            {
                throw new DbUpdateException("Simulated transient failure");
            }
        };

        // Act
        await repo.SaveAsync(entity, CancellationToken.None);

        // Assert
        Assert.Equal(2, callCount); // First attempt throws, second succeeds
    }


    [Fact]
    public async Task SaveAsync_LogsWarningOnRetry()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EventDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new EventDbContext(options);
        var logger = new Mock<ILogger<EventRepository>>();

        var entity = new EventEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = Guid.NewGuid(),
            EventType = "Test",
            EventData = "{}",
            OccurredAt = DateTimeOffset.UtcNow
        };

        var repo = new EventRepository(context, logger.Object);

        // Simulate transient failure by overriding SaveChangesAsync
        var callCount = 0;
        context.SavingChanges += (_, _) =>
        {
            callCount++;
            if (callCount == 1)
            {
                throw new DbUpdateException("Simulated transient failure");
            }
        };

        // Act
        await repo.SaveAsync(entity, CancellationToken.None);

        // Assert
        logger.Verify(
    x => x.Log(
        LogLevel.Warning,
        It.IsAny<EventId>(),
        It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Attempt 1 failed")),
        It.IsAny<Exception>(),
        It.IsAny<Func<It.IsAnyType, Exception, string>>()),
    Times.Once);

    }


    [Fact]
    public async Task SaveAsync_SavesCorrectEntity()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EventDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // isolate per test
            .Options;

        using var context = new EventDbContext(options);
        var logger = new Mock<ILogger<EventRepository>>();
        var repo = new EventRepository(context, logger.Object);

        var entity = new EventEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = Guid.NewGuid(),
            EventType = "Test",
            EventData = "{\"title\":\"example\"}",
            OccurredAt = DateTimeOffset.UtcNow
        };

        // Act
        await repo.SaveAsync(entity, CancellationToken.None);

        // Assert
        var saved = await context.Events.FindAsync(entity.Id);
        Assert.NotNull(saved);
        Assert.Equal(entity.EventType, saved.EventType);
        Assert.Equal(entity.EventData, saved.EventData);
    }

    [Fact]
    public async Task Listener_FlushesFinalBatch_OnShutdown()
    {
        // Arrange
        var mockRepo = new Mock<IEventRepository>();
        var mockLogger = new Mock<ILogger<KafkaComicListener>>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Kafka:BootstrapServers", "localhost:9092" },
                { "Kafka:GroupId", "test-group" },
                { "Kafka:Topic", "test-topic" },
                { "Kafka:BatchSize", "5" }
            })
            .Build();

        var mockConsumer = new Mock<IConsumer<Ignore, string>>();
        var cancellationSource = new CancellationTokenSource();

        var envelope = new KafkaEnvelope<ComicCsvRecordDto>
        {
            ImportId = Guid.NewGuid().ToString(),
            Payload = new ComicCsvRecordDto { SeriesName = "X-Men", FullTitle = "1", ReleaseDate = "2024-12-16" }
        };

        var message = new ConsumeResult<Ignore, string>
        {
            Message = new Message<Ignore, string>
            {
                Value = JsonSerializer.Serialize(envelope)
            }
        };

        mockConsumer.SetupSequence(c => c.Consume(It.IsAny<CancellationToken>()))
            .Returns(message)
            .Throws(new OperationCanceledException());

        mockConsumer.Setup(c => c.Close());
        mockConsumer.Setup(c => c.Dispose());

        IEnumerable<EventEntity>? capturedBatch = null;
        mockRepo.Setup(r => r.SaveBatchAsync(It.IsAny<IEnumerable<EventEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<EventEntity>, CancellationToken>((batch, _) =>
            {
                capturedBatch = batch.ToList();
            })
            .Returns(Task.CompletedTask);

        var listener = new KafkaComicListener(mockLogger.Object, config, mockRepo.Object, mockConsumer.Object);

        var listenerTask = listener.StartAsync(cancellationSource.Token);
        await Task.Delay(100);
        cancellationSource.Cancel();
        await listenerTask;

        // Assert
        Assert.NotNull(capturedBatch);
        Assert.Equal(1, capturedBatch.Count());
        mockConsumer.Verify(c => c.Close(), Times.Once);
        mockConsumer.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public async Task SaveBatchAsync_PersistsAllEntities()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<EventDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new EventDbContext(options);
        var logger = new Mock<ILogger<EventRepository>>();
        var repo = new EventRepository(context, logger.Object);

        var entities = Enumerable.Range(0, 5).Select(i => new EventEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = Guid.NewGuid(),
            EventType = "Test",
            EventData = "{}",
            OccurredAt = DateTimeOffset.UtcNow
        }).ToList();

        // Act
        await repo.SaveBatchAsync(entities, CancellationToken.None);

        // Assert
        var savedEntities = context.Events.ToList();
        Assert.Equal(5, savedEntities.Count);
        foreach (var entity in entities)
        {
            Assert.Contains(savedEntities, e => e.Id == entity.Id);
        }
    }

}
