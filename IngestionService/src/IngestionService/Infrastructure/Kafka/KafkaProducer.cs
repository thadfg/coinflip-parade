using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedLibrary.Kafka;
using System.Text.Json;

namespace IngestionService.Infrastructure.Kafka
{
    public class KafkaProducer : IKafkaProducer
    {        
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaProducer> _logger;

        public KafkaProducer(IOptions<KafkaSettings> options, ILogger<KafkaProducer> logger)
        {
            _logger = logger;
            logger.LogInformation("Kafka BootstrapServers: {BootstrapServers}", options.Value.BootstrapServers);


            var config = new ProducerConfig
            {
                BootstrapServers = options.Value.BootstrapServers,
                ClientId = options.Value.ClientId,
                Acks = Acks.All,
                EnableIdempotence = true,
                MessageSendMaxRetries = 3,
                RetryBackoffMs = 100
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
            
        }

        public async Task ProduceAsync<T>(string topic, string key, T message)
        {
            var payload = JsonSerializer.Serialize(message);

            try
            {
                var result = await _producer.ProduceAsync(topic, new Message<string, string>
                {
                    Key = key,
                    Value = payload
                });

                _logger.LogInformation("Kafka message delivered to {TopicPartitionOffset}", result.TopicPartitionOffset);                 
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(ex, "Kafka delivery failed for topic {Topic}: {Reason}", topic, ex.Error.Reason);                 
                throw;
            }
        }
    }
}
