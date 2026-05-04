using Microsoft.AspNetCore.Mvc.RazorPages;
using ReadingListService.Data;
using ReadingListService.Dtos;

namespace ReadingListService.Pages;

public class WeeklyModel : PageModel
{
    private readonly IComicRepository _repository;

    public WeeklyModel(IComicRepository repository)
    {
        _repository = repository;
    }

    public WeeklyReadingListViewDto WeeklyData { get; set; } = new();
    public int CurrentOffset { get; set; }

    public async Task OnGetAsync(int? week = null)
    {
        if (week == null)
        {
            CurrentOffset = await _repository.GetFirstUnreadWeekOffsetAsync();
        }
        else
        {
            CurrentOffset = week.Value;
        }
        
        WeeklyData = await _repository.GetComicsByWeekOffsetAsync(CurrentOffset);
    }
}
