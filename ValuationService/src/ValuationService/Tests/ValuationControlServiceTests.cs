using Xunit;
using ValuationService.Service;

namespace ValuationService.Tests;

public class ValuationControlServiceTests
{
    [Fact]
    public void InitialState_IsStopped()
    {
        var service = new ValuationControlService();
        Assert.False(service.IsRunning);
    }

    [Fact]
    public void Start_SetsIsRunningToTrue()
    {
        var service = new ValuationControlService();
        service.Start();
        Assert.True(service.IsRunning);
    }

    [Fact]
    public void Stop_SetsIsRunningToFalse()
    {
        var service = new ValuationControlService();
        service.Start();
        service.Stop();
        Assert.False(service.IsRunning);
    }
}
