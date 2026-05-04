using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ReadingListService.Data;
using ReadingListService.Dtos;

namespace ReadingListService.Pages;

public class IndexModel : PageModel
{
    private readonly IComicRepository _repository;

    public IndexModel(IComicRepository repository)
    {
        _repository = repository;
    }

    public List<ComicSearchResultDto> Comics { get; set; } = new();

    public int CurrentPage { get; set; } = 1;
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }

    public async Task OnGetAsync(string? sortBy = null, bool desc = false)
    {
        SortBy = sortBy;
        SortDescending = desc;
        Comics = await _repository.SearchCollectionAsync(null, 1, pageSize: 20, sortBy: sortBy, sortDescending: desc);
    }

    public async Task<IActionResult> OnGetSearchAsync(string? term, int page = 1, string? sortBy = null, bool desc = false)
    {
        SearchTerm = term;
        CurrentPage = page;
        SortBy = sortBy;
        SortDescending = desc;
        Comics = await _repository.SearchCollectionAsync(term, page, pageSize: 20, sortBy: sortBy, sortDescending: desc);
        return Partial("_ComicTable", Comics);
    }
}
