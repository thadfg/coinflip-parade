using PersistenceService.Domain.Entities;
using SharedLibrary.Facet;
using SharedLibrary.Models;
using System;

namespace PersistenceService.Application.Mappers;

public static class ComicRecordMapper
{
    public static ComicRecordEntity ToEntity(KafkaEnvelope<ComicCsvRecordDto> envelope, string kafkaMessageKey)
    {
        if (envelope?.Payload == null)
            throw new ArgumentNullException(nameof(envelope.Payload), "Payload cannot be null");

        if (string.IsNullOrWhiteSpace(kafkaMessageKey))
            throw new ArgumentException("Kafka message key cannot be null or empty.", nameof(kafkaMessageKey));

        if (!Guid.TryParse(kafkaMessageKey, out var comicId))
            throw new FormatException($"Kafka message key is not a valid GUID: '{kafkaMessageKey}'");

        var dto = envelope.Payload;

        if (!DateTime.TryParse(dto.ReleaseDate, out var parsedReleaseDate))
            throw new FormatException($"Invalid ReleaseDate format: '{dto.ReleaseDate}'");

        return new ComicRecordEntity
        {
            Id = comicId,
            PublisherName = dto.PublisherName,
            SeriesName = dto.SeriesName,
            FullTitle = dto.FullTitle,
            ReleaseDate = parsedReleaseDate,
            InCollection = dto.InCollection,
            Value = dto.Value,  // if Null that means the value has not been pulled yet not yet appraised.
            CoverArtPath = dto.CoverArtPath ?? string.Empty,
            ImportedAt = envelope.Timestamp
        };
    }
}
