using Microsoft.EntityFrameworkCore;

namespace OrderingService.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var dbContext = serviceProvider.GetRequiredService<OrderingDbContext>();

        await dbContext.Database.MigrateAsync();

        var legacyConnectionString = app.Configuration["LegacyImport:ConnectionString"];
        if (string.IsNullOrWhiteSpace(legacyConnectionString))
        {
            return;
        }

        var importer = new LegacyOrderImporter(
            dbContext,
            legacyConnectionString,
            serviceProvider.GetRequiredService<ILogger<LegacyOrderImporter>>());

        await importer.ImportAsync(CancellationToken.None);
    }
}
