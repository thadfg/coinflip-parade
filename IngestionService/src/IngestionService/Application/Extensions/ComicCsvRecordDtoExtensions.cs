using IngestionService.Application.Facets;
using IngestionService.Application.Models;
using SharedLibrary.Constants;
using System.Diagnostics.Metrics;


namespace IngestionService.Application.Extensions
{
    public static class ComicCsvRecordDtoExtensions
    {
        private static readonly Meter Meter = new(MeterNames.ComicIngestion);
        private static readonly Counter<int> InvalidBoolMetricCounter = Meter.CreateCounter<int>("csv.ingestion.invalid_bool.in_collection");
        private static readonly Counter<int> InvalidDateMetricFailureCounter = Meter.CreateCounter<int>("csv.ingestion.invalid_release_date");


        public static ComicCsvRecordMappedDto ToMappedDto(this ComicCsvRecordDto dto)
        {
            var mapped = new ComicCsvRecordMappedDto
            {
                PublisherName = dto.PublisherName,
                SeriesName = dto.SeriesName,
                FullTitle = dto.FullTitle,                
                InCollection = bool.TryParse(dto.InCollection, out var inCollection)
                    ? inCollection
                    : EmitInvalidBoolMetric(),
                ReleaseDate = DateTime.TryParse(dto.ReleaseDate, out var parsed)
                    ? parsed
                    : EmitInvalidDateMetric() // Or log/throw if you prefer strict validation
            };

            return mapped;
        }
        private static bool? EmitInvalidBoolMetric()
        {
            InvalidBoolMetricCounter.Add(1, new KeyValuePair<string, object?>("Service", "ComicCsvIngestorService"), new KeyValuePair<string, object?>("Source", "UserUpload"));
            return null;
        }

        private static DateTime EmitInvalidDateMetric()
        {
            InvalidDateMetricFailureCounter.Add(1, new KeyValuePair<string, object?>("Service", "ComicCsvIngestorService"), new KeyValuePair<string, object?>("Source", "UserUpload"));            
            return DateTime.MinValue;
        }
    }
}
