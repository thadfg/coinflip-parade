using PersistenceService.Domain.Entities;
using SharedLibrary.Facet;
using SharedLibrary.Models;
using System;

namespace PersistenceService.Application.Mappers;

public static class ComicRecordMapper
{
    public static ComicRecordEntity ToEntity(KafkaEnvelope<ComicCsvRecordDto> envelope)
    {
        if (envelope?.Payload == null)
            throw new ArgumentNullException(nameof(envelope.Payload), "Payload cannot be null");

        var dto = envelope.Payload;

        if (!DateTime.TryParse(dto.ReleaseDate, out var parsedReleaseDate))
            throw new FormatException($"Invalid ReleaseDate format: '{dto.ReleaseDate}'");

        return new ComicRecordEntity
        {
            Id = Guid.NewGuid(),
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
