using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PersistenceService.Domain.Entities;

[Table("events")]
public class EventEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("aggregate_id")]
    public Guid AggregateId { get; set; }

    [Column("event_type")]
    public string EventType { get; set; } = default!;

    [Column("event_data", TypeName = "jsonb")]
    public string EventData { get; set; } = default!;

    [Column("occurred_at")]
    public DateTimeOffset OccurredAt { get; set; }
}
