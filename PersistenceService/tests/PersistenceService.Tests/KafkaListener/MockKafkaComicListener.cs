using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PersistenceService.Application.Interfaces;
using PersistenceService.Infrastructure.Kafka;


namespace PersistenceService.Tests.KafkaListener;

public class MockKafkaComicListener : KafkaComicListener
{
    private readonly IConsumer<Ignore, string> _mockConsumer;

    public MockKafkaComicListener(
        ILogger<KafkaComicListener> logger,
        IConfiguration config,
        IEventRepository eventRepository,
        IComicCollectionRepository comicCollectionRepository,
        IKafkaLogHelper kafkaLogHelper,
        IConsumer<Ignore, string> mockConsumer)
        : base(logger, config, eventRepository, comicCollectionRepository, kafkaLogHelper)
    {
        _mockConsumer = mockConsumer;
    }

    protected override IConsumer<Ignore, string> CreateConsumer()
    {
        return _mockConsumer;
    }
}
