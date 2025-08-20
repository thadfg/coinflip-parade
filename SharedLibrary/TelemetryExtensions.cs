using System.Diagnostics;

namespace SharedLibrary.Extensions;

public static class TelemetryExtensions
{
    public static TagList BuildMetricTags(Guid importId, string sourceSystem, string triggeredBy)
    {
        return new TagList
        {
            { "import_id", importId.ToString() },
            { "source_system", sourceSystem },
            { "triggered_by", triggeredBy }
        };
    }
}
