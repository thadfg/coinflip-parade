namespace IngestionService.Infrastructure.Kafka;

public class KafkaSettings
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string ClientId { get; set; } = "IngestionService";
    // Add other config options as needed
}
