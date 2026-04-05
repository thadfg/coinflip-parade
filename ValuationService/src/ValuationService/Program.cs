using ValuationService.Service;
using PersistenceService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ValuationService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ComicDbContext>(options =>
    options.UseNpgsql("Host=localhost;Port=5432;Database=comicdb;Username=comicadmin;Password=comicpass"));

builder.Services.AddSingleton<IMcpClientWrapper, McpClientWrapper>();
builder.Services.AddHostedService<ValuationBackgroundWorker>();

var app = builder.Build();

// API Endpoints
/*app.MapPost("/run-once", (ValuationControlService control) => {
    control.TriggerRunOnce();
    return Results.Accepted();
});

app.MapPost("/start-continuous", (ValuationControlService control) => {
    control.SetContinuous(true);
    return Results.Ok("Continuous mode enabled.");
});

app.MapPost("/stop", (ValuationControlService control) => {
    control.SetContinuous(false);
    return Results.Ok("Service paused.");
});*/

app.Run();