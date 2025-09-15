using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PersistenceService.Application.Interfaces;
using PersistenceService.Domain.Entities;
using SharedLibrary.Facet;
using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace PersistenceService.Tests.KafkaListener
{
    public class KafkaComicListenerTests
    {        

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


            //var mockLogger = new Mock<ILogger<KafkaComicListener>>();
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
                Payload = new ComicCsvRecordDto { SeriesName = "X-Men", FullTitle = "1", PublisherName = "Marvel", ReleaseDate = "2024-12-16" }
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

            // 👇 Capture the batch passed to SaveBatchAsync
            IEnumerable<EventEntity>? capturedBatch = null;
            mockRepo.Setup(r => r.SaveBatchAsync(It.IsAny<IEnumerable<EventEntity>>(), It.IsAny<CancellationToken>()))
                .Callback<IEnumerable<EventEntity>, CancellationToken>((batch, _) =>
                {
                    capturedBatch = batch.ToList(); // Force materialization  IEnumerable caused an issue with deffered execution and the list being empty at assertion time
                    Console.WriteLine($"Captured batch count: {batch.Count()}");
                    foreach (var e in batch)
                    {
                        Console.WriteLine($"EventType: {e.EventType}, EventData: {e.EventData}");
                    }
                });


            // Inject the mock consumer via a testable subclass
            var listener = new MockKafkaComicListener(mockLogger.Object, config, mockRepo.Object, mockConsumer.Object);

            // Act
            await listener.StartAsync(cancellationSource.Token);

            mockRepo.Verify(); // Confirms the setup was hit


            // Assert
            //mockRepo.Verify(r => r.SaveBatchAsync(It.Is<List<EventEntity>>(b => b.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
            //mockRepo.Verify(r => r.SaveBatchAsync(It.IsAny<List<EventEntity>>(), It.IsAny<CancellationToken>()), Times.Once);

            //List<EventEntity>? capturedBatch = null;

            /*mockRepo.Setup(r => r.SaveBatchAsync(It.IsAny<List<EventEntity>>(), It.IsAny<CancellationToken>()))
                .Callback<List<EventEntity>, CancellationToken>((batch, _) => capturedBatch = batch);*/

            Assert.NotNull(capturedBatch);
            Assert.Single(capturedBatch); // ✅ This confirms the count


            mockConsumer.Verify(c => c.Close(), Times.Once);
            mockConsumer.Verify(c => c.Dispose(), Times.Once);
        }

    }
}
