using PersistenceService.Infrastructure;
using Microsoft.EntityFrameworkCore;


namespace PersistenceService.Startup;


    /// <summary>
    /// Configuration class for managing dependencies in the PersistenceService.
    /// </summary>


public static class DependenciesConfig
{
    public static void AddDependencies(this WebApplicationBuilder builder)
    {

        var env = builder.Environment.EnvironmentName;
        var config = builder.Configuration;

        // Add DbContext with PostgreSQL provider
        builder.Services.AddDbContext<EventDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("EventDb")));
        // Add other dependencies as needed
        // services.AddScoped<IYourService, YourServiceImplementation>();
    }
}
