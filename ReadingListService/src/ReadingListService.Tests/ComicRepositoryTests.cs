using Microsoft.EntityFrameworkCore;
using ReadingListService.Data;
using ReadingListService.Models;
using ReadingListService.Options;
using Microsoft.Extensions.Options;
using System.Globalization;
using Xunit;
using Moq;

namespace ReadingListService.Tests;

public class ComicRepositoryTests
{
    private IOptions<SearchOptions> GetSearchOptions()
    {
        var options = new SearchOptions
        {
            DefaultPageSize = 50,
            ExclusionPattern = "%Annual%"
        };
        var mock = new Mock<IOptions<SearchOptions>>();
        mock.Setup(m => m.Value).Returns(options);
        return mock.Object;
    }

    private ReadingListDbContext GetDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ReadingListDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        var context = new ReadingListDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task GetComicsByWeekOffsetAsync_ShouldJumpToNextAvailableWeek()
    {
        // Arrange
        var context = GetDbContext("JumpToNextAvailableWeek");
        
        // First record: 7/1/1990 (Sunday)
        // ISO 8601: 1990-07-01 is Year 1990, Week 26
        // NOTE: ISO week starts on Monday. 1990-07-01 (Sunday) is the LAST day of Week 26.
        var date1 = new DateTime(1990, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        
        // Second record: 8/7/2023 (Monday)
        // ISO 8601: 2023-08-07 is Year 2023, Week 32
        var date2 = new DateTime(2023, 8, 7, 0, 0, 0, DateTimeKind.Utc);

        context.ComicCollection.AddRange(
            new ComicRecord { Id = Guid.NewGuid(), FullTitle = "Old Comic", ReleaseDate = date1 },
            new ComicRecord { Id = Guid.NewGuid(), FullTitle = "New Comic", ReleaseDate = date2 }
        );
        await context.SaveChangesAsync();

        var repository = new ComicRepository(context, GetSearchOptions());

        // Act & Assert
        // Offset 0 should be the 1990 week
        var week0 = await repository.GetComicsByWeekOffsetAsync(0);
        Assert.Single(week0.Comics);
        Assert.Equal("Old Comic", week0.Comics[0].FullTitle);
        Assert.Equal(1990, week0.Year);
        Assert.Equal(26, week0.WeekNumber);

        // Offset 1 should be the 2023 week
        var week1 = await repository.GetComicsByWeekOffsetAsync(1);
        Assert.Single(week1.Comics);
        Assert.Equal("New Comic", week1.Comics[0].FullTitle);
        Assert.Equal(2023, week1.Year);
        Assert.Equal(32, week1.WeekNumber);
    }

    [Fact]
    public async Task GetComicsByWeekOffsetAsync_ShouldHandleSundayCorrect()
    {
        // Arrange
        var context = GetDbContext("SundayCheck");
        
        // 1990-07-01 is a Sunday.
        // It belongs to Week 26 of 1990.
        var date = new DateTime(1990, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        context.ComicCollection.Add(new ComicRecord { Id = Guid.NewGuid(), FullTitle = "Sunday Comic", ReleaseDate = date });
        await context.SaveChangesAsync();

        var repository = new ComicRepository(context, GetSearchOptions());

        // Act
        var result = await repository.GetComicsByWeekOffsetAsync(0);

        // Assert
        Assert.Single(result.Comics);
        Assert.Equal("Sunday Comic", result.Comics[0].FullTitle);
        Assert.Equal(1990, result.Year);
        Assert.Equal(26, result.WeekNumber);
    }

    [Fact]
    public async Task GetComicsByWeekOffsetAsync_ShouldIncludeNextMondayInNextWeek()
    {
        // Arrange
        var context = GetDbContext("BoundaryCheck");
        
        // Week 26, 1990 ends on Sunday July 1.
        var sunday = new DateTime(1990, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        // Week 27, 1990 starts on Monday July 2.
        var monday = new DateTime(1990, 7, 2, 0, 0, 0, DateTimeKind.Utc);

        context.ComicCollection.AddRange(
            new ComicRecord { Id = Guid.NewGuid(), FullTitle = "Sunday Comic", ReleaseDate = sunday },
            new ComicRecord { Id = Guid.NewGuid(), FullTitle = "Monday Comic", ReleaseDate = monday }
        );
        await context.SaveChangesAsync();

        var repository = new ComicRepository(context, GetSearchOptions());

        // Act
        var week0 = await repository.GetComicsByWeekOffsetAsync(0);
        var week1 = await repository.GetComicsByWeekOffsetAsync(1);

        // Assert
        Assert.Single(week0.Comics);
        Assert.Equal("Sunday Comic", week0.Comics[0].FullTitle);
        
        Assert.Single(week1.Comics);
        Assert.Equal("Monday Comic", week1.Comics[0].FullTitle);
    }

    [Fact]
    public async Task GetComicsByWeekOffsetAsync_ShouldHandleLocalTimeIssues()
    {
        // Arrange
        var context = GetDbContext("LocalTimeIssue");
        
        // 1990-07-01 is a Sunday.
        // If it's stored as 1990-07-01 00:00:00 Unspecified or Local, it might be treated differently by Postgres/EF.
        // But here we use InMemory.
        var date = new DateTime(1990, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        context.ComicCollection.Add(new ComicRecord { Id = Guid.NewGuid(), FullTitle = "Comic", ReleaseDate = date });
        await context.SaveChangesAsync();

        var repository = new ComicRepository(context, GetSearchOptions());

        // Act
        var result = await repository.GetComicsByWeekOffsetAsync(0);

        // Assert
        Assert.Single(result.Comics);
    }

    [Fact]
    public async Task SearchCollectionAsync_ShouldSortByTitle()
    {
        // Arrange
        var context = GetDbContext("SortByTitle");
        context.ComicCollection.AddRange(
            new ComicRecord { Id = Guid.NewGuid(), FullTitle = "B Comic", SeriesName = "S1" },
            new ComicRecord { Id = Guid.NewGuid(), FullTitle = "A Comic", SeriesName = "S2" },
            new ComicRecord { Id = Guid.NewGuid(), FullTitle = "C Comic", SeriesName = "S3" }
        );
        await context.SaveChangesAsync();
        var repository = new ComicRepository(context, GetSearchOptions());

        // Act
        var resultsAsc = await repository.SearchCollectionAsync(null, sortBy: "title", sortDescending: false);
        var resultsDesc = await repository.SearchCollectionAsync(null, sortBy: "title", sortDescending: true);

        // Assert
        Assert.Equal("A Comic", resultsAsc[0].FullTitle);
        Assert.Equal("B Comic", resultsAsc[1].FullTitle);
        Assert.Equal("C Comic", resultsAsc[2].FullTitle);

        Assert.Equal("C Comic", resultsDesc[0].FullTitle);
        Assert.Equal("B Comic", resultsDesc[1].FullTitle);
        Assert.Equal("A Comic", resultsDesc[2].FullTitle);
    }

    [Fact]
    public async Task GetComicsByWeekOffsetAsync_ShouldJumpFrom1990To1996()
    {
        // Arrange
        var context = GetDbContext("Jump1990To1996");
        
        // First record: 1990-07-01 (Sunday) -> Year 1990, Week 26
        var date1 = new DateTime(1990, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        
        // Second record: 1996-06-05 (Wednesday) -> Year 1996, Week 23
        var date2 = new DateTime(1996, 6, 5, 0, 0, 0, DateTimeKind.Utc);

        context.ComicCollection.AddRange(
            new ComicRecord { Id = Guid.NewGuid(), FullTitle = "The Thanos Quest 1A", ReleaseDate = date1 },
            new ComicRecord { Id = Guid.NewGuid(), FullTitle = "Onslaught: X-Men 1A", ReleaseDate = date2 }
        );
        await context.SaveChangesAsync();

        var repository = new ComicRepository(context, GetSearchOptions());

        // Act
        var week0 = await repository.GetComicsByWeekOffsetAsync(0);
        var week1 = await repository.GetComicsByWeekOffsetAsync(1);

        // Assert
        Assert.Single(week0.Comics);
        Assert.Equal("The Thanos Quest 1A", week0.Comics[0].FullTitle);
        Assert.Equal(1990, week0.Year);
        Assert.Equal(26, week0.WeekNumber);
        Assert.Equal(0, week0.CurrentOffset);
        Assert.Equal(2, week0.TotalWeeks);

        Assert.Single(week1.Comics);
        Assert.Equal("Onslaught: X-Men 1A", week1.Comics[0].FullTitle);
        Assert.Equal(1996, week1.Year);
        Assert.Equal(23, week1.WeekNumber);
        Assert.Equal(1, week1.CurrentOffset);
    }
    [Fact]
    public async Task GetFirstUnreadWeekOffsetAsync_ShouldReturnZeroWhenAllRead()
    {
        // Arrange
        var context = GetDbContext("AllRead");
        var date = new DateTime(1990, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var id = Guid.NewGuid();
        context.ComicCollection.Add(new ComicRecord { Id = id, FullTitle = "Read Comic", ReleaseDate = date });
        context.ReadingProgress.Add(new ReadingProgress { ComicId = id, IsRead = true });
        await context.SaveChangesAsync();

        var repository = new ComicRepository(context, GetSearchOptions());

        // Act
        var result = await repository.GetFirstUnreadWeekOffsetAsync();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetComicsByWeekOffsetAsync_ShouldJumpFrom1990To1996_UserScenario()
    {
        // Arrange
        var context = GetDbContext("Jump1990To1996User");
        
        // Date 1: 07/01/1990 (Sunday) -> ISO Year 1990, Week 26
        var date1 = new DateTime(1990, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        // Date 2: 06/05/1996 (Wednesday) -> ISO Year 1996, Week 23 (approx)
        var date2 = new DateTime(1996, 6, 5, 0, 0, 0, DateTimeKind.Utc);

        context.ComicCollection.AddRange(
            new ComicRecord { Id = Guid.NewGuid(), FullTitle = "The Thanos Quest 1A", ReleaseDate = date1 },
            new ComicRecord { Id = Guid.NewGuid(), FullTitle = "Onslaught: X-Men 1A", ReleaseDate = date2 }
        );
        await context.SaveChangesAsync();

        var repository = new ComicRepository(context, GetSearchOptions());

        // Act
        var week0 = await repository.GetComicsByWeekOffsetAsync(0);
        var week1 = await repository.GetComicsByWeekOffsetAsync(1);

        // Assert
        Assert.Single(week0.Comics);
        Assert.Equal("The Thanos Quest 1A", week0.Comics[0].FullTitle);
        
        Assert.Single(week1.Comics);
        Assert.Equal("Onslaught: X-Men 1A", week1.Comics[0].FullTitle);
    }
    [Fact]
    public async Task GetComicsByWeekOffsetAsync_ShouldHandleMicroseconds()
    {
        // Arrange
        var context = GetDbContext("Microseconds");
        // Sunday July 1, 1990 at 23:59:59.999
        var sundayLate = new DateTime(1990, 7, 1, 23, 59, 59, DateTimeKind.Utc).AddMilliseconds(999);
        context.ComicCollection.Add(new ComicRecord { Id = Guid.NewGuid(), FullTitle = "Late Sunday Comic", ReleaseDate = sundayLate });
        await context.SaveChangesAsync();

        var repository = new ComicRepository(context, GetSearchOptions());

        // Act
        var result = await repository.GetComicsByWeekOffsetAsync(0);

        // Assert
        Assert.Single(result.Comics);
        Assert.Equal("Late Sunday Comic", result.Comics[0].FullTitle);
    }
    [Fact]
    public async Task GetComicsByWeekOffsetAsync_ShouldReturnComics_WhenDataExists()
    {
        // Arrange
        var context = GetDbContext("ReturnComicsWhenDataExists");
        
        // 1990-07-01 (Sunday) -> ISO Week 26, 1990
        var date1 = new DateTime(1990, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        // 1996-06-05 (Wednesday) -> ISO Week 23, 1996
        var date2 = new DateTime(1996, 6, 5, 0, 0, 0, DateTimeKind.Utc);

        context.ComicCollection.AddRange(
            new ComicRecord { Id = Guid.NewGuid(), FullTitle = "Thanos Quest", ReleaseDate = date1, SeriesName = "Thanos", IssueNumber = "1" },
            new ComicRecord { Id = Guid.NewGuid(), FullTitle = "Onslaught", ReleaseDate = date2, SeriesName = "Onslaught", IssueNumber = "1" }
        );
        await context.SaveChangesAsync();

        var repository = new ComicRepository(context, GetSearchOptions());

        // Act
        var result0 = await repository.GetComicsByWeekOffsetAsync(0);
        var result1 = await repository.GetComicsByWeekOffsetAsync(1);

        // Assert
        Assert.Single(result0.Comics);
        Assert.Equal("Thanos Quest", result0.Comics[0].FullTitle);
        Assert.Equal(1990, result0.Year);
        Assert.Equal(26, result0.WeekNumber);

        Assert.Single(result1.Comics);
        Assert.Equal("Onslaught", result1.Comics[0].FullTitle);
        Assert.Equal(1996, result1.Year);
        Assert.Equal(23, result1.WeekNumber);
    }
}
