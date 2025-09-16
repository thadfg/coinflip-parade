using Microsoft.EntityFrameworkCore;
using PersistenceService.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistenceService.Tests.TestContexts;

public static class InMemoryComicDbContext
{
    public static ComicCollectionDbContext CreateComicDbContext()
    {
        var options = new DbContextOptionsBuilder<ComicCollectionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ComicCollectionDbContext(options);
    }

    public static EventDbContext CreateEventDbContext()
    {
        var options = new DbContextOptionsBuilder<EventDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EventDbContext(options);
    }

}
