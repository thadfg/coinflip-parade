using Confluent.Kafka;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PersistenceService.Application.Interfaces;
using PersistenceService.Domain.Entities;
using PersistenceService.Infrastructure;
using PersistenceService.Infrastructure.Repositories;
using SharedLibrary.Facet;
using SharedLibrary.Models;
using System.Text.Json;
using NUnitAssert = NUnit.Framework.Assert;


namespace PersistenceService.Tests.Repositories;

[TestFixture]
public class EventRepositoryTests
{
    [Test]
    public async Task SaveAsync_RetriesOnTransientFailure()
    {
        // Arrange
        var mockContext = new Mock<EventDbContext>();
        var mockSet = new Mock<DbSet<EventEntity>>();
        var logger = new Mock<ILogger<EventRepository>>();

        var entity = new EventEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = Guid.NewGuid(),
            EventType = "Test",
            EventData = "{}",
            OccurredAt = DateTimeOffset.UtcNow
        };

        mockContext.SetupSequence(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Throws(new DbUpdateException())
            .ReturnsAsync(1);

        mockContext.Setup(x => x.Events).Returns(mockSet.Object);

        var repo = new EventRepository(mockContext.Object, logger.Object);

        // Act
        await repo.SaveAsync(entity, CancellationToken.None);

        // Assert
        mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Test]
    public async Task SaveAsync_LogsWarningOnRetry()
    {
        // Arrange
        var mockContext = new Mock<EventDbContext>();
        var mockSet = new Mock<DbSet<EventEntity>>();
        var logger = new Mock<ILogger<EventRepository>>();

        var entity = new EventEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = Guid.NewGuid(),
            EventType = "Test",
            EventData = "{}",
            OccurredAt = DateTimeOffset.UtcNow
        };

        mockContext.SetupSequence(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Throws(new DbUpdateException())
            .ReturnsAsync(1);

        mockContext.Setup(x => x.Events).Returns(mockSet.Object);

        var repo = new EventRepository(mockContext.Object, logger.Object);

        // Act
        await repo.SaveAsync(entity, CancellationToken.None);

        // Assert
        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Transient failure")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task SaveAsync_SavesCorrectEntity()
    {
        // Arrange
        var mockContext = new Mock<EventDbContext>();
        var mockSet = new Mock<DbSet<EventEntity>>();
        var logger = new Mock<ILogger<EventRepository>>();

        var entity = new EventEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = Guid.NewGuid(),
            EventType = "Test",
            EventData = "{\"title\":\"example\"}",
            OccurredAt = DateTimeOffset.UtcNow
        };

        mockSet.Setup(x => x.Add(It.IsAny<EventEntity>())).Verifiable();
        mockContext.Setup(x => x.Events).Returns(mockSet.Object);
        mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var repo = new EventRepository(mockContext.Object, logger.Object);

        // Act
        await repo.SaveAsync(entity, CancellationToken.None);

        // Assert
        mockSet.Verify(x => x.Add(It.Is<EventEntity>(e =>
            e.Id == entity.Id &&
            e.AggregateId == entity.AggregateId &&
            e.EventType == entity.EventType &&
            e.EventData == entity.EventData)), Times.Once);
    }

    [Fact]
    public async Task Listener_FlushesFinalBatch_OnShutdown()
    {
        // Arrange
        var mockRepo = new Mock<IEventRepository>();
        var mockLogger = new Mock<ILogger<KafkaComicListener>>();
        mockLogger.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()))
                .Callback((LogLevel level, EventId id, object state, Exception ex, object formatter) =>
                {
                    Console.WriteLine($"[{level}] {state}");
                });
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

        // Simulate one message then cancel
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
                capturedBatch = batch.ToList(); // Materialize immediately
                Console.WriteLine($"Captured batch count: {capturedBatch.Count()}");
                foreach (var e in capturedBatch)
                {
                    Console.WriteLine($"EventType: {e.EventType}, EventData: {e.EventData}");
                }
            })
            .Returns(Task.CompletedTask);

        // Inject the mock consumer via a testable subclass
        var listener = new KafkaComicListener(mockLogger.Object, config, mockRepo.Object, mockConsumer.Object);

        var listenerTask = listener.StartAsync(cancellationSource.Token);
        await Task.Delay(100); // Let the listener consume the message
        cancellationSource.Cancel();        

        

        // Act
        await listenerTask;

        // Assert
        //mockRepo.Verify(r => r.SaveBatchAsync(It.Is<List<EventEntity>>(b => b.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
        NUnitAssert.That(capturedBatch, Is.Not.Null);
        NUnitAssert.That(1, Is.EqualTo(capturedBatch.Count()));
        mockConsumer.Verify(c => c.Close(), Times.Once);
        mockConsumer.Verify(c => c.Dispose(), Times.Once);
    }

    [Test]
    public async Task SaveBatchAsync_PersistsAllEntities()
    {
        // Arrange
        var mockContext = new Mock<EventDbContext>();
        var mockSet = new Mock<DbSet<EventEntity>>();
        var logger = new Mock<ILogger<EventRepository>>();

        var entities = Enumerable.Range(0, 5).Select(i => new EventEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = Guid.NewGuid(),
            EventType = "Test",
            EventData = "{}",
            OccurredAt = DateTimeOffset.UtcNow
        }).ToList();

        mockContext.Setup(x => x.Events).Returns(mockSet.Object);
        mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(entities.Count);

        var repo = new EventRepository(mockContext.Object, logger.Object);

        // Act
        await repo.SaveBatchAsync(entities, CancellationToken.None);

        // Assert
        mockSet.Verify(x => x.AddRange(entities), Times.Once);
        mockContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }



}

