using Confluent.Kafka;
using SharedLibrary.Kafka;
using System.Text.Json;

namespace IngestionService.Infrastructure.Kafka
{
    public class KafkaProducer : IKafkaProducer
    {
        private readonly IProducer<string, string> _producer;

        public KafkaProducer(KafkaSettings settings)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = settings.BootstrapServers,
                ClientId = settings.ClientId,
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

                Console.WriteLine($"[Kafka] Delivered to {result.TopicPartitionOffset}");
            }
            catch (ProduceException<string, string> ex)
            {
                Console.Error.WriteLine($"[Kafka] Delivery failed: {ex.Error.Reason}");
                throw;
            }
        }
    }
}
