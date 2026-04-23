using Microsoft.EntityFrameworkCore;
using ReadingListService.Data;
using ReadingListService.Dtos;
using ReadingListService.Models;
using System.Globalization;

namespace ReadingListService.Data;

public interface IComicRepository
{
    Task<List<ComicSearchResultDto>> SearchCollectionAsync(string? searchTerm);
    Task<List<WeeklyReadingListViewDto>> GetWeeklyReadingListAsync();
    Task ToggleReadStatusAsync(Guid comicId);
}

public class ComicRepository : IComicRepository
{
    private readonly ReadingListDbContext _context;

    public ComicRepository(ReadingListDbContext context)
    {
        _context = context;
    }

    public async Task<List<ComicSearchResultDto>> SearchCollectionAsync(string? searchTerm)
    {
        var query = _context.ComicCollection
            .AsNoTracking()
            .Where(c => c.InCollection); // Mandatory filter

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            // Format the search term for ILike: %term%
            var formattedSearch = $"%{searchTerm}%";

            query = query.Where(c => 
                EF.Functions.ILike(c.PublisherName, formattedSearch) ||
                EF.Functions.ILike(c.SeriesName, formattedSearch) ||
                EF.Functions.ILike(c.FullTitle, formattedSearch)
            );
        }

        return await query
            .Select(c => new ComicSearchResultDto
            {
                Id = c.Id,
                FullTitle = c.FullTitle,
                SeriesName = c.SeriesName,
                PublisherName = c.PublisherName,
                IssueNumber = c.IssueNumber,
                ReleaseDate = c.ReleaseDate,
                // Left Join with ReadingProgress
                IsRead = _context.ReadingProgress
                            .Any(p => p.ComicId == c.Id && p.IsRead)
            })
            .OrderBy(c => c.SeriesName)
            .ThenBy(c => c.IssueNumber)
            .ToListAsync();
    }

    public async Task<List<WeeklyReadingListViewDto>> GetWeeklyReadingListAsync()
    {
        var comics = await _context.ComicCollection
            .AsNoTracking()
            .Where(c => c.InCollection)
            .Where(c => !EF.Functions.ILike(c.FullTitle, "%Annual%"))
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
                Year = c.ReleaseDate!.Value.Year, 
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

    public async Task ToggleReadStatusAsync(Guid comicId)
    {
        var progress = await _context.ReadingProgress
            .FirstOrDefaultAsync(p => p.ComicId == comicId);

        if (progress == null)
        {
            _context.ReadingProgress.Add(new ReadingProgress 
            { 
                ComicId = comicId, 
                IsRead = true, 
                ReadAtUtc = DateTime.UtcNow 
            });
        }
        else
        {
            progress.IsRead = !progress.IsRead;
            progress.ReadAtUtc = progress.IsRead ? DateTime.UtcNow : null;
        }

        await _context.SaveChangesAsync();
    }
}
