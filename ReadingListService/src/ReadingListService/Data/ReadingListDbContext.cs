using Microsoft.EntityFrameworkCore;
using ReadingListService.Models;

namespace ReadingListService.Data;

public class ReadingListDbContext : DbContext
{
    public ReadingListDbContext(DbContextOptions<ReadingListDbContext> options)
        : base(options)
    {
    }

    public DbSet<ComicRecord> ComicCollection { get; set; } = null!;
    public DbSet<ReadingProgress> ReadingProgress { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ComicRecord>(entity =>
        {
            entity.HasOne(c => c.ReadingProgress)
                .WithOne(p => p.Comic)
                .HasForeignKey<ReadingProgress>(p => p.ComicId);
        });

        modelBuilder.Entity<ReadingProgress>(entity =>
        {
            entity.HasKey(p => p.ComicId);
        });
    }
}
