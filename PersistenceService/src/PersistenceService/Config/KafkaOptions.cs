using Confluent.Kafka;

namespace PersistenceService.Config;

public class KafkaOptions
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public string Topic { get; set; } = "comic-imported";
    public string LogTopic { get; set; } = "service-logs";
    public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Earliest;
    public int BatchSize { get; set; } = 10;
    public int FlushIntervalSeconds { get; set; } = 20;
    public int ConsumeTimeoutMs { get; set; } = 10;
    public int ConsumeInitializeDelay { get; set; } = 1000;
    public int DatabaseReadyCheckDelaySeconds { get; set; } = 2;
    public int SessionTimeoutMs { get; set; } = 30000;
    public int SocketTimeoutMs { get; set; } = 60000;
    public int MaxPollIntervalMs { get; set; } = 300000;
    public int AutoCommitIntervalMs { get; set; } = 5000;
    public int LagUpdateIntervalSeconds { get; set; } = 5;
    
    // Logger specific
    public int LogBatchSize { get; set; } = 50;
    public int LogFlushIntervalSeconds { get; set; } = 2;
}
