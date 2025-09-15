using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PersistenceService.Application.Interfaces;
using PersistenceService.Infrastructure;
using PersistenceService.Infrastructure.Repositories;


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

        builder.Services.AddDbContext<ComicCollectionDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("EventDb")));


        // Add other dependencies as needed
        builder.Services.AddScoped<IEventRepository, EventRepository>();
    }
}
