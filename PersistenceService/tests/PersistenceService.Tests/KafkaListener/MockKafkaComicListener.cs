using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PersistenceService.Application.Interfaces;


namespace PersistenceService.Tests.KafkaListener;

public class MockKafkaComicListener : KafkaComicListener
{
    private readonly IConsumer<Ignore, string> _mockConsumer;

    public MockKafkaComicListener(
        ILogger<KafkaComicListener> logger,
        IConfiguration config,
        IEventRepository eventRepository,
        IConsumer<Ignore, string> mockConsumer)
        : base(logger, config, eventRepository)
    {
        _mockConsumer = mockConsumer;
    }

    protected override IConsumer<Ignore, string> CreateConsumer()
    {
        return _mockConsumer;
    }
}
