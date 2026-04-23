using PersistenceService.Application.Mappers;
using SharedLibrary.Facet;
using SharedLibrary.Models;

public class ComicRecordMapperTests
{
    [Fact]
    public void ToEntity_ValidEnvelope_MapsAllFieldsCorrectly()
    {
        var timestamp = DateTime.UtcNow;
        var kafkaMessageKey = Guid.NewGuid().ToString();

        var envelope = new KafkaEnvelope<ComicCsvRecordDto>
        {
            ImportId = "import-001",
            Timestamp = timestamp,
            Payload = new ComicCsvRecordDto
            {
                PublisherName = "Marvel",
                SeriesName = "X-Men",
                IssueNumber = "1",
                FullTitle = "The Uncanny X-Men",
                ReleaseDate = "1991-10-01",
                InCollection = "Yes",
                Value = 9.99m,
                CoverArtPath = "/covers/xmen1.jpg"
            }
        };

        var entity = ComicRecordMapper.ToEntity(envelope, kafkaMessageKey);

        Assert.Equal(Guid.Parse(kafkaMessageKey), entity.Id);
        Assert.Equal("Marvel", entity.Publisher);
        Assert.Equal("X-Men", entity.Series);
        Assert.Equal("X-Men1The Uncanny X-Men", entity.FullTitle);
        Assert.Equal(new DateTime(1991, 10, 1), entity.ReleaseDate);
        Assert.Equal("Yes", entity.KeyStatus);
        Assert.Equal("/covers/xmen1.jpg", entity.CoverArtPath);
        Assert.Equal(timestamp, entity.ImportedAt);
    }

    [Fact]
    public void ToEntity_NullEnvelope_ThrowsArgumentNullException()
    {
        var kafkaMessageKey = Guid.NewGuid().ToString();

        Assert.Throws<ArgumentNullException>(() => ComicRecordMapper.ToEntity(null, kafkaMessageKey));
    }

    [Fact]
    public void ToEntity_NullPayload_ThrowsArgumentNullException()
    {
        var kafkaMessageKey = Guid.NewGuid().ToString();

        var envelope = new KafkaEnvelope<ComicCsvRecordDto>
        {
            ImportId = "import-002",
            Timestamp = DateTime.UtcNow,
            Payload = null
        };

        Assert.Throws<ArgumentNullException>(() => ComicRecordMapper.ToEntity(envelope, kafkaMessageKey));
    }

    [Fact]
    public void ToEntity_MissingRequiredFields_MapsWithDefaultsOrThrows()
    {
        var kafkaMessageKey = Guid.NewGuid().ToString();

        var envelope = new KafkaEnvelope<ComicCsvRecordDto>
        {
            ImportId = "import-003",
            Timestamp = DateTime.UtcNow,
            Payload = new ComicCsvRecordDto
            {
                PublisherName = "",
                SeriesName = "Series",
                IssueNumber = "1",
                FullTitle = "Untitled",
                ReleaseDate = "0001-01-01",
                InCollection = null,
                Value = null,
                CoverArtPath = ""
            }
        };

        var entity = ComicRecordMapper.ToEntity(envelope, kafkaMessageKey);

        Assert.Equal(Guid.Parse(kafkaMessageKey), entity.Id);
        Assert.Equal("Series1Untitled", entity.FullTitle);
        Assert.Equal(DateTime.MinValue, entity.ReleaseDate);
        Assert.Equal(string.Empty, entity.KeyStatus);
        Assert.Equal("", entity.CoverArtPath);
    }

    [Fact]
    public void ToEntity_InvalidKafkaMessageKey_ThrowsFormatException()
    {
        var envelope = new KafkaEnvelope<ComicCsvRecordDto>
        {
            ImportId = "import-004",
            Timestamp = DateTime.UtcNow,
            Payload = new ComicCsvRecordDto
            {
                PublisherName = "Marvel",
                SeriesName = "X-Men",
                FullTitle = "X-Men #1",
                ReleaseDate = "1991-10-01"
            }
        };

        Assert.Throws<FormatException>(() => ComicRecordMapper.ToEntity(envelope, "not-a-guid"));
    }
    [Fact]
    public void ToEntity_FullTitlePopulated_MapsWithSeriesIssueAndFullTitle()
    {
        var kafkaMessageKey = Guid.NewGuid().ToString();
        var envelope = new KafkaEnvelope<ComicCsvRecordDto>
        {
            ImportId = "import-005",
            Timestamp = DateTime.UtcNow,
            Payload = new ComicCsvRecordDto
            {
                PublisherName = "Marvel",
                SeriesName = "X-Men",
                IssueNumber = "1",
                FullTitle = "The Uncanny X-Men",
                ReleaseDate = "1991-10-01"
            }
        };

        var entity = ComicRecordMapper.ToEntity(envelope, kafkaMessageKey);

        Assert.Equal("X-Men1The Uncanny X-Men", entity.FullTitle);
    }

    [Fact]
    public void ToEntity_FullTitleNull_MapsWithSeriesAndIssueOnly()
    {
        var kafkaMessageKey = Guid.NewGuid().ToString();
        var envelope = new KafkaEnvelope<ComicCsvRecordDto>
        {
            ImportId = "import-006",
            Timestamp = DateTime.UtcNow,
            Payload = new ComicCsvRecordDto
            {
                PublisherName = "Marvel",
                SeriesName = "X-Men",
                IssueNumber = "1",
                FullTitle = null,
                ReleaseDate = "1991-10-01"
            }
        };

        var entity = ComicRecordMapper.ToEntity(envelope, kafkaMessageKey);

        Assert.Equal("X-Men1", entity.FullTitle);
    }
}
