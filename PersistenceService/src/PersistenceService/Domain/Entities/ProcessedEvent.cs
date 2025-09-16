using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PersistenceService.Domain.Entities;

[Table("processedevents")]
public class ProcessedEvent
{
    [Key]
    public Guid EventId { get; set; }

    public DateTime ProcessedAtUtc { get; set; }
}

