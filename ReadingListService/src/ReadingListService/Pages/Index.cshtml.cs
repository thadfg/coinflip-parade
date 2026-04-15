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

    public async Task OnGetAsync()
    {
        Comics = await _repository.SearchCollectionAsync(null);
    }

    public async Task<IActionResult> OnGetSearchAsync(string? term)
    {
        Comics = await _repository.SearchCollectionAsync(term);
        return Partial("_ComicGrid", Comics);
    }
}
