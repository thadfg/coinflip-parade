using Confluent.Kafka;
using PersistenceService.Application.Mappers;
using SharedLibrary.Facet;
using SharedLibrary.Models;
using System.Text.Json;

public class KafkaComicListener : BackgroundService
{
    private readonly ILogger<KafkaComicListener> _logger;
    private readonly IConfiguration _config;
    private IConsumer<Ignore, string>? _consumer;

    public KafkaComicListener(ILogger<KafkaComicListener> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"],
            GroupId = _config["Kafka:GroupId"],
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            EnablePartitionEof = true
        };

        _consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
        _consumer.Subscribe(_config["Kafka:Topic"]);

        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(stoppingToken);
                    var message = result.Message.Value;

                    var envelope = JsonSerializer.Deserialize<KafkaEnvelope<ComicCsvRecordDto>>(message);

                    if (envelope != null)
                    {
                        var entity = ComicRecordMapper.ToEntity(envelope);
                        _logger.LogInformation("Mapped comic: {Title}", entity.FullTitle);
                        // TODO: Persist entity
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
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka consumer cancellation requested");
        }
        finally
        {
            _consumer?.Close(); // Commit offsets and leave group
            _consumer?.Dispose();
            _logger.LogInformation("Kafka consumer shut down gracefully");
        }
    }
}
