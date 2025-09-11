using SharedLibrary.Models;
using System.Text.Json;
using Confluent.Kafka;
using PersistenceService.Application.Mappers;

public class KafkaComicListener : BackgroundService
{
    private readonly ILogger<KafkaComicListener> _logger;
    private readonly IConfiguration _config;

    public KafkaComicListener(ILogger<KafkaComicListener> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"],
            GroupId = _config["Kafka:GroupId"],
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(_config["Kafka:Topic"]);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                var message = result.Message.Value;

                var envelope = JsonSerializer.Deserialize<KafkaEnvelope<ComicRecordDto>>(message);

                if (envelope != null)
                {
                    // TODO: Transform and persist
                    var entity = ComicRecordMapper.ToEntity(envelope);
                    _logger.LogInformation("Mapped comic: {Title}", entity.FullTitle);
                }
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error");
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Malformed JSON payload");
            }
        }

        consumer.Close();
    }
}
