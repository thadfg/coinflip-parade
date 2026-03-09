using Microsoft.EntityFrameworkCore;
using Polly;
using Npgsql;
using System.Net.Sockets;

namespace PersistenceService.Infrastructure.Database;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        // Define a policy: Retry 5 times, doubling the wait time between each (2s, 4s, 8s...)
        var retryPolicy = Policy
            .Handle<NpgsqlException>() // Specifically handle Postgres connection issues
            .Or<SocketException>()
            .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($"Database not ready. Retry {retryCount} in {timeSpan.TotalSeconds}s...");
                });

        await retryPolicy.ExecuteAsync(async () =>
        {
            var eventDb = scope.ServiceProvider.GetRequiredService<EventDbContext>();
            await eventDb.Database.MigrateAsync();

            var comicDb = scope.ServiceProvider.GetRequiredService<ComicCollectionDbContext>();
            await comicDb.Database.MigrateAsync();
        });
    }
}
