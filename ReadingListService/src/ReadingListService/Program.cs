using Microsoft.EntityFrameworkCore;
using ReadingListService.Data;
using ReadingListService.Options;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.Configure<ServiceOptions>(
    builder.Configuration.GetSection(ServiceOptions.SectionName));
builder.Services.Configure<SearchOptions>(
    builder.Configuration.GetSection(SearchOptions.SectionName));

builder.Services.AddDbContext<ReadingListDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ReadingListDbContext>();

builder.Services.AddScoped<IComicRepository, ComicRepository>();

builder.Services.AddRazorPages();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Container"))
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var serviceOptions = app.Services.GetRequiredService<IOptions<ServiceOptions>>().Value;
app.UsePathBase(serviceOptions.PathBase);

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.MapHealthChecks(serviceOptions.HealthCheckPath);

// Apply migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ReadingListDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
