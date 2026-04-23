using PersistenceService.Domain.Entities;
using SharedLibrary.Facet;
using SharedLibrary.Models;
using System;
using Microsoft.JSInterop.Infrastructure;

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
        
        dto.IssueNumber = dto.IssueNumber ?? "Unknown";

        var fullTitle = dto.FullTitle != null 
            ? dto.SeriesName + " " + dto.IssueNumber + " " + dto.FullTitle 
            : dto.SeriesName + " " + dto.IssueNumber;

        if (!DateTime.TryParse(dto.ReleaseDate, out var parsedReleaseDate))
            throw new FormatException($"Invalid ReleaseDate format: '{dto.ReleaseDate}'");

        return new ComicRecordEntity
        {
            Id = comicId,
            Series = dto.SeriesName,
            Issue = dto.IssueNumber, 
            Publisher = dto.PublisherName,
            ReleaseDate = parsedReleaseDate,
            Format =dto.Format, 
            Barcode = dto.Barcode.ToString() ?? "0", 
            FullTitle = fullTitle,
            CoverArtPath = dto.CoverArtPath ?? string.Empty,
            ImportedAt = envelope.Timestamp,
            KeyStatus = dto.Key ?? string.Empty
        };
    }
}
