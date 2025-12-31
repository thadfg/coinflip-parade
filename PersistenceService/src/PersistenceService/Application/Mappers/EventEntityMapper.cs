using PersistenceService.Domain.Entities;
using SharedLibrary.Facet;
using SharedLibrary.Models;
using System;
using System.Text.Json;

namespace PersistenceService.Application.Mappers;

public static class EventEntityMapper
{
    public static EventEntity FromPayload<T>(T payload, System.Guid aggregateId, string eventType)
    {
        return new EventEntity
        {
            Id = System.Guid.NewGuid(),
            AggregateId = aggregateId,
            EventType = eventType,
            EventData = JsonSerializer.Serialize(payload),
            OccurredAt = DateTimeOffset.UtcNow
        };
    }
}


