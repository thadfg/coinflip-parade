using Confluent.Kafka;
using PersistenceService.Application.Interfaces;
using PersistenceService.Application.Mappers;
using PersistenceService.Domain.Entities;
using PersistenceService.Infrastructure.Kafka;
using PersistenceService.Infrastructure.Logging;
using SharedLibrary.Facet;
using SharedLibrary.Models;
using System.Text.Json;

public class KafkaComicListener : BackgroundService
{
    private readonly ILogger<KafkaComicListener> _logger;
    private readonly IConfiguration _config;
    private IConsumer<Ignore, string>? _consumer;
    private readonly IEventRepository _eventRepository;
    private readonly IComicCollectionRepository _comicCollectionRepository;
    private readonly List<EventEntity> _eventBuffer = new(); // ✅ Field-level buffer
    private readonly List<(ComicRecordEntity Comic, Guid EventId)> _comicRecordBuffer = new();
    private readonly IKafkaLogHelper _kafkaLogHelper;
    



    public KafkaComicListener(
        ILogger<KafkaComicListener> logger, 
        IConfiguration config, 
        IEventRepository eventRepository,
        IComicCollectionRepository comicCollectionRepository,
        IKafkaLogHelper kafkaLogHelper,
        IConsumer<Ignore, string>? consumer = null
        )
        
    {
        _logger = logger;
        _config = config;
        _eventRepository = eventRepository;
        _comicCollectionRepository = comicCollectionRepository; 
        _consumer = consumer;
        _kafkaLogHelper = kafkaLogHelper;
        
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

    protected virtual IConsumer<Ignore, string> CreateConsumer() //Overiden for testing purposes. IConsumer belongs to Kafka Library.
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

                        _eventBuffer.Add(eventEntity);
                        _comicRecordBuffer.Add((comic, Guid.Parse(envelope.ImportId)));

                        // ✅ Upsert comic to state table
                        if (_comicRecordBuffer.Count >= batchSize)
                        {
                            await _comicCollectionRepository.UpsertBatchAsync(_comicRecordBuffer, stoppingToken);
                            _logger.LogInformation("Persisted batch of {Count} comics", _comicRecordBuffer.Count);
                            _comicRecordBuffer.Clear();
                        }
                        

                        if (_eventBuffer.Count >= batchSize)
                        {
                            await _eventRepository.SaveBatchAsync(_eventBuffer, stoppingToken);
                            _logger.LogInformation("Persisted batch of {Count} events", _eventBuffer.Count);
                            _eventBuffer.Clear();
                        }
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error");
                    await _kafkaLogHelper.LogToKafkaAsync("Error", "Kafka consume error", ex);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Malformed JSON payload");
                    await _kafkaLogHelper.LogToKafkaAsync("Warning", "Malformed JSON payload", ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka consumer cancellation requested"); 
            await _kafkaLogHelper.LogToKafkaAsync("Information", "Kafka consumer cancellation requested");
        }
        finally
        {
            if (_eventBuffer.Count > 0)
            {
                _logger.LogInformation("Final buffer count before flush: {Count}", _eventBuffer.Count);
                await _eventRepository.SaveBatchAsync(_eventBuffer, stoppingToken);
                _logger.LogInformation("Persisted final batch of {Count} events", _eventBuffer.Count);
                _eventBuffer.Clear();
            }

            if (_comicRecordBuffer.Count > 0)
            {
                _logger.LogInformation("Final comic buffer count before flush: {Count}", _comicRecordBuffer.Count);
                await _comicCollectionRepository.UpsertBatchAsync(_comicRecordBuffer, stoppingToken);
                _logger.LogInformation("Persisted final batch of {Count} comics", _comicRecordBuffer.Count);
                _comicRecordBuffer.Clear();
            }

            _consumer?.Close(); // Commit offsets and leave group
            _consumer?.Dispose();
            _logger.LogInformation("Kafka consumer shut down gracefully");
            await _kafkaLogHelper.LogToKafkaAsync("Information", "Kafka consumer shut down gracefully");
        }
    }

}
