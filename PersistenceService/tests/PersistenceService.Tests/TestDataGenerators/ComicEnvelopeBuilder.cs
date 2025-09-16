using SharedLibrary.Facet;
using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersistenceService.Tests.TestDataGenerators;

public static class ComicEnvelopeBuilder
{
    public static KafkaEnvelope<ComicCsvRecordDto> Build(Guid importId)
    {
        return new KafkaEnvelope<ComicCsvRecordDto>
        {
            ImportId = importId.ToString(),
            Payload = new ComicCsvRecordDto
            {
                PublisherName = "Dark Horse",
                SeriesName = "Hellboy",
                FullTitle = "Hellboy: Seed of Destruction",
                ReleaseDate = "1994-03-01",
                InCollection = "Yes",
                Value = (decimal)12.99,
                CoverArtPath = "/covers/hellboy1.jpg"
            }
        };
    }
}

