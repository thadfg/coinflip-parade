using PersistenceService.Domain.Entities;
using SharedLibrary.Models;

namespace PersistenceService.Application.Mappers;

public static class ComicRecordMapper
{
    public static ComicRecordEntity ToEntity(KafkaEnvelope<ComicRecordDto> envelope)
    {
        if (envelope?.Payload == null)
            throw new ArgumentNullException(nameof(envelope.Payload), "Payload cannot be null");

        var dto = envelope.Payload;

        return new ComicRecordEntity
        {
            Id = Guid.NewGuid(),
            PublisherName = dto.PublisherName,
            SeriesName = dto.SeriesName,
            FullTitle = dto.FullTitle,
            ReleaseDate = dto.ReleaseDate,
            InCollection = dto.InCollection,
            Value = dto.Value,
            CoverArtPath = dto.CoverArtPath,
            ImportedAt = envelope.Timestamp
        };
    }
}
