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

        modelBuilder.HasDefaultSchema("comics");

        modelBuilder.Entity<ComicRecord>(entity =>
        {
            entity.ToTable("comiccollection", "comics");
            
            entity.Property(e => e.FullTitle).HasColumnName("Full Title");
            entity.Property(e => e.SeriesName).HasColumnName("Series");
            entity.Property(e => e.PublisherName).HasColumnName("Publisher");
            entity.Property(e => e.IssueNumber).HasColumnName("Issue");
            entity.Property(e => e.ReleaseDate).HasColumnName("Release Date");
            entity.Property(e => e.VariantDescription).HasColumnName("Variant Description");
            entity.Property(e => e.CoverArtPath).HasColumnName("coverartpath");
            entity.Property(e => e.ImportedAt).HasColumnName("importedat");
            entity.Property(e => e.Barcode).HasColumnName("Barcode");
            entity.Property(e => e.Format).HasColumnName("Format");
            entity.Property(e => e.Key).HasColumnName("Key");

            entity.HasOne(c => c.ReadingProgress)
                .WithOne(p => p.Comic)
                .HasForeignKey<ReadingProgress>(p => p.ComicId);
        });

        modelBuilder.Entity<ReadingProgress>(entity =>
        {
            entity.ToTable("reading_progress", "comics");
            entity.HasKey(p => p.ComicId);
        });
    }
}
