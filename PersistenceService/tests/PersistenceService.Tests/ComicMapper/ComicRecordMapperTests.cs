using PersistenceService.Application.Mappers;
using SharedLibrary.Models;

public class ComicRecordMapperTests
{
    [Fact]
    public void ToEntity_ValidEnvelope_MapsAllFieldsCorrectly()
    {
        var timestamp = DateTime.UtcNow;

        var envelope = new KafkaEnvelope<ComicRecordDto>
        {
            ImportId = "import-001",
            Timestamp = timestamp,
            Payload = new ComicRecordDto
            {
                PublisherName = "Marvel",
                SeriesName = "X-Men",
                FullTitle = "X-Men #1",
                ReleaseDate = new DateTime(1991, 10, 1),
                InCollection = "Yes",
                Value = 9.99m,
                CoverArtPath = "/covers/xmen1.jpg"
            }
        };

        var entity = ComicRecordMapper.ToEntity(envelope);

        Assert.Equal("Marvel", entity.PublisherName);
        Assert.Equal("X-Men", entity.SeriesName);
        Assert.Equal("X-Men #1", entity.FullTitle);
        Assert.Equal(new DateTime(1991, 10, 1), entity.ReleaseDate);
        Assert.Equal("Yes", entity.InCollection);
        Assert.Equal(9.99m, entity.Value);
        Assert.Equal("/covers/xmen1.jpg", entity.CoverArtPath);
        Assert.Equal(timestamp, entity.ImportedAt);
        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    [Fact]
    public void ToEntity_NullEnvelope_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ComicRecordMapper.ToEntity(null));
    }

    [Fact]
    public void ToEntity_NullPayload_ThrowsArgumentNullException()
    {
        var envelope = new KafkaEnvelope<ComicRecordDto>
        {
            ImportId = "import-002",
            Timestamp = DateTime.UtcNow,
            Payload = null
        };

        Assert.Throws<ArgumentNullException>(() => ComicRecordMapper.ToEntity(envelope));
    }

    [Fact]
    public void ToEntity_MissingRequiredFields_MapsWithDefaultsOrThrows()
    {
        var envelope = new KafkaEnvelope<ComicRecordDto>
        {
            ImportId = "import-003",
            Timestamp = DateTime.UtcNow,
            Payload = new ComicRecordDto
            {
                PublisherName = "", // Invalid
                SeriesName = null,  // Invalid
                FullTitle = "Untitled",
                ReleaseDate = DateTime.MinValue,
                InCollection = null,
                Value = null,
                CoverArtPath = ""
            }
        };

        var entity = ComicRecordMapper.ToEntity(envelope);

        Assert.Equal("Untitled", entity.FullTitle);
        Assert.Equal(DateTime.MinValue, entity.ReleaseDate);
        Assert.Null(entity.InCollection);
        Assert.Null(entity.Value);
        Assert.Equal("", entity.CoverArtPath);
    }
}
