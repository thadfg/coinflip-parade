/* PSEUDOCODE / PLAN (detailed)
1. Purpose:
   - Extract the Logging-to-Kafka helper logic from KafkaComicListener into a dedicated helper class so the listener can delegate logging responsibilities.

2. Class responsibilities:
   - Accept dependencies required for logging: an ILogger, IConfiguration, an optional IProducer<Null,string>, and an optional IKafkaLogSink.
   - Provide a single public async method `LogToKafkaAsync(string level, string message, Exception? ex = null)` that:
     a. If an IKafkaLogSink is provided, delegate logging to it and return.
     b. If no IProducer is configured, fall back to the provided ILogger (write an informational message) and return.
     c. Otherwise, construct a log payload (Timestamp, Level, Message, Exception, Host, Service).
     d. Serialize the payload to JSON.
     e. Determine the topic from configuration key "Kafka:LogTopic", default to "service-logs".
     f. Produce the message to Kafka using the provided IProducer.

3. Error handling:
   - Throw ArgumentNullException in constructor if required non-null dependencies (ILogger, IConfiguration) are not provided.
   - Do not swallow exceptions from ProduceAsync; let caller handle or await exceptions bubble up.

4. Namespacing and compatibility:
   - Place class in `PersistenceService.Infrastructure.Kafka` to match existing code structure.
   - Use the same referenced types as the original file (Confluent.Kafka, Microsoft.Extensions.Logging, Microsoft.Extensions.Configuration, System.Text.Json).
   - Keep method name identical to original (`LogToKafkaAsync`) for minimal changes when replacing call sites.

5. Usage:
   - Construct an instance of `KafkaLogHelper` in `KafkaComicListener` (inject or instantiate) and replace the extracted method with a simple delegation call:
     await _kafkaLogHelper.LogToKafkaAsync(level, message, ex);

End of pseudocode
*/

using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PersistenceService.Infrastructure.Logging;

namespace PersistenceService.Infrastructure.Kafka
{
    public class KafkaLogHelper : IKafkaLogHelper
    {
        private readonly ILogger<KafkaLogHelper> _logger;
        private readonly IConfiguration _config;
        private readonly IProducer<Null, string>? _kafkaProducer;
        private readonly IKafkaLogSink? _kafkaLogSink;

        public KafkaLogHelper(
            ILogger<KafkaLogHelper> logger,
            IConfiguration config,
            IProducer<Null, string>? kafkaProducer = null,
            IKafkaLogSink? kafkaLogSink = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _kafkaProducer = kafkaProducer;
            _kafkaLogSink = kafkaLogSink;
        }

        public async Task LogToKafkaAsync(string level, string message, Exception? ex = null, string? correlationId = null)
        {
            // Prefer explicit correlationId; fall back to current Activity (trace id) when available
            correlationId ??= Activity.Current?.TraceId.ToString() ?? Activity.Current?.Id;

            if (_kafkaLogSink != null)
            {
                await _kafkaLogSink.LogToKafkaAsync(level, message, ex);
                return;
            }

            if (_kafkaProducer == null)
            {
                // No producer configured (e.g. in unit tests). Fall back to normal logger.
                // Include correlation id in structured log
                _logger.LogInformation("{Level}: {Message} {CorrelationId}", level, message, correlationId);
                return;
            }

            var log = new
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Message = message,
                Exception = ex?.ToString(),
                CorrelationId = correlationId,
                Host = Environment.MachineName,
                Service = "PersistenceService"
            };

            var payload = JsonSerializer.Serialize(log);
            var topic = _config["Kafka:LogTopic"] ?? "service-logs";

            await _kafkaProducer.ProduceAsync(topic, new Message<Null, string> { Value = payload });
        }
    }
}