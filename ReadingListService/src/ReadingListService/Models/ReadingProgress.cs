using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReadingListService.Models;

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
