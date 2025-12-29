using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedLibrary.Kafka;
using System.Text.Json;
using System.Text;
using System.Diagnostics;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry;
using Polly;

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
            
            logger.LogInformation("Kafka producer successfully constructed");

        }

        // Added optional correlationId parameter; if null a new GUID will be generated.
        public async Task ProduceAsync<T>(string topic, string key, T message, string? correlationId = null)
        {
            var payload = JsonSerializer.Serialize(message);
            var corrId = correlationId ?? Guid.NewGuid().ToString();

            try
            {
                var msg = new Message<string, string>
                {
                    Key = key,
                    Value = payload,
                    Headers = new Headers()
                };

                // Add stable per-message correlation id
                msg.Headers.Add("correlation-id", Encoding.UTF8.GetBytes(corrId));

                // Use OpenTelemetry propagator to inject trace context + baggage into headers automatically.
                // The setter maps carrier,key,value => add to Confluent.Kafka.Headers
                if (Propagators.DefaultTextMapPropagator != null)
                {
                    var propagationContext = Activity.Current != null
                        ? new PropagationContext(Activity.Current.Context, Baggage.Current)
                        : new PropagationContext(default, default);

                    void Setter(Headers headers, string name, string value)
                        => headers.Add(name, Encoding.UTF8.GetBytes(value ?? string.Empty));

                    Propagators.DefaultTextMapPropagator.Inject(propagationContext, msg.Headers, (h, name, value) => Setter(h, name, value));
                }

                var result = await _producer.ProduceAsync(topic, msg);

                _logger.LogInformation("Kafka message delivered to {TopicPartitionOffset} (corr:{CorrelationId})", result.TopicPartitionOffset, corrId);
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(ex, "Kafka delivery failed for topic {Topic}: {Reason}", topic, ex.Error.Reason);
                throw;
            }
        }
    }
}
