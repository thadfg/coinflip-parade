using Microsoft.EntityFrameworkCore;
//using PersistenceService.Domain.Entities;
//using PersistenceService.Infrastructure.Configurations;
using SharedLibrary.Models;


namespace PersistenceService.Infrastructure;

public class ComicDbContext : DbContext
{    
    public ComicDbContext(DbContextOptions<ComicDbContext> options)
        : base(options) { }

    public DbSet<ComicRecordEntity> ComicRecords => Set<ComicRecordEntity>();
    //public DbSet<ProcessedEvent> ProcessedEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("comics");
    }
}