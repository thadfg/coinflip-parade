using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReadingListService.Models;

[Table("comiccollection", Schema = "comics")]
public class ComicRecord
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }
    
    [Required]
    [Column("Full Title")]
    public string FullTitle { get; set; } = string.Empty;
    
    [Column("Series")]
    public string? SeriesName { get; set; }
    
    [Column("Publisher")]
    public string? PublisherName { get; set; }
    
    [Column("Issue")]
    public string? IssueNumber { get; set; }
    
    [Column("Release Date")]
    public DateTime? ReleaseDate { get; set; }

    [Column("Variant Description")]
    public string? VariantDescription { get; set; }

    [Column("coverartpath")]
    public string? CoverArtPath { get; set; }

    [Column("importedat")]
    public DateTime? ImportedAt { get; set; }

    [Column("Barcode")]
    public string? Barcode { get; set; }

    [Column("Format")]
    public string? Format { get; set; }

    [Column("Key")]
    public string? Key { get; set; }
    
    [NotMapped]
    public bool InCollection { get; set; }
    
    // Navigation property
    public ReadingProgress? ReadingProgress { get; set; }
}
