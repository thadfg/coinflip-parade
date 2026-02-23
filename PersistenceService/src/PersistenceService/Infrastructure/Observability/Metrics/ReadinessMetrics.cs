using Prometheus;
using System.Diagnostics.Metrics;

public static class ReadinessMetrics
{
    public static readonly Gauge DatabaseReady =
        Metrics.CreateGauge("persistence_database_ready", "Indicates whether the database is ready.");
}
