using ECommerce.Shared.Protos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using OrderingService.Data;
using Xunit;

namespace Ordering.IntegrationTests.Infrastructure;

public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestConnectionString = "Host=localhost;Port=5432;Database=EcommerceOrdersTestDb;Username=postgres;Password=1111";

    public FakeCatalogClient Catalog { get; } = new();

    public TestTokenProvider Tokens => new(Services.GetRequiredService<IConfiguration>());

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = TestConnectionString,
                ["LegacyImport:ConnectionString"] = null
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<CatalogGrpcService.CatalogGrpcServiceClient>();
            services.AddSingleton<CatalogGrpcService.CatalogGrpcServiceClient>(Catalog);
        });
    }

    public async Task InitializeAsync()
    {
        var databaseName = new NpgsqlConnectionStringBuilder(TestConnectionString).Database;
        if (databaseName is null || !databaseName.EndsWith("TestDb", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Integration tests must run against a dedicated *TestDb database.");
        }

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
    }

    public async Task WithDbContextAsync(Func<OrderingDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<OrderingDbContext>());
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}
