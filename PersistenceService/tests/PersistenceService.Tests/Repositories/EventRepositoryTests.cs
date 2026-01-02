using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PersistenceService.Application.Interfaces;
using PersistenceService.Domain.Entities;
using PersistenceService.Infrastructure;
using PersistenceService.Infrastructure.Database;
using PersistenceService.Infrastructure.Kafka;
using PersistenceService.Infrastructure.Repositories;
using PersistenceService.Tests.KafkaListener;
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
        var options = new DbContextOptionsBuilder<EventDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
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
                throw new DbUpdateException("Simulated transient failure");
        };

        await repo.SaveAsync(entity, CancellationToken.None);

        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task SaveAsync_LogsWarningOnRetry()
    {
        var options = new DbContextOptionsBuilder<EventDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
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

        var callCount = 0;
        context.SavingChanges += (_, _) =>
        {
            callCount++;
            if (callCount == 1)
                throw new DbUpdateException("Simulated transient failure");
        };

        await repo.SaveAsync(entity, CancellationToken.None);

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
        var options = new DbContextOptionsBuilder<EventDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
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

        await repo.SaveAsync(entity, CancellationToken.None);

        var saved = await context.Events.FindAsync(entity.Id);
        Assert.NotNull(saved);
        Assert.Equal(entity.EventType, saved.EventType);
        Assert.Equal(entity.EventData, saved.EventData);
    }


    [Fact]
    public async Task Listener_FlushesFinalBatch_OnShutdown()
    {
        // Arrange
        var mockEventRepo = new Mock<IEventRepository>();
        var mockComicRepo = new Mock<IComicCollectionRepository>();
        var mockKafkaLogHelper = new Mock<IKafkaLogHelper>();
        var mockLogger = new Mock<ILogger<KafkaComicListener>>();
        var mockDbReadyChecker = new Mock<IDatabaseReadyChecker>();

        mockDbReadyChecker
            .Setup(x => x.IsReadyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                    { "Kafka:BootstrapServers", "localhost:9092" },
                    { "Kafka:GroupId", "test-group" },
                    { "Kafka:Topic", "test-topic" },
                    { "Kafka:BatchSize", "5" },
                    { "KafkaListener:FlushIntervalSeconds", "5" },
                    { "KafkaListener:ConsumeTimeoutMs", "10" }
            })
            .Build();

        // Create a valid envelope
        var envelope = new KafkaEnvelope<ComicCsvRecordDto>
        {
            ImportId = Guid.NewGuid().ToString(),
            Payload = new ComicCsvRecordDto
            {
                SeriesName = "X-Men",
                FullTitle = "1",
                ReleaseDate = "2024-12-16"
            }
        };

        var message = new ConsumeResult<Ignore, string>
        {
            Message = new Message<Ignore, string>
            {
                Value = JsonSerializer.Serialize(envelope)
            }
        };
        
        var mockConsumer = new Mock<IConsumer<Ignore, string>>();

        mockConsumer
            .SetupSequence(c => c.Consume(It.IsAny<CancellationToken>()))
            .Returns(message) // first call returns a valid message
            .Throws(new OperationCanceledException()); // second call ends the loop


        mockConsumer.Setup(c => c.Close());
        mockConsumer.Setup(c => c.Dispose());

        IEnumerable<EventEntity>? capturedEvents = null;

        mockEventRepo
            .Setup(r => r.SaveBatchAsync(It.IsAny<IEnumerable<EventEntity>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<EventEntity>, CancellationToken>((batch, _) =>
            {
                capturedEvents = batch.ToList();
            })
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(mockEventRepo.Object);
        services.AddSingleton(mockComicRepo.Object);
        var provider = services.BuildServiceProvider();

        var listener = new MockKafkaComicListener(
            mockLogger.Object,
            config,
            mockKafkaLogHelper.Object,
            mockDbReadyChecker.Object,
            provider,
            mockConsumer.Object
        );

        // Act
        await listener.RunConsumeLoopAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(capturedEvents);
        Assert.Single(capturedEvents);

        mockConsumer.Verify(c => c.Close(), Times.Once);
        mockConsumer.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public async Task SaveBatchAsync_PersistsAllEntities()
    {
        var options = new DbContextOptionsBuilder<EventDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
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

        await repo.SaveBatchAsync(entities, CancellationToken.None);

        var savedEntities = context.Events.ToList();
        Assert.Equal(5, savedEntities.Count);
        foreach (var entity in entities)
            Assert.Contains(savedEntities, e => e.Id == entity.Id);
    }
}
