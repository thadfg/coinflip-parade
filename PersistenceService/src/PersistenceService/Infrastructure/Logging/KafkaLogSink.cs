using System;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace PersistenceService.Infrastructure.Logging;

public class KafkaLogSink : IKafkaLogSink
{
    private readonly IProducer<Null, string> _producer;
    private readonly string _topic;

    public KafkaLogSink(IProducer<Null, string> producer, IConfiguration config)
    {
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
        _topic = config["Kafka:LogTopic"] ?? "service-logs";
    }

    public async Task LogToKafkaAsync(string level, string message, Exception? ex = null)
    {
        var log = new
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Message = message,
            Exception = ex?.ToString(),
            Host = Environment.MachineName,
            Service = "PersistenceService"
        };

        var payload = JsonSerializer.Serialize(log);

        await _producer.ProduceAsync(_topic, new Message<Null, string> { Value = payload });
    }
}