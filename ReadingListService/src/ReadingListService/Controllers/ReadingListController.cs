using Microsoft.AspNetCore.Mvc;
using ReadingListService.Data;
using ReadingListService.Dtos;

namespace ReadingListService.Controllers;

[ApiController]
[Route("api")]
public class ReadingListController : ControllerBase
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

    [HttpPost("reading-list/mark-read/{comicId}")]
    public async Task<IActionResult> ToggleReadStatus(Guid comicId)
    {
        await _repository.ToggleReadStatusAsync(comicId);
        return NoContent();
    }
}
