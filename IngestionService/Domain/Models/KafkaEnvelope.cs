namespace IngestionService.Domain.Models;

public class KafkaEnvelope<T>
{
    public string ImportId { get; set; }
    public DateTime Timestamp { get; set; }
    public T Payload { get; set; }
}
