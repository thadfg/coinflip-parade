namespace PersistenceService.Infrastructure.Database
{
    public static class EndpointConfig
    {
        public static void MapHealthEndpoints(this WebApplication app)
        {
            app.MapGet("/ready", async (IDatabaseReadyChecker checker) =>
            {
                var ready = await checker.IsReadyAsync();
                return ready ? Results.Ok("Ready") : Results.StatusCode(503);
            });
        }
    }

}
