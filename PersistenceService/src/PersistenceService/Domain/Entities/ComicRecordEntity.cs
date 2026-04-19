using System;
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
    [Column("Id")]
    public Guid Id { get; set; }

    [Required]
    public string Series { get; set; }

    [Required]
    public string Issue { get; set; }

    [Column("Variant Description")]
    public string? VariantDescription { get; set; }

    [Required]
    public string Publisher { get; set; }

    [Required]
    [Column("Release Date")]
    [DataType(DataType.Date)]
    public DateTime ReleaseDate { get; set; }

    [Required]
    public string Format { get; set; }

    [Required]
    [MaxLength(20)]
    public string Barcode { get; set; }

    [Column("Full Title")]
    public string? FullTitle { get; set; }

    [Column("coverartpath")]public string? CoverArtPath { get; set; }
            
    [Required]
    [Column("importedat")]
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Maps to the 'Key' column in the CSV. 
    /// Named 'KeyStatus' to avoid conflict with the EF [Key] attribute.
    /// </summary>
    [Column("Key")]
    public string KeyStatus { get; set; }
}