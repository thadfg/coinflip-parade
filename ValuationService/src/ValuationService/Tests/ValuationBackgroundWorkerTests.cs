using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PersistenceService.Infrastructure;
using SharedLibrary.Models;
using ValuationService.Infrastructure;
using ValuationService.Service;
using Xunit;
using System.Diagnostics.Metrics;

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

    private static ValuationBackgroundWorker CreateWorker(IServiceProvider provider, IMcpClientWrapper mcpClient, ValuationControlService controlService)
    {
        var logger = Mock.Of<ILogger<ValuationBackgroundWorker>>();
        var meterFactory = new Mock<IMeterFactory>();
        var meter = new Meter("ValuationService");
        meterFactory.Setup(x => x.Create(It.IsAny<MeterOptions>())).Returns(meter);

        return new ValuationBackgroundWorker(provider, logger, mcpClient, controlService, meterFactory.Object);
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

        var controlService = new ValuationControlService();
        controlService.Start();

        var worker = CreateWorker(provider, mcp.Object, controlService);

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

        var controlService = new ValuationControlService();
        controlService.Start();

        var worker = CreateWorker(provider, mcp.Object, controlService);

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

    [Fact]
    public async Task ExecuteAsync_RespectsBatchSize()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var dbName = Guid.NewGuid().ToString();
        var dbContext = CreateDbContext(dbName);

        // Add 15 records that need update
        for (int i = 0; i < 15; i++)
        {
            dbContext.ComicRecords.Add(new ComicRecordEntity
            {
                SeriesName = "Marvel Series",
                FullTitle = $"Comic #{i}",
                PublisherName = "Marvel",
                Value = null
            });
        }
        await dbContext.SaveChangesAsync();

        services.AddSingleton(dbContext);
        var provider = services.BuildServiceProvider();

        var mcp = new Mock<IMcpClientWrapper>();
        mcp.Setup(x => x.ExecuteResearch(It.IsAny<string>()))
           .ReturnsAsync("{\"result\": \"10.0\"}");

        var controlService = new ValuationControlService();
        controlService.Start();

        var worker = CreateWorker(provider, mcp.Object, controlService);

        using var cts = new CancellationTokenSource();
        var task = worker.StartAsync(cts.Token);
        
        await Task.Delay(500); // Give it time to process one batch
        await cts.CancelAsync();
        try { await task; } catch (OperationCanceledException) { }

        // It should have processed exactly 10 records in the first batch
        var updatedCount = await dbContext.ComicRecords.CountAsync(r => r.Value != null);
        Assert.Equal(10, updatedCount);
    }

    [Fact]
    public async Task ExecuteAsync_SkipsRecentRecords()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var dbName = Guid.NewGuid().ToString();
        var dbContext = CreateDbContext(dbName);

        // Record updated 5 days ago (recent)
        dbContext.ComicRecords.Add(new ComicRecordEntity
        {
            SeriesName = "Marvel",
            PublisherName = "Marvel",
            FullTitle = "Recent Comic",
            Value = 50.0m,
            LastUpdatedUtc = DateTime.UtcNow.AddDays(-5)
        });
        
        // Record never updated
        dbContext.ComicRecords.Add(new ComicRecordEntity
        {
            SeriesName = "Marvel",
            PublisherName = "Marvel",
            FullTitle = "Old Comic",
            Value = null,
            LastUpdatedUtc = null
        });

        await dbContext.SaveChangesAsync();

        services.AddSingleton(dbContext);
        var provider = services.BuildServiceProvider();

        var mcp = new Mock<IMcpClientWrapper>();
        mcp.Setup(x => x.ExecuteResearch(It.IsAny<string>()))
           .ReturnsAsync("{\"result\": \"100.0\"}");

        var controlService = new ValuationControlService();
        controlService.Start();

        var worker = CreateWorker(provider, mcp.Object, controlService);

        using var cts = new CancellationTokenSource();
        var task = worker.StartAsync(cts.Token);
        
        await Task.Delay(200);
        await cts.CancelAsync();
        try { await task; } catch (OperationCanceledException) { }

        var recent = await dbContext.ComicRecords.FirstAsync(r => r.FullTitle == "Recent Comic");
        var old = await dbContext.ComicRecords.FirstAsync(r => r.FullTitle == "Old Comic");

        Assert.Equal(50.0m, recent.Value); // Unchanged
        Assert.Equal(100.0m, old.Value);   // Updated
    }

    [Fact]
    public async Task ExecuteAsync_HandlesMcpExceptionGracefully()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var dbName = Guid.NewGuid().ToString();
        var dbContext = CreateDbContext(dbName);

        dbContext.ComicRecords.Add(new ComicRecordEntity
        {
            SeriesName = "Marvel",
            PublisherName = "Marvel",
            FullTitle = "Problematic Comic",
            Value = null
        });
        await dbContext.SaveChangesAsync();

        services.AddSingleton(dbContext);
        var provider = services.BuildServiceProvider();

        var mcp = new Mock<IMcpClientWrapper>();
        mcp.Setup(x => x.ExecuteResearch(It.IsAny<string>()))
           .ThrowsAsync(new Exception("MCP failed"));

        var controlService = new ValuationControlService();
        controlService.Start();

        var worker = CreateWorker(provider, mcp.Object, controlService);

        using var cts = new CancellationTokenSource();
        var task = worker.StartAsync(cts.Token);
        
        await Task.Delay(200);
        await cts.CancelAsync();
        try { await task; } catch (OperationCanceledException) { }

        var record = await dbContext.ComicRecords.FirstAsync();
        Assert.Null(record.Value); // Not updated, but worker didn't crash
    }
}
