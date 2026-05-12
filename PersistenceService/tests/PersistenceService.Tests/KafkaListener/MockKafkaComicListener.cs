using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PersistenceService.Application.Interfaces;
using PersistenceService.Infrastructure.Database;
using PersistenceService.Infrastructure.Kafka;
using PersistenceService.Config;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PersistenceService.Tests.KafkaListener
{
    public class MockKafkaComicListener : KafkaComicListener
    {
        public MockKafkaComicListener(
            ILogger<KafkaComicListener> logger,
            IOptions<KafkaOptions> options,
            IKafkaLogHelper kafkaLogHelper,
            IServiceProvider serviceProvider,
            IConsumer<string, string> consumer)
            : base(logger, options, kafkaLogHelper, serviceProvider, consumer)
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
