using System.Globalization;
using CsvHelper;
using IngestionService.Application.Facets;
using SharedLibrary.Kafka;
using IngestionService.Domain.Models;
using Facet.Extensions;


namespace IngestionService.Application.Services;

public class ComicCsvIngestor
{
    private readonly IKafkaProducer _producer;

    public ComicCsvIngestor(IKafkaProducer producer)
    {
        _producer = producer;
    }

    public async Task IngestAsync(string csvPath)
    {
        var importId = Guid.NewGuid();

        using var reader = new StreamReader(csvPath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var records = csv.GetRecords<ComicCsvRecord>().ToList();

        foreach (var record in records)
        {
            //var facet = record.MapTo<ComicImportFacet>(); // 👈 Facet.Extensions in action
            var comicEvent = record.ToFacet<ComicCsvRecord, ComicCsvRecordDto>();

            await _producer.ProduceAsync("comic-imported", comicEvent);
        }
    }
}
