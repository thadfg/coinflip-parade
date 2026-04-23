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
               .HasColumnName("Id")
               .HasColumnType("uuid");
        builder.Property(c => c.Series)
               .IsRequired()
               .HasColumnType("text");
        builder.Property(c => c.Issue)
               .IsRequired()
               .HasColumnType("text");
        builder.Property(c => c.VariantDescription)
               .HasColumnName("Variant Description")
               .HasColumnType("text");
        builder.Property(c => c.Publisher)
               .IsRequired()
               .HasColumnType("text");
        builder.Property(c => c.ReleaseDate)
               .IsRequired()
               .HasColumnName("Release Date")
               .HasColumnType("date");
        builder.Property(c => c.Format)
               .IsRequired()
               .HasColumnType("text");
        builder.Property(c => c.Barcode)
               .IsRequired()
               .HasMaxLength(20)
               .HasColumnType("text");
        builder.Property(c => c.FullTitle)
               .HasColumnName("Full Title")
               .HasColumnType("text");
        builder.Property(c => c.CoverArtPath)
               .HasColumnName("coverartpath")
               .HasColumnType("text");
        builder.Property(c => c.ImportedAt)
               .IsRequired()
               .HasColumnName("importedat")
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(c => c.KeyStatus)
               .HasColumnName("Key")
               .HasColumnType("text");
    }
}
