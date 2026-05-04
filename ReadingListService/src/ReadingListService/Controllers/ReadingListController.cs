using Microsoft.AspNetCore.Mvc;
using ReadingListService.Data;
using ReadingListService.Dtos;

namespace ReadingListService.Controllers;

[ApiController]
[Route("api")]
public class ReadingListController : Controller
{
    private readonly IComicRepository _repository;

    public ReadingListController(IComicRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("collection/search")]
    public async Task<ActionResult<List<ComicSearchResultDto>>> SearchCollection([FromQuery] string? term)
    {
        var results = await _repository.SearchCollectionAsync(term);
        return Ok(results);
    }

    [HttpGet("reading-list/weekly")]
    public async Task<ActionResult<List<WeeklyReadingListViewDto>>> GetWeeklyList()
    {
        var results = await _repository.GetWeeklyReadingListAsync();
        return Ok(results);
    }

    [HttpGet("reading-list/weekly-offset")]
    public async Task<ActionResult<WeeklyReadingListViewDto>> GetWeeklyListByOffset([FromQuery] int? offset = null)
    {
        int actualOffset = offset ?? await _repository.GetFirstUnreadWeekOffsetAsync();
        var result = await _repository.GetComicsByWeekOffsetAsync(actualOffset);
        return Ok(result);
    }

    [HttpPost("reading-list/mark-read/{comicId}")]
    public async Task<IActionResult> ToggleReadStatus(Guid comicId)
    {
        var result = await _repository.ToggleReadStatusAsync(comicId);
        
        // If it's an HTMX request, return the partial for the button
        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return PartialView("~/Pages/Shared/_ReadStatusToggle.cshtml", result);
        }

        // For non-HTMX (like React), return JSON
        return Json(result);
    }
}
