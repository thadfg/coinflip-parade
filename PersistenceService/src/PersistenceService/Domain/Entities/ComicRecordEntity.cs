using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PersistenceService.Domain.Entities;

[Table("comiccollection")]
[Index(nameof(ReleaseDate))]
[Index(nameof(ImportedAt))]
public class ComicRecordEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("publishername")]
    public string PublisherName { get; set; }

    [Required]
    [Column ("seriesname")]
    public string SeriesName { get; set; }

    [Required]
    [Column("fulltitle")]
    public string FullTitle { get; set; }

    [Required]
    [Column("releasedate")]
    public DateTime ReleaseDate { get; set; }

    [Column("incollection")]
    public string? InCollection { get; set; }

    [Column("value")]
    public decimal? Value { get; set; }

    [Column("coverartpath")]public string CoverArtPath { get; set; }

    [Required]
    [Column("importedat")]
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
}
