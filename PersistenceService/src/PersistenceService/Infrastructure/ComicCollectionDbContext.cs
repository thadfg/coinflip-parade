using Microsoft.EntityFrameworkCore;
using PersistenceService.Domain.Entities;
using PersistenceService.Infrastructure.Configurations;

namespace PersistenceService.Infrastructure;

public class ComicCollectionDbContext : DbContext
{    
    public ComicCollectionDbContext(DbContextOptions<ComicCollectionDbContext> options)
        : base(options) { }

    public DbSet<ComicRecordEntity> ComicRecords => Set<ComicRecordEntity>();
    public DbSet<ProcessedEvent> ProcessedEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("comics");        

        modelBuilder.Entity<ProcessedEvent>().HasKey(p =>  p.Id);

        modelBuilder.Entity<ProcessedEvent>()
            .HasIndex(p => p.EventId)
            .IsUnique();

        modelBuilder.Entity<ProcessedEvent>()
            .Property(p => p.ProcessedAtUtc)
            .IsRequired();

        modelBuilder.ApplyConfiguration(new ComicRecordConfiguration());
    }
}
