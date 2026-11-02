using OrderingService.Data;
using OrderingService.Features.Orders;

namespace OrderingService.Features.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", async (OrderingDbContext db, ICatalogLookup catalog, CancellationToken ct) =>
        {
            var databaseUp = await CanConnectAsync(db, ct);
            var catalogUp = await catalog.IsReachableAsync(ct);

            var body = new
            {
                service = "OrderingService",
                status = databaseUp && catalogUp ? "Healthy" : "Unhealthy",
                database = databaseUp ? "Connected" : "Unavailable",
                catalog = catalogUp ? "Reachable" : "Unreachable"
            };

            return databaseUp && catalogUp
                ? Results.Ok(body)
                : Results.Json(body, statusCode: StatusCodes.Status503ServiceUnavailable);
        })
        .AllowAnonymous()
        .WithTags("Health")
        .WithName("HealthCheck")
        .WithSummary("Состояние сервиса, его БД и доступность CatalogService")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<bool> CanConnectAsync(OrderingDbContext db, CancellationToken ct)
    {
        try
        {
            return await db.Database.CanConnectAsync(ct);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
