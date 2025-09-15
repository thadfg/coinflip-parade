using Confluent.Kafka;
using PersistenceService.Application.Interfaces;
using PersistenceService.Application.Mappers;
using PersistenceService.Domain.Entities;
using SharedLibrary.Facet;
using SharedLibrary.Models;
using System.Text.Json;

public class KafkaComicListener : BackgroundService
{
    private readonly ILogger<KafkaComicListener> _logger;
    private readonly IConfiguration _config;
    private IConsumer<Ignore, string>? _consumer;
    private readonly IEventRepository _eventRepository;
    private readonly int _batchSize;
    private readonly List<EventEntity> _buffer = new(); // ✅ Field-level buffer


    public KafkaComicListener(ILogger<KafkaComicListener> logger, IConfiguration config, IEventRepository eventRepository,
        IConsumer<Ignore, string>? consumer = null)
    {
        _logger = logger;
        _config = config;
        _eventRepository = eventRepository;
        _batchSize = int.TryParse(_config["Kafka:BatchSize"], out var size) ? size : 10;
        _consumer = consumer;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_consumer == null)
        {
            _consumer = CreateConsumer();
        }
        _consumer.Subscribe(_config["Kafka:Topic"]);

        return ConsumeLoopAsync(stoppingToken);
    }

    protected virtual IConsumer<Ignore, string> CreateConsumer()
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"],
            GroupId = _config["Kafka:GroupId"],
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            EnablePartitionEof = true
        };

        return new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
    }

    private async Task ConsumeLoopAsync(CancellationToken stoppingToken)
    {
        //var buffer = new List<EventEntity>();
        var batchSize = int.TryParse(_config["Kafka:BatchSize"], out var size) ? size : 10;

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
                        var comic = ComicRecordMapper.ToEntity(envelope);
                        var eventEntity = EventEntityMapper.FromPayload(
                            envelope.Payload,
                            Guid.Parse(envelope.ImportId),
                            "ComicCsvRecordReceived"
                        );
                        _logger.LogInformation("Mapped event entity: {EventType}", eventEntity.EventType);

                        _logger.LogInformation("Mapped comic: {Title}", comic.FullTitle);

                        _buffer.Add(eventEntity);
                        // TODO: Persist comic

                        if (_buffer.Count >= batchSize)
                        {
                            await _eventRepository.SaveBatchAsync(_buffer, stoppingToken);
                            _logger.LogInformation("Persisted batch of {Count} events", _buffer.Count);
                            _buffer.Clear();
                        }
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
            if (_buffer.Count > 0)
            {
                _logger.LogInformation("Final buffer count before flush: {Count}", _buffer.Count);
                await _eventRepository.SaveBatchAsync(_buffer, stoppingToken);
                _logger.LogInformation("Persisted final batch of {Count} events", _buffer.Count);
                _buffer.Clear();
            }

            _consumer?.Close(); // Commit offsets and leave group
            _consumer?.Dispose();
            _logger.LogInformation("Kafka consumer shut down gracefully");
        }
    }

}
