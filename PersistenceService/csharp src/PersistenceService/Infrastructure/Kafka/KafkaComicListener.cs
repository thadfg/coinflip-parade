public class KafkaComicListener : BackgroundService
{
    private readonly PersistenceService.Infrastructure.Kafka.KafkaLogHelper _kafkaLogHelper;
    // ...

    public KafkaComicListener(
        ILogger<KafkaComicListener> logger,
        IConfiguration config,
        IEventRepository eventRepository,
        IComicCollectionRepository comicCollectionRepository,
        PersistenceService.Infrastructure.Kafka.KafkaLogHelper kafkaLogHelper,
        IConsumer<Ignore, string>? consumer = null)
    {
        _logger = logger;
        _config = config;
        _eventRepository = eventRepository;
        _comicCollectionRepository = comicCollectionRepository;
        _consumer = consumer;
        _kafkaLogHelper = kafkaLogHelper;
    }

    // example usage inside your loop:
    await _kafkaLogHelper.LogToKafkaAsync("Information", "Mapped event entity: " + eventEntity.EventType);
}