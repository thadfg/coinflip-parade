using Microsoft.AspNetCore.Mvc;
using ValuationService.Service;

namespace ValuationService.Controllers;

[ApiController]
[Route("api/valuation")]
public class ValuationController : ControllerBase
{
    private readonly ValuationControlService _controlService;

    public ValuationController(ValuationControlService controlService)
    {
        _controlService = controlService;
    }

    [HttpPost("start")]
    public IActionResult Start()
    {
        _controlService.Start();
        return Ok(new { status = "started" });
    }

    [HttpPost("stop")]
    public IActionResult Stop()
    {
        _controlService.Stop();
        return Ok(new { status = "stopped" });
    }

    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(new { isRunning = _controlService.IsRunning });
    }
}
