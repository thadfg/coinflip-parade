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
        modelBuilder.Entity<ComicRecordEntity>()
            .HasKey(c => c.Id);

        modelBuilder.Entity<ProcessedEvent>().HasKey(p => p.EventId);

        modelBuilder.Entity<ProcessedEvent>()
            .HasIndex(p => p.EventId)
            .IsUnique();

        modelBuilder.Entity<ProcessedEvent>()
            .Property(p => p.ProcessedAtUtc)
            .IsRequired();

        

        modelBuilder.Entity<ComicRecordEntity>()
            .HasIndex(c => c.ReleaseDate);

        modelBuilder.Entity<ComicRecordEntity>()
            .HasIndex(c => c.ImportedAt);

        // Optional: Add concurrency token if needed
        modelBuilder.Entity<ComicRecordEntity>()
            .Property(c => c.ImportedAt)
            .IsConcurrencyToken();

        modelBuilder.ApplyConfiguration(new ComicRecordConfiguration());
    }
}
