namespace PersistenceService.Config;

public class RepositoryOptions
{
    public int MaxRetries { get; set; } = 3;
    public int DelayMilliseconds { get; set; } = 500;
    public int DbMaxRetryCount { get; set; } = 5;
    public int DbMaxRetryDelaySeconds { get; set; } = 30;
}
