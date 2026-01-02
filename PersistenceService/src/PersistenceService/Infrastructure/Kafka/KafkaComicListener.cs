using Confluent.Kafka;
using PersistenceService.Application.Interfaces;
using PersistenceService.Application.Mappers;
using PersistenceService.Domain.Entities;
using PersistenceService.Infrastructure.Kafka;
using SharedLibrary.Facet;
using SharedLibrary.Models;
using System.Text.Json;
using PersistenceService.Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection; 

public class KafkaComicListener : BackgroundService
{
    private readonly ILogger<KafkaComicListener> _logger;
    private readonly IConfiguration _config;
    private readonly IKafkaLogHelper _kafkaLogHelper;
    private readonly IDatabaseReadyChecker _dbReadyChecker;
    private readonly TimeSpan _dbReadyDelay;

    private readonly IServiceProvider _serviceProvider; 

    private IConsumer<Ignore, string>? _consumer;

    private readonly List<EventEntity> _eventBuffer = new();
    private readonly List<(ComicRecordEntity Comic, Guid EventId)> _comicRecordBuffer = new();

    private DateTime _lastFlushTime = DateTime.UtcNow;

    private readonly TimeSpan _flushInterval;
    private readonly TimeSpan _consumeTimeout;
    protected CancellationTokenSource? _internalCts;





    public KafkaComicListener(
        ILogger<KafkaComicListener> logger,
        IConfiguration config,
        IKafkaLogHelper kafkaLogHelper,
        IDatabaseReadyChecker dbReadyChecker,
        IServiceProvider serviceProvider,                  
        IConsumer<Ignore, string>? consumer = null)
    {
        _logger = logger;
        _config = config;
        _kafkaLogHelper = kafkaLogHelper;
        _dbReadyChecker = dbReadyChecker;
        _consumer = consumer; 
        _serviceProvider = serviceProvider;               

        _dbReadyDelay = TimeSpan.FromSeconds(
            _config.GetValue<int>("KafkaListener:DatabaseReadyCheckDelaySeconds", 2));

        _flushInterval = TimeSpan.FromSeconds(
            _config.GetValue<int>("KafkaListener:FlushIntervalSeconds", 5));

        _consumeTimeout = TimeSpan.FromMilliseconds(
            _config.GetValue<int>("KafkaListener:ConsumeTimeoutMs", 10));

    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _internalCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken); 
        var linkedToken = _internalCts.Token;

        _logger.LogInformation("KafkaComicListener starting…");

        // Wait for the database to be ready before consuming Kafka messages
        while (!await _dbReadyChecker.IsReadyAsync(linkedToken))
        {
            _logger.LogInformation("Waiting for database to be ready…");
            await Task.Delay(_dbReadyDelay, linkedToken);
        }

        _logger.LogInformation("Database ready. Initializing Kafka consumer…");

        // Create consumer if not already created (useful for testing/mocking)
        if (_consumer == null)
            _consumer = CreateConsumer();

        // Subscribe to Kafka topic
        var topic = _config["Kafka:Topic"];
        _logger.LogInformation("Subscribing to Kafka topic: {Topic}", topic);
        _consumer.Subscribe(topic);

        _logger.LogInformation("Kafka subscription complete. Starting consume loop…");

        // Begin consuming messages
        await ConsumeLoopAsync(linkedToken);
    }

    private IConsumer<Ignore, string> CreateConsumer()
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

    protected virtual async Task ConsumeLoopAsync(CancellationToken stoppingToken)
    {
        var batchSize = int.TryParse(_config["Kafka:BatchSize"], out var size) ? size : 10;

        try
        {
            while (true)
            {
                stoppingToken.ThrowIfCancellationRequested();

                try
                {
                    if (_consumer == null)
                    {
                        _logger.LogWarning("Kafka consumer is not initialized.");
                        await Task.Delay(1000, stoppingToken);
                        continue;
                    }

                    // ✅ Cancellation-aware consume
                    var result = _consumer.Consume(stoppingToken);

                    if (result == null)
                    {
                        if ((DateTime.UtcNow - _lastFlushTime) >= _flushInterval)
                        {
                            await FlushBuffersAsync(stoppingToken);
                            _lastFlushTime = DateTime.UtcNow;
                        }
                        continue;
                    }

                    if (result.IsPartitionEOF)
                    {
                        _logger.LogDebug("Reached end of partition {Partition}", result.Partition);
                        continue;
                    }

                    if (result.Message == null || result.Message.Value == null)
                    {
                        _logger.LogWarning("Kafka returned a message with null value.");
                        continue;
                    }

                    _logger.LogInformation(
                        "Consumed message from {Topic} [{Partition}] @ {Offset}",
                        result.Topic, result.Partition, result.Offset);

                    _logger.LogInformation("Consumed Kafka message: {Raw}", result.Message.Value);

                    KafkaEnvelope<ComicCsvRecordDto>? envelope = null;

                    try
                    {
                        envelope = JsonSerializer.Deserialize<KafkaEnvelope<ComicCsvRecordDto>>(result.Message.Value);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Malformed JSON payload");
                        continue;
                    }

                    if (envelope == null)
                    {
                        _logger.LogWarning("Envelope deserialized to null.");
                        continue;
                    }

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

                    _logger.LogInformation("Preparing to write {Count} comics to DB", _comicRecordBuffer.Count);
                    foreach (var (c, _) in _comicRecordBuffer)
                    {
                        _logger.LogInformation("Comic ready for DB: {Title}", c.FullTitle);
                    }

                    if (_comicRecordBuffer.Count >= batchSize)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var comicRepo = scope.ServiceProvider.GetRequiredService<IComicCollectionRepository>();

                        await comicRepo.UpsertBatchAsync(_comicRecordBuffer, stoppingToken);
                        _logger.LogInformation("Persisted batch of {Count} comics", _comicRecordBuffer.Count);

                        _comicRecordBuffer.Clear();
                    }

                    if (_eventBuffer.Count >= batchSize)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var eventRepo = scope.ServiceProvider.GetRequiredService<IEventRepository>();

                        await eventRepo.SaveBatchAsync(_eventBuffer, stoppingToken);
                        _logger.LogInformation("Persisted batch of {Count} events", _eventBuffer.Count);

                        _eventBuffer.Clear();
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
            _logger.LogInformation("KafkaComicListener cancellation requested");
        }
        finally
        {
            if (_eventBuffer.Count > 0)
            {
                _logger.LogInformation("Final event buffer flush: {Count}", _eventBuffer.Count);

                using var scope = _serviceProvider.CreateScope();
                var eventRepo = scope.ServiceProvider.GetRequiredService<IEventRepository>();

                await eventRepo.SaveBatchAsync(_eventBuffer, CancellationToken.None);
            }

            if (_comicRecordBuffer.Count > 0)
            {
                _logger.LogInformation("Final comic buffer flush: {Count}", _comicRecordBuffer.Count);

                using var scope = _serviceProvider.CreateScope();
                var comicRepo = scope.ServiceProvider.GetRequiredService<IComicCollectionRepository>();

                await comicRepo.UpsertBatchAsync(_comicRecordBuffer, CancellationToken.None);
            }

            _consumer?.Close();
            _consumer?.Dispose();

            _logger.LogInformation("KafkaComicListener shut down gracefully");
        }
    }


    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _internalCts?.Cancel();
        return base.StopAsync(cancellationToken);
    }


    private async Task FlushBuffersAsync(CancellationToken stoppingToken)
    {
        if (_comicRecordBuffer.Count > 0)
        {
            using var scope = _serviceProvider.CreateScope();
            var comicRepo = scope.ServiceProvider.GetRequiredService<IComicCollectionRepository>();
            await comicRepo.UpsertBatchAsync(_comicRecordBuffer, stoppingToken);
            _logger.LogInformation("Timed flush: persisted {Count} comics", _comicRecordBuffer.Count);
            _comicRecordBuffer.Clear();
        }

        if (_eventBuffer.Count > 0)
        {
            using var scope = _serviceProvider.CreateScope();
            var eventRepo = scope.ServiceProvider.GetRequiredService<IEventRepository>();
            await eventRepo.SaveBatchAsync(_eventBuffer, stoppingToken);
            _logger.LogInformation("Timed flush: persisted {Count} events", _eventBuffer.Count);
            _eventBuffer.Clear();
        }
    }

}


