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

    public List<WeeklyReadingListViewDto> WeeklyGroups { get; set; } = new();

    public async Task OnGetAsync()
    {
        WeeklyGroups = await _repository.GetWeeklyReadingListAsync();
    }
}
