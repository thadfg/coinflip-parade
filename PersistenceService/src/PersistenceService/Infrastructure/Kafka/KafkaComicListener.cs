using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using PersistenceService.Application.Interfaces;
using PersistenceService.Application.Mappers;
using PersistenceService.Domain.Entities;
using PersistenceService.Infrastructure.Kafka;
using PersistenceService.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using PersistenceService.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using SharedLibrary.Facet;
using SharedLibrary.Models;
using System.Text.Json;
using Microsoft.Extensions.Hosting;

public class KafkaComicListener : BackgroundService
{
    private readonly ILogger<KafkaComicListener> _logger;
    private readonly IConfiguration _config;
    private readonly IEventRepository _eventRepository;
    private readonly IComicCollectionRepository _comicCollectionRepository;
    private readonly IKafkaLogHelper _kafkaLogHelper;

    private IConsumer<Ignore, string>? _consumer;

    private readonly List<EventEntity> _eventBuffer = new();
    private readonly List<(ComicRecordEntity Comic, System.Guid EventId)> _comicRecordBuffer = new();

    public KafkaComicListener(
        ILogger<KafkaComicListener> logger,
        IConfiguration config,
        IEventRepository eventRepository,
        IComicCollectionRepository comicCollectionRepository,
        IKafkaLogHelper kafkaLogHelper,
        IConsumer<Ignore, string>? consumer = null)
    {
        _logger = logger;
        _config = config;
        _eventRepository = eventRepository;
        _comicCollectionRepository = comicCollectionRepository;
        _kafkaLogHelper = kafkaLogHelper;
        _consumer = consumer; // optional override for testing
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("KafkaComicListener starting…");

        if (_consumer == null)
            _consumer = CreateConsumer();

        _consumer.Subscribe(_config["Kafka:Topic"]);

        await ConsumeLoopAsync(stoppingToken);
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

    private async Task ConsumeLoopAsync(CancellationToken stoppingToken)
    {
        var batchSize = int.TryParse(_config["Kafka:BatchSize"], out var size) ? size : 10;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer!.Consume(stoppingToken);

                    if (result == null)
                    {
                        _logger.LogWarning("Kafka returned null consume result.");
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
                await _eventRepository.SaveBatchAsync(_eventBuffer, stoppingToken);
            }

            if (_comicRecordBuffer.Count > 0)
            {
                _logger.LogInformation("Final comic buffer flush: {Count}", _comicRecordBuffer.Count);
                await _comicCollectionRepository.UpsertBatchAsync(_comicRecordBuffer, stoppingToken);
            }

            _consumer?.Close();
            _consumer?.Dispose();

            _logger.LogInformation("KafkaComicListener shut down gracefully");
        }
    }
}
