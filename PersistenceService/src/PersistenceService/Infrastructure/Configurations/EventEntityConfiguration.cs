using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersistenceService.Domain.Entities;

namespace PersistenceService.Infrastructure.Configurations;

public class EventEntityConfiguration : IEntityTypeConfiguration<EventEntity>
{
    public void Configure(EntityTypeBuilder<EventEntity> builder)
    {
        builder.ToTable("events", "comics");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasColumnName("id")
               .HasColumnType("uuid");

        builder.Property(e => e.AggregateId)
               .HasColumnName("aggregate_id")
               .HasColumnType("uuid");

        builder.Property(e => e.EventType)
               .HasColumnName("event_type")
               .HasColumnType("text");

        builder.Property(e => e.EventData)
               .HasColumnName("event_data")
               .HasColumnType("jsonb");

        builder.Property(e => e.OccurredAt)
               .HasColumnName("occurred_at")
               .HasColumnType("timestamp with time zone");
    }
}
