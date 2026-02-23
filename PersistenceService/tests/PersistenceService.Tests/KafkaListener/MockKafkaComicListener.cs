using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PersistenceService.Application.Interfaces;
using PersistenceService.Infrastructure.Database;
using PersistenceService.Infrastructure.Kafka;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PersistenceService.Tests.KafkaListener
{
    public class MockKafkaComicListener : KafkaComicListener
    {
        public MockKafkaComicListener(
            ILogger<KafkaComicListener> logger,
            IConfiguration config,
            IKafkaLogHelper kafkaLogHelper,
            IDatabaseReadyChecker dbReadyChecker,
            IServiceProvider serviceProvider,
            IConsumer<Ignore, string> mockConsumer)
            : base(logger, config, kafkaLogHelper, dbReadyChecker, serviceProvider, mockConsumer)
        {
        }

        // Disable BackgroundService pipeline entirely
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        // Directly run the real consume loop
        public Task RunConsumeLoopAsync(CancellationToken token)
        {
            return base.ConsumeLoopAsync(token);
        }
    }
}
