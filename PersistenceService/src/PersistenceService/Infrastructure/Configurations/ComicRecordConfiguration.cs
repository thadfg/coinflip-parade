using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersistenceService.Domain.Entities;

namespace PersistenceService.Infrastructure.Configurations;

public class ComicRecordConfiguration : IEntityTypeConfiguration<ComicRecordEntity>
{
    public void Configure(EntityTypeBuilder<ComicRecordEntity> builder)
    {
        builder.ToTable("comiccollection", "comics");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
               .HasColumnName("id")
               .HasColumnType("uuid");
        builder.Property(c => c.PublisherName)
               .IsRequired()
               .HasColumnName("publishername")
               .HasColumnType("text");
        builder.Property(c => c.SeriesName)
               .IsRequired()
               .HasColumnName("seriesname")
               .HasColumnType("text");
        builder.Property(c => c.FullTitle)
               .IsRequired()
               .HasColumnName("fulltitle")
               .HasColumnType("text");
        builder.Property(c => c.ReleaseDate)
               .IsRequired()
               .HasColumnName("releasedate")
               .HasColumnType("date");
        builder.Property(c => c.InCollection)
               .HasColumnName("incollection")
               .HasColumnType("text");
        builder.Property(c => c.Value)
               .HasColumnName("value")
               .HasColumnType("numeric");
        builder.Property(c => c.CoverArtPath)
               .HasColumnName("coverartpath")
               .HasColumnType("text");
        builder.Property(c => c.ImportedAt)
               .IsRequired()
               .HasColumnName("importedat")
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
