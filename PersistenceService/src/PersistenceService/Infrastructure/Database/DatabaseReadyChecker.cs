using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PersistenceService.Infrastructure;
using PersistenceService.Infrastructure.Database;

public sealed class DatabaseReadyChecker : IDatabaseReadyChecker
{
    private readonly EventDbContext _eventDb;
    private readonly ComicCollectionDbContext _comicDb;
    private readonly ILogger<DatabaseReadyChecker> _logger;

    public DatabaseReadyChecker(
        EventDbContext eventDb,
        ComicCollectionDbContext comicDb,
        ILogger<DatabaseReadyChecker> logger)
    {
        _eventDb = eventDb;
        _comicDb = comicDb;
        _logger = logger;
    }

    public async Task<bool> IsReadyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Connectivity
            if (!await _eventDb.Database.CanConnectAsync(cancellationToken))
            {
                _logger.LogInformation("Event DB not reachable");
                SetNotReady();
                return false;
            }

            if (!await _comicDb.Database.CanConnectAsync(cancellationToken))
            {
                _logger.LogInformation("Comic DB not reachable");
                SetNotReady();
                return false;
            }

            // 2. Pending migrations
            var pendingEventMigrations = await _eventDb.Database.GetPendingMigrationsAsync(cancellationToken);
            var pendingComicMigrations = await _comicDb.Database.GetPendingMigrationsAsync(cancellationToken);

            if (pendingEventMigrations.Any() || pendingComicMigrations.Any())
            {
                _logger.LogInformation("Pending migrations detected");
                SetNotReady();
                return false;
            }

            // 3. Required tables exist
            if (!await RequiredTablesExistAsync(cancellationToken))
            {
                _logger.LogInformation("Required tables missing in comics schema");
                SetNotReady();
                return false;
            }

            SetReady();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database readiness check failed");
            SetNotReady();
            return false;
        }
    }

    private async Task<bool> RequiredTablesExistAsync(CancellationToken cancellationToken)
    {
        await using var connection = _comicDb.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(*) 
            FROM information_schema.tables
            WHERE table_schema = 'comics'
            AND table_name IN ('events', 'comiccollection', 'processedevents');";

        var result = await command.ExecuteScalarAsync(cancellationToken);
        var count = Convert.ToInt32(result);

        return count >= 3;
    }

    private void SetReady() => ReadinessMetrics.DatabaseReady.Set(1);
    private void SetNotReady() => ReadinessMetrics.DatabaseReady.Set(0);
}
