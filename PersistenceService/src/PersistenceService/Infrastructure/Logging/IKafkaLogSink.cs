using System;
using System.Threading.Tasks;

namespace PersistenceService.Infrastructure.Logging;

public interface IKafkaLogSink
{
    Task LogToKafkaAsync(string level, string message, Exception? ex = null);
}