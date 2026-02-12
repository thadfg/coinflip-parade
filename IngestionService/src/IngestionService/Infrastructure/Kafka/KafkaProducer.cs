using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using SharedLibrary.Kafka; // Ensure this contains your KafkaSettings and IKafkaProducer

namespace IngestionService.Infrastructure.Kafka
{
    public class KafkaProducer : IKafkaProducer, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaProducer> _logger;
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

        public KafkaProducer(IOptions<KafkaSettings> options, ILogger<KafkaProducer> logger)
        {
            _logger = logger;
            
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
            _logger.LogInformation("Kafka producer started for BootstrapServers: {BootstrapServers}", options.Value.BootstrapServers);
        }

        public async Task ProduceAsync<T>(string topic, string key, T message, string? correlationId = null)
        {
            var payload = JsonSerializer.Serialize(message);
            var currentActivity = Activity.Current;
            var corrId = correlationId ?? Guid.NewGuid().ToString();

            try
            {
                var msg = new Message<string, string>
                {
                    Key = key,
                    Value = payload,
                    Headers = new Headers()
                };

                // 1. Manual Correlation ID Logic
                msg.Headers.Add("correlation-id", Encoding.UTF8.GetBytes(corrId));

                // 2. OpenTelemetry Context Injection
                // Injects 'traceparent' and 'baggage' into Kafka Headers for distributed tracing
                var contextToInject = currentActivity?.Context ?? default;
                var propagationContext = new PropagationContext(contextToInject, Baggage.Current);

                Propagator.Inject(propagationContext, msg.Headers, (headers, name, value) =>
                {
                    // Remove existing header to avoid duplicates during internal Kafka retries
                    headers.Remove(name);
                    headers.Add(name, Encoding.UTF8.GetBytes(value));
                });

                // 3. Enrich the current Span with Kafka metadata
                currentActivity?.SetTag("messaging.system", "kafka");
                currentActivity?.SetTag("messaging.destination", topic);
                currentActivity?.SetTag("messaging.kafka.message_key", key);

                var result = await _producer.ProduceAsync(topic, msg);

                _logger.LogInformation("Kafka message delivered to {TopicPartitionOffset} [TraceId: {TraceId}, CorrId: {CorrelationId}]", 
                    result.TopicPartitionOffset, 
                    currentActivity?.TraceId.ToString() ?? "n/a", 
                    corrId);
            }
            catch (ProduceException<string, string> ex)
            {
                // Mark the telemetry span as failed so it shows up red in your dashboard
                currentActivity?.SetStatus(ActivityStatusCode.Error, ex.Error.Reason);
                _logger.LogError(ex, "Kafka delivery failed for topic {Topic}: {Reason}", topic, ex.Error.Reason);
                throw;
            }
        }

        public void Dispose()
        {
            // Ensure any buffered messages are sent before the application shuts down
            _producer?.Flush(TimeSpan.FromSeconds(10));
            _producer?.Dispose();
        }
    }
}