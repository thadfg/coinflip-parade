using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace PersistenceService.Infrastructure
{
    public class DesignTimeComicCollectionDbContextFactory : IDesignTimeDbContextFactory<ComicCollectionDbContext>
    {
        public ComicCollectionDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ComicCollectionDbContext>();
            optionsBuilder.UseNpgsql(config.GetConnectionString("ComicCollectionDb"));

            return new ComicCollectionDbContext(optionsBuilder.Options);
        }
    }
}
