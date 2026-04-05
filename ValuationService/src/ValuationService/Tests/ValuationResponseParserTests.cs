using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PersistenceService.Infrastructure;
using SharedLibrary.Models;
using ValuationService.Infrastructure;
using ValuationService.Service;
using Xunit;

namespace ValuationService.Tests;

public class ValuationBackgroundWorkerTests
{
    private static ComicDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ComicDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new ComicDbContext(options);
    }

    private static ValuationBackgroundWorker CreateWorker(IServiceProvider provider, IMcpClientWrapper mcpClient)
    {
        var logger = Mock.Of<ILogger<ValuationBackgroundWorker>>();
        return new ValuationBackgroundWorker(provider, logger, mcpClient);
    }

    [Fact]
    public async Task ExecuteAsync_ProcessesRecord_AndUpdatesValue()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var dbName = Guid.NewGuid().ToString();
        var dbContext = CreateDbContext(dbName);

        var recordToAdd = new ComicRecordEntity
        {
            SeriesName = "Amazing Spider-Man",
            FullTitle = "Amazing Spider-Man #1",
            PublisherName = "Marvel",
            Value = null,
            LastUpdatedUtc = null
        };
        dbContext.ComicRecords.Add(recordToAdd);
        await dbContext.SaveChangesAsync();

        services.AddSingleton(dbContext);

        var provider = services.BuildServiceProvider();

        var mcp = new Mock<IMcpClientWrapper>();
        mcp.Setup(x => x.ExecuteResearch(It.IsAny<string>()))
           .ReturnsAsync("""
           {
             "result": "123.45"
           }
           """);

        var worker = CreateWorker(provider, mcp.Object);

        using var cts = new CancellationTokenSource();
        var task = worker.StartAsync(cts.Token);
        
        // Wait a bit for it to process
        await Task.Delay(200);
        await cts.CancelAsync();
        try { await task; } catch (OperationCanceledException) { }

        // Refresh record from database
        var updated = await dbContext.ComicRecords.FindAsync(recordToAdd.Id);
        Assert.NotNull(updated);
        Assert.Equal(123.45m, updated.Value);
        Assert.NotNull(updated.LastUpdatedUtc);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotUpdate_WhenResponseCannotBeParsed()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var dbName = Guid.NewGuid().ToString();
        var dbContext = CreateDbContext(dbName);

        dbContext.ComicRecords.Add(new ComicRecordEntity
        {
            SeriesName = "Batman",
            FullTitle = "Batman #1",
            PublisherName = "DC",
            Value = null,
            LastUpdatedUtc = null
        });
        await dbContext.SaveChangesAsync();

        services.AddSingleton(dbContext);

        var provider = services.BuildServiceProvider();

        var mcp = new Mock<IMcpClientWrapper>();
        mcp.Setup(x => x.ExecuteResearch(It.IsAny<string>()))
           .ReturnsAsync("""
           {
             "result": "unknown"
           }
           """);

        var worker = CreateWorker(provider, mcp.Object);

        using var cts = new CancellationTokenSource();
        var task = worker.StartAsync(cts.Token);
        
        // Wait a bit for it to process
        await Task.Delay(200);
        await cts.CancelAsync();
        try { await task; } catch (OperationCanceledException) { }

        // Refresh record from database
        var record = await dbContext.ComicRecords.FirstAsync();
        Assert.Null(record.Value);
        Assert.Null(record.LastUpdatedUtc);
    }
}
