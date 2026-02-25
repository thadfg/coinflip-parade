using System.Diagnostics.Metrics;
using SharedLibrary.Constants;

namespace PersistenceService.Infrastructure.Observability.Metrics;

public static class ReadinessMetrics
{
    private static readonly Meter ReadinessMeter = new(MeterNames.ComicPersistence, "1.0.0");
    
    private static int _databaseReadyValue = 0;
    
    // Added this method to force static initialization
    public static void Initialize() { }

    public static void SetDatabaseReady(int value)
    {
        _databaseReadyValue = value;
    }

    static ReadinessMetrics()
    {
        ReadinessMeter.CreateObservableGauge("persistence_database_ready", 
            () => _databaseReadyValue, 
            description: "Indicates whether the database is ready.");
    }
}
