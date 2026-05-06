namespace PersistenceService.Config;

public class RepositoryOptions
{
    public int MaxRetries { get; set; } = 3;
    public int DelayMilliseconds { get; set; } = 500;
}
