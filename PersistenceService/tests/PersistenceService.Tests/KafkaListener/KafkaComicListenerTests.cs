using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PersistenceService.Application.Interfaces;
using PersistenceService.Domain.Entities;
using PersistenceService.Infrastructure.Database;
using PersistenceService.Infrastructure.Kafka;
using SharedLibrary.Facet;
using SharedLibrary.Models;
using System.Text.Json;
using Xunit;

namespace PersistenceService.Tests.KafkaListener
{
    public class KafkaComicListenerTests
    {
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
    }
}
