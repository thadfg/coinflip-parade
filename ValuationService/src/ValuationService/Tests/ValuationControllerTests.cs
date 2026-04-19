using Microsoft.AspNetCore.Mvc;
using ValuationService.Controllers;
using ValuationService.Service;
using Xunit;

namespace ValuationService.Tests;

public class ValuationControllerTests
{
    [Fact]
    public void Start_CallsControlServiceStart()
    {
        var controlService = new ValuationControlService();
        var controller = new ValuationController(controlService);

        var result = controller.Start();

        Assert.IsType<OkObjectResult>(result);
        Assert.True(controlService.IsRunning);
    }

    [Fact]
    public void Stop_CallsControlServiceStop()
    {
        var controlService = new ValuationControlService();
        controlService.Start();
        var controller = new ValuationController(controlService);

        var result = controller.Stop();

        Assert.IsType<OkObjectResult>(result);
        Assert.False(controlService.IsRunning);
    }

    [Fact]
    public void Status_ReturnsCorrectRunningState()
    {
        var controlService = new ValuationControlService();
        var controller = new ValuationController(controlService);

        var result = controller.Status() as OkObjectResult;
        Assert.NotNull(result);
        
        // Use dynamic or reflection to check anonymous object
        var isRunning = result.Value?.GetType().GetProperty("isRunning")?.GetValue(result.Value, null);
        Assert.Equal(false, isRunning);

        controlService.Start();
        result = controller.Status() as OkObjectResult;
        isRunning = result.Value?.GetType().GetProperty("isRunning")?.GetValue(result.Value, null);
        Assert.Equal(true, isRunning);
    }
}
