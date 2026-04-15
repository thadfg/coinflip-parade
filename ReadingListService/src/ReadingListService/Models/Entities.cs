using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReadingListService.Models;

[Table("comiccollection", Schema = "comics")]
public class ComicRecord
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public string FullTitle { get; set; } = string.Empty;
    
    public string? SeriesName { get; set; }
    
    public string? PublisherName { get; set; }
    
    public string? IssueNumber { get; set; }
    
    public DateTime? ReleaseDate { get; set; }
    
    public bool InCollection { get; set; }
    
    // Navigation property
    public ReadingProgress? ReadingProgress { get; set; }
}

[Table("reading_progress", Schema = "comics")]
public class ReadingProgress
{
    [Key]
    public Guid ComicId { get; set; }
    
    public bool IsRead { get; set; }
    
    public DateTime? ReadAtUtc { get; set; }
    
    // Relationship
    [ForeignKey(nameof(ComicId))]
    public ComicRecord? Comic { get; set; }
}
