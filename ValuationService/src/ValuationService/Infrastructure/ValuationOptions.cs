namespace ValuationService.Infrastructure;

public class ValuationOptions
{
    public const string Valuations = "Valuations";

    public int CutoffDays { get; set; } = 30;
    public int BatchSize { get; set; } = 10;
    public int DelayMinutes { get; set; } = 1;
}
