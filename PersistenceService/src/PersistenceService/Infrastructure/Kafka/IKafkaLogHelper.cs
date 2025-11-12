namespace PersistenceService.Infrastructure.Kafka
{
    public interface IKafkaLogHelper
    {
        Task LogToKafkaAsync(string level, string message, Exception? ex = null, string? correlationId = null);
    }
}