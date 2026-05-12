using Microsoft.EntityFrameworkCore;
using ReadingListService.Data;
using ReadingListService.Dtos;
using ReadingListService.Models;
using ReadingListService.Options;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace ReadingListService.Data;

public interface IComicRepository
{
    Task<List<ComicSearchResultDto>> SearchCollectionAsync(string? searchTerm, int page = 1, int? pageSize = null, string? sortBy = null, bool sortDescending = false);
    Task<List<WeeklyReadingListViewDto>> GetWeeklyReadingListAsync();
    Task<DateTime?> GetEarliestReleaseDateAsync();
    Task<int> GetFirstUnreadWeekOffsetAsync();
    Task<WeeklyReadingListViewDto> GetComicsByWeekOffsetAsync(int offset);
    Task<ComicSearchResultDto> ToggleReadStatusAsync(Guid comicId);
}

public class ComicRepository : IComicRepository
{
    private readonly ReadingListDbContext _context;
    private readonly SearchOptions _searchOptions;

    public ComicRepository(ReadingListDbContext context, IOptions<SearchOptions> searchOptions)
    {
        _context = context;
        _searchOptions = searchOptions.Value;
    }

    public async Task<List<ComicSearchResultDto>> SearchCollectionAsync(string? searchTerm, int page = 1, int? pageSize = null, string? sortBy = null, bool sortDescending = false)
    {
        int effectivePageSize = pageSize ?? _searchOptions.DefaultPageSize;
        var query = _context.ComicCollection
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var formattedSearch = $"%{searchTerm}%";

            query = query.Where(c => 
                EF.Functions.ILike(c.PublisherName ?? string.Empty, formattedSearch) ||
                EF.Functions.ILike(c.SeriesName ?? string.Empty, formattedSearch) ||
                EF.Functions.ILike(c.FullTitle, formattedSearch)
            );
        }

        var projection = query.Select(c => new ComicSearchResultDto
        {
            Id = c.Id,
            FullTitle = c.FullTitle,
            SeriesName = c.SeriesName,
            PublisherName = c.PublisherName,
            IssueNumber = c.IssueNumber,
            ReleaseDate = c.ReleaseDate,
            IsRead = _context.ReadingProgress
                        .Any(p => p.ComicId == c.Id && p.IsRead)
        });

        if (!string.IsNullOrEmpty(sortBy))
        {
            projection = sortBy.ToLower() switch
            {
                "title" => sortDescending ? projection.OrderByDescending(c => c.FullTitle) : projection.OrderBy(c => c.FullTitle),
                "series" => sortDescending ? projection.OrderByDescending(c => c.SeriesName).ThenBy(c => c.IssueNumber) : projection.OrderBy(c => c.SeriesName).ThenBy(c => c.IssueNumber),
                "publisher" => sortDescending ? projection.OrderByDescending(c => c.PublisherName).ThenBy(c => c.SeriesName) : projection.OrderBy(c => c.PublisherName).ThenBy(c => c.SeriesName),
                "releasedate" => sortDescending ? projection.OrderByDescending(c => c.ReleaseDate) : projection.OrderBy(c => c.ReleaseDate),
                _ => projection.OrderBy(c => c.SeriesName).ThenBy(c => c.IssueNumber)
            };
        }
        else
        {
            projection = projection.OrderBy(c => c.SeriesName).ThenBy(c => c.IssueNumber);
        }

        return await projection
            .Skip((page - 1) * effectivePageSize)
            .Take(effectivePageSize)
            .ToListAsync();
    }

    public async Task<List<WeeklyReadingListViewDto>> GetWeeklyReadingListAsync()
    {
        var comics = await _context.ComicCollection
            .AsNoTracking()
            // .Where(c => c.InCollection)
            .Where(c => !EF.Functions.ILike(c.FullTitle, _searchOptions.ExclusionPattern))
            .Select(c => new ComicSearchResultDto
            {
                Id = c.Id,
                FullTitle = c.FullTitle,
                SeriesName = c.SeriesName,
                PublisherName = c.PublisherName,
                IssueNumber = c.IssueNumber,
                ReleaseDate = c.ReleaseDate,
                IsRead = _context.ReadingProgress
                            .Any(p => p.ComicId == c.Id && p.IsRead)
            })
            .ToListAsync();

        return comics
            .Where(c => c.ReleaseDate.HasValue)
            .GroupBy(c => new 
            { 
                Year = ISOWeek.GetYear(c.ReleaseDate!.Value), 
                Week = ISOWeek.GetWeekOfYear(c.ReleaseDate.Value) 
            })
            .Select(g => new WeeklyReadingListViewDto
            {
                Year = g.Key.Year,
                WeekNumber = g.Key.Week,
                Comics = g.OrderBy(c => c.SeriesName).ThenBy(c => c.IssueNumber).ToList()
            })
            .OrderByDescending(g => g.Year)
            .ThenByDescending(g => g.WeekNumber)
            .ToList();
    }

    public async Task<DateTime?> GetEarliestReleaseDateAsync()
    {
        return await _context.ComicCollection
            .Where(c => c.ReleaseDate.HasValue)
            .MinAsync(c => c.ReleaseDate);
    }

    public async Task<int> GetFirstUnreadWeekOffsetAsync()
    {
        var allDates = await _context.ComicCollection
            .AsNoTracking()
            .Where(c => c.ReleaseDate.HasValue)
            .OrderBy(c => c.ReleaseDate)
            .Select(c => c.ReleaseDate!.Value)
            .ToListAsync();

        if (!allDates.Any()) return 0;

        var distinctWeeks = allDates
            .Select(d => new { Year = ISOWeek.GetYear(d), Week = ISOWeek.GetWeekOfYear(d) })
            .Distinct()
            .OrderBy(w => w.Year)
            .ThenBy(w => w.Week)
            .ToList();

        var firstUnreadComic = await _context.ComicCollection
            .AsNoTracking()
            .Where(c => c.ReleaseDate.HasValue && !_context.ReadingProgress.Any(p => p.ComicId == c.Id))
            .OrderBy(c => c.ReleaseDate)
            .FirstOrDefaultAsync();

        if (firstUnreadComic == null) return 0;

        var unreadYear = ISOWeek.GetYear(firstUnreadComic.ReleaseDate!.Value);
        var unreadWeek = ISOWeek.GetWeekOfYear(firstUnreadComic.ReleaseDate!.Value);

        var index = distinctWeeks.FindIndex(w => w.Year == unreadYear && w.Week == unreadWeek);
        return index >= 0 ? index : 0;
    }

    public async Task<WeeklyReadingListViewDto> GetComicsByWeekOffsetAsync(int offset)
    {
        var allDates = await _context.ComicCollection
            .AsNoTracking()
            .Where(c => c.ReleaseDate.HasValue)
            .OrderBy(c => c.ReleaseDate)
            .Select(c => c.ReleaseDate!.Value)
            .ToListAsync();

        var distinctWeeks = allDates
            .Select(d => new { Year = ISOWeek.GetYear(d), Week = ISOWeek.GetWeekOfYear(d) })
            .Distinct()
            .OrderBy(w => w.Year)
            .ThenBy(w => w.Week)
            .ToList();

        if (!distinctWeeks.Any()) return new WeeklyReadingListViewDto();

        // Ensure offset is within bounds
        int safeOffset = Math.Max(0, Math.Min(offset, distinctWeeks.Count - 1));
        var targetWeek = distinctWeeks[safeOffset];

        // Using ISOWeek standard: week starts on Monday
        var startDate = ISOWeek.ToDateTime(targetWeek.Year, targetWeek.Week, DayOfWeek.Monday);
        var endDate = startDate.AddDays(7);

        // Adjust dates to handle UTC conversion issues in PostgreSQL
        var startUtc = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(endDate.Date, DateTimeKind.Utc);

        var comics = await _context.ComicCollection
            .AsNoTracking()
            .Where(c => c.ReleaseDate.HasValue && c.ReleaseDate >= startUtc && c.ReleaseDate < endUtc)
            .Select(c => new ComicSearchResultDto
            {
                Id = c.Id,
                FullTitle = c.FullTitle,
                SeriesName = c.SeriesName,
                PublisherName = c.PublisherName,
                IssueNumber = c.IssueNumber,
                ReleaseDate = c.ReleaseDate,
                IsRead = _context.ReadingProgress
                            .Any(p => p.ComicId == c.Id && p.IsRead)
            })
            .OrderBy(c => c.SeriesName)
            .ThenBy(c => c.IssueNumber)
            .ToListAsync();

        // FALLBACK: If no comics found by date range query, use ISOWeek logic in memory
        if (!comics.Any())
        {
            var allComics = await _context.ComicCollection
                .AsNoTracking()
                .Where(c => c.ReleaseDate.HasValue)
                .Select(c => new ComicSearchResultDto
                {
                    Id = c.Id,
                    FullTitle = c.FullTitle,
                    SeriesName = c.SeriesName,
                    PublisherName = c.PublisherName,
                    IssueNumber = c.IssueNumber,
                    ReleaseDate = c.ReleaseDate,
                    IsRead = _context.ReadingProgress
                                .Any(p => p.ComicId == c.Id && p.IsRead)
                })
                .ToListAsync();

            comics = allComics
                .Where(c => ISOWeek.GetYear(c.ReleaseDate!.Value) == targetWeek.Year && 
                            ISOWeek.GetWeekOfYear(c.ReleaseDate.Value) == targetWeek.Week)
                .OrderBy(c => c.SeriesName)
                .ThenBy(c => c.IssueNumber)
                .ToList();
        }

        var totalComics = await _context.ComicCollection.CountAsync();
        var readComics = await _context.ReadingProgress.CountAsync(p => p.IsRead);

        return new WeeklyReadingListViewDto
        {
            Year = targetWeek.Year,
            WeekNumber = targetWeek.Week,
            TotalWeeks = distinctWeeks.Count,
            CurrentOffset = safeOffset,
            TotalComicsInCollection = totalComics,
            ReadComicsCount = readComics,
            Comics = comics
        };
    }

    public async Task<ComicSearchResultDto> ToggleReadStatusAsync(Guid comicId)
    {
        var comic = await _context.ComicCollection
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == comicId);

        if (comic == null) throw new KeyNotFoundException("Comic not found");

        var progress = await _context.ReadingProgress
            .FirstOrDefaultAsync(p => p.ComicId == comicId);

        bool isRead;
        if (progress == null)
        {
            _context.ReadingProgress.Add(new ReadingProgress 
            { 
                ComicId = comicId, 
                IsRead = true, 
                ReadAtUtc = DateTime.UtcNow 
            });
            isRead = true;
        }
        else
        {
            _context.ReadingProgress.Remove(progress);
            isRead = false;
        }

        await _context.SaveChangesAsync();

        return new ComicSearchResultDto
        {
            Id = comic.Id,
            FullTitle = comic.FullTitle,
            SeriesName = comic.SeriesName,
            PublisherName = comic.PublisherName,
            IssueNumber = comic.IssueNumber,
            ReleaseDate = comic.ReleaseDate,
            IsRead = isRead
        };
    }
}
