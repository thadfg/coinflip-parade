using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection; 
using PersistenceService.Application.Interfaces;
using PersistenceService.Application.Mappers;
using PersistenceService.Domain.Entities;
using PersistenceService.Infrastructure.Database;
using PersistenceService.Infrastructure.Kafka;
using PersistenceService.Infrastructure.Observability.Metrics;
using SharedLibrary.Facet;
using SharedLibrary.Models;
using System.Text.Json;
using System.Diagnostics.Metrics;

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
    private readonly int _consumeInitializeDelay;
    private CancellationTokenSource? _internalCts;
    private readonly int _batchSize;

    private static readonly Meter KafkaMeter = new("PersistenceService.Kafka", "1.0.0");
    
    private static readonly Counter<long> SavedComicsCounter = 
        KafkaMeter.CreateCounter<long>("saved_comics_total", "comics", "Total comics successfully persisted");
    
    private static readonly ObservableGauge<long> KafkaConsumerLag = 
        KafkaMeter.CreateObservableGauge<long>("kafka_consumer_lag", () => _latestLags.Values, "Kafka consumer lag for persistence service");

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Measurement<long>> _latestLags = new();
    
    public static void Initialize() { /* Forces static field initialization */ }

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
            _config.GetValue<int>("KafkaListener:FlushIntervalSeconds", 20));

        _consumeTimeout = TimeSpan.FromMilliseconds(
            _config.GetValue<int>("KafkaListener:ConsumeTimeoutMs", 10));

        _consumeInitializeDelay = _config.GetValue<int>("KafkaListener:ConsumeInitializeDelay", 1000);

         _batchSize = _config.GetValue<int>("KafkaListener:BatchSize",  10);
        

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

        _logger.LogInformation("Database ready. Exposing readiness metric.");
        ReadinessMetrics.SetDatabaseReady(1);

        _logger.LogInformation("Initializing Kafka consumer…");

        // Create consumer if not already created (useful for testing/mocking)
        _consumer ??= CreateConsumer() ;

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
                        await Task.Delay(_consumeInitializeDelay, stoppingToken);
                        continue;
                    }

                    // 1️ Consume (blocking, cancellation-aware)
                    var result = _consumer.Consume(stoppingToken);

                    if (result == null || result.IsPartitionEOF)
                        continue;

                    // --- LAG CALCULATION BLOCK ---
                    var topicPartition = result.TopicPartition; 
                    // Query the broker for the latest offsets
                    var watermark = _consumer.QueryWatermarkOffsets(topicPartition, TimeSpan.FromSeconds(1)); 
                    
                    long endOffset = watermark.High; 

                    long currentOffset = result.Offset.Value; 
                    
                    long lag = endOffset - currentOffset; 
                    // Emit the metric
                    var key = $"{result.Topic}-{result.Partition}";
                    _latestLags[key] = new Measurement<long>(lag, 
                        new KeyValuePair<string, object?>("topic", result.Topic),
                        new KeyValuePair<string, object?>("partition", result.Partition.ToString()),
                        new KeyValuePair<string, object?>("service", "persistence"));
                    // --- END BLOCK ---

                    if (result.Message?.Value == null)
                    {
                        _logger.LogWarning("Kafka returned a message with null value.");
                        continue;
                    }

                    // 2️ Deserialize
                    KafkaEnvelope<ComicCsvRecordDto>? envelope;
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

                    // 3️ Map to domain entities
                    var comic = ComicRecordMapper.ToEntity(envelope);
                    var eventEntity = EventEntityMapper.FromPayload(
                        envelope.Payload,
                        Guid.Parse(envelope.ImportId),
                        "ComicCsvRecordReceived"
                    );

                    //_comicRecordBuffer.Add((comic, Guid.Parse(envelope.ImportId)));
                    _comicRecordBuffer.Add((comic, eventEntity.Id));
                    _eventBuffer.Add(eventEntity);

                    // 4️ Batch flush based on size
                    if (_comicRecordBuffer.Count >= _batchSize)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var comicRepo = scope.ServiceProvider.GetRequiredService<IComicCollectionRepository>();
                        await comicRepo.UpsertBatchAsync(_comicRecordBuffer, stoppingToken);
                        
                        // REPORT SUCCESS TO METRICS
                        SavedComicsCounter.Add(_comicRecordBuffer.Count, new KeyValuePair<string, object?>("service_name", "PersistenceService"));
                        
                        _comicRecordBuffer.Clear();
                    }

                    if (_eventBuffer.Count >= _batchSize)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var eventRepo = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                        await eventRepo.SaveBatchAsync(_eventBuffer, stoppingToken);
                        _eventBuffer.Clear();
                    }

                    // 5️ Time-based flush (only once per loop)
                    await FlushIfNeededAsync(stoppingToken);

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
            // 6️ Final flush on shutdown
            if (_eventBuffer.Count > 0)
            {
                using var scope = _serviceProvider.CreateScope();
                var eventRepo = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                await eventRepo.SaveBatchAsync(_eventBuffer, CancellationToken.None);
            }

            if (_comicRecordBuffer.Count > 0)
            {
                using var scope = _serviceProvider.CreateScope();
                var comicRepo = scope.ServiceProvider.GetRequiredService<IComicCollectionRepository>();
                await comicRepo.UpsertBatchAsync(_comicRecordBuffer, CancellationToken.None);
                
                SavedComicsCounter.Add(_comicRecordBuffer.Count, new KeyValuePair<string, object?>("service_name", "PersistenceService"));
            }

            _consumer?.Close();
            _consumer?.Dispose();
        }
    }



    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _internalCts?.Cancel();
        return base.StopAsync(cancellationToken);
    }


    private async Task FlushBuffersAsync(CancellationToken stoppingToken)
    {

        // Log event buffer contents before flushing
        if (_eventBuffer.Count > 0) 
        { 
            _logger.LogInformation("Flushing {Count} events", _eventBuffer.Count); 
            
            foreach (var e in _eventBuffer) 
            { 
                _logger.LogInformation("EventEntity.Id: {Id}, AggregateId: {AggregateId}", 
                    e.Id, e.AggregateId); 
            } 
        }


        if (_comicRecordBuffer.Count > 0)
        {
            using var scope = _serviceProvider.CreateScope();
            var comicRepo = scope.ServiceProvider.GetRequiredService<IComicCollectionRepository>();
            await comicRepo.UpsertBatchAsync(_comicRecordBuffer, stoppingToken);
            _logger.LogInformation("Timed flush: persisted {Count} comics", _comicRecordBuffer.Count);

            SavedComicsCounter.Add(_comicRecordBuffer.Count, new KeyValuePair<string, object?>("service_name", "PersistenceService"));

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

    private async Task FlushIfNeededAsync(CancellationToken token)
    {
        if ((DateTime.UtcNow - _lastFlushTime) >= _flushInterval)
        {
            await FlushBuffersAsync(token);
            _lastFlushTime = DateTime.UtcNow;
        }
    }


}


