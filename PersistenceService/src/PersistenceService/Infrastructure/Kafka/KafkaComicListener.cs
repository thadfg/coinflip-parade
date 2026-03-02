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
    private readonly IServiceProvider _serviceProvider; 

    private IConsumer<Ignore, string>? _consumer;

    private readonly List<EventEntity> _eventBuffer = new();
    private readonly List<(ComicRecordEntity Comic, Guid EventId)> _comicRecordBuffer = new();

    private DateTime _lastFlushTime = DateTime.UtcNow;

    private readonly TimeSpan _flushInterval;
    private readonly TimeSpan _consumeTimeout;
    private readonly int _consumeInitializeDelay;
    private readonly TimeSpan _dbReadyDelay;
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
        IServiceProvider serviceProvider,                  
        IConsumer<Ignore, string>? consumer = null)
    {
        _logger = logger;
        _config = config;
        _kafkaLogHelper = kafkaLogHelper;
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

        // FIX: Resolve the Scoped IDatabaseReadyChecker inside a manual scope to avoid DI lifetime errors
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbReadyChecker = scope.ServiceProvider.GetRequiredService<IDatabaseReadyChecker>();
            while (!await dbReadyChecker.IsReadyAsync(linkedToken))
            {
                _logger.LogInformation("Waiting for database to be ready…");
                await Task.Delay(_dbReadyDelay, linkedToken);
            }
        }

        _logger.LogInformation("Database ready. Exposing readiness metric.");
        ReadinessMetrics.SetDatabaseReady(1);

        _logger.LogInformation("Initializing Kafka consumer…");

        _consumer ??= CreateConsumer();

        var topic = _config["Kafka:Topic"] ?? "comic-imported";
        _logger.LogInformation("Subscribing to Kafka topic: {Topic}", topic);
        _consumer.Subscribe(topic);

        _logger.LogInformation("Kafka subscription complete. Starting consume loop…");

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
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_consumer == null)
                    {
                        _logger.LogWarning("Kafka consumer is not initialized.");
                        await Task.Delay(_consumeInitializeDelay, stoppingToken);
                        continue;
                    }

                    var result = _consumer.Consume(_consumeTimeout);

                    if (result == null || result.IsPartitionEOF)
                    {
                        // Check if we need to flush buffers even if no message was received
                        await FlushIfNeededAsync(stoppingToken);
                        continue;
                    }

                    // --- LAG CALCULATION ---
                    UpdateLagMetrics(result);

                    if (result.Message?.Value == null) continue;

                    // --- DESERIALIZATION ---
                    var envelope = JsonSerializer.Deserialize<KafkaEnvelope<ComicCsvRecordDto>>(result.Message.Value);
                    if (envelope == null) continue;

                    // --- MAPPING ---
                    var comic = ComicRecordMapper.ToEntity(envelope);
                    
                    // Note: Ensure EventEntityMapper generates/assigns a unique ID that matches your ProcessedEvents EventId logic
                    var eventEntity = EventEntityMapper.FromPayload(
                        envelope.Payload,
                        Guid.Parse(envelope.ImportId),
                        "ComicCsvRecordReceived"
                    );
                    
                    _comicRecordBuffer.Add((comic, eventEntity.Id));
                    _eventBuffer.Add(eventEntity);

                    // --- BATCH FLUSH ---
                    if (_comicRecordBuffer.Count >= _batchSize || _eventBuffer.Count >= _batchSize)
                    {
                        await FlushBuffersAsync(stoppingToken);
                    }

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
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in consume loop");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("KafkaComicListener cancellation requested");
        }
        finally
        {
            await FinalCleanupAsync();
        }
    }

    private void UpdateLagMetrics(ConsumeResult<Ignore, string> result)
    {
        try 
        {
            var watermark = _consumer!.QueryWatermarkOffsets(result.TopicPartition, TimeSpan.FromMilliseconds(500));
            long lag = watermark.High - result.Offset.Value;
            var key = $"{result.Topic}-{result.Partition}";
            _latestLags[key] = new Measurement<long>(lag, 
                new KeyValuePair<string, object?>("topic", result.Topic),
                new KeyValuePair<string, object?>("partition", result.Partition.ToString()));
        }
        catch (Exception ex)
        {
            _logger.LogTrace("Could not update lag metrics: {Msg}", ex.Message);
        }
    }

    private async Task FlushBuffersAsync(CancellationToken stoppingToken)
    {
        if (_comicRecordBuffer.Count == 0 && _eventBuffer.Count == 0) return;

        using var scope = _serviceProvider.CreateScope();
        
        if (_comicRecordBuffer.Count > 0)
        {
            var comicRepo = scope.ServiceProvider.GetRequiredService<IComicCollectionRepository>();
            await comicRepo.UpsertBatchAsync(_comicRecordBuffer, stoppingToken);
            
            SavedComicsCounter.Add(_comicRecordBuffer.Count, new KeyValuePair<string, object?>("service_name", "PersistenceService"));
            _logger.LogInformation("Persisted {Count} comics", _comicRecordBuffer.Count);
            _comicRecordBuffer.Clear();
        }

        if (_eventBuffer.Count > 0)
        {
            var eventRepo = scope.ServiceProvider.GetRequiredService<IEventRepository>();
            await eventRepo.SaveBatchAsync(_eventBuffer, stoppingToken);
            _logger.LogInformation("Persisted {Count} events", _eventBuffer.Count);
            _eventBuffer.Clear();
        }
        
        _lastFlushTime = DateTime.UtcNow;
    }

    private async Task FlushIfNeededAsync(CancellationToken token)
    {
        if ((DateTime.UtcNow - _lastFlushTime) >= _flushInterval)
        {
            await FlushBuffersAsync(token);
        }
    }

    private async Task FinalCleanupAsync()
    {
        _logger.LogInformation("Shutting down Kafka listener and performing final flush...");
        try 
        {
            await FlushBuffersAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during final buffer flush");
        }
        finally 
        {
            _consumer?.Close();
            _consumer?.Dispose();
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _internalCts?.Cancel();
        return base.StopAsync(cancellationToken);
    }
}