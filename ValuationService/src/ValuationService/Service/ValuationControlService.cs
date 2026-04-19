namespace ValuationService.Service;

public class ValuationControlService
{
    private bool _isRunning = false;

    public bool IsRunning => _isRunning;

    public void Start()
    {
        _isRunning = true;
    }

    public void Stop()
    {
        _isRunning = false;
    }
}
