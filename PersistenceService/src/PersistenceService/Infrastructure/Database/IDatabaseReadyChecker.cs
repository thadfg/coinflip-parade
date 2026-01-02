namespace PersistenceService.Infrastructure.Database
{
    public interface IDatabaseReadyChecker
    {
        Task<bool> IsReadyAsync(CancellationToken cancellationToken = default);
    }
}
