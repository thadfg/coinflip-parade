using Microsoft.EntityFrameworkCore;

namespace PersistenceService.Infrastructure.Database;

public static class DbInitializer
{
    public static void Initialize(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var eventDb = scope.ServiceProvider.GetRequiredService<EventDbContext>();
        eventDb.Database.Migrate();

        var comicDb = scope.ServiceProvider.GetRequiredService<ComicCollectionDbContext>();
        comicDb.Database.Migrate();
    }
}
