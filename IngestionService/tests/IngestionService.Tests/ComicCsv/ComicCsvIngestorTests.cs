using System.Globalization;
using IngestionService.Application.Facets;
using IngestionService.Application.Services;
using IngestionService.Domain.Models;
using Moq;
using SharedLibrary.Kafka;
using Xunit;

public class ComicCsvIngestorTests
{
    [Fact]
    public async Task IngestAsync_ValidRecord_ProducesToKafka()
    {
        // Arrange
        var mockProducer = new Mock<IKafkaProducer>();
        var ingestor = new ComicCsvIngestor(mockProducer.Object);

        // Create a valid CSV file
        var csvContent = "Publisher Name,Series Name,Full Title,Release Date,In Collection\n" +
                         "Marvel,Spider-Man,The Amazing Spider-Man,2024-01-01,Yes";
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, csvContent);

        // Act
        await ingestor.IngestAsync(tempFile);

        // Assert
        mockProducer.Verify(p => p.ProduceAsync(
            "comic-imported",
            It.IsAny<string>(),
            It.IsAny<KafkaEnvelope<ComicCsvRecordDto>>()),
            Times.Once);

        // Cleanup
        File.Delete(tempFile);
    }

    [Fact]
    public async Task IngestAsync_InvalidRecord_ProducesToDeadLetter()
    {
        // Arrange
        var mockProducer = new Mock<IKafkaProducer>();
        var ingestor = new ComicCsvIngestor(mockProducer.Object);

        // Missing required Release Date
        var csvContent = "Publisher Name,Series Name,Full Title,Release Date,In Collection\n" +
                         "Marvel,Spider-Man,The Amazing Spider-Man,,Yes";
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, csvContent);

        // Act
        await ingestor.IngestAsync(tempFile);

        // Assert
        mockProducer.Verify(p => p.ProduceAsync(
            "comic-ingestion-dead-letter",
            It.IsAny<string>(),
            It.IsAny<DeadLetterEnvelope<ComicCsvRecord>>()),
            Times.AtLeastOnce);

        // Cleanup
        File.Delete(tempFile);
    }
}