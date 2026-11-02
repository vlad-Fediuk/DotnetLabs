using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using CatalogService.Data;
using ECommerce.Shared.Protos;
using ECommerce.Shared.Security;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Xunit;

namespace CatalogService.IntegrationTests.Infrastructure;

public class CatalogTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestConnectionString = "Host=localhost;Port=5432;Database=EcommerceCatalogTestDb;Username=postgres;Password=1111";
    public const string TestJwtKey = "SuperSecretKeyForJwtAuthenticationMustBeAtLeast32BytesLong!";
    public const string TestIssuer = "ECommerceApi";
    public const string TestAudience = "ECommerceApi";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = TestConnectionString,
                ["Jwt:Key"] = TestJwtKey,
                ["Jwt:Issuer"] = TestIssuer,
                ["Jwt:Audience"] = TestAudience
            });
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
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    public HttpClient CreateAuthorizedClient(Guid userId, params string[] roles)
    {
        var client = CreateClient();
        var token = GenerateToken(userId, roles);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public CatalogGrpcService.CatalogGrpcServiceClient CreateGrpcClient()
    {
        var httpClient = CreateClient();
        var channel = GrpcChannel.ForAddress(httpClient.BaseAddress!, new GrpcChannelOptions
        {
            HttpClient = httpClient
        });

        return new CatalogGrpcService.CatalogGrpcServiceClient(channel);
    }

    public string GenerateToken(Guid userId, params string[] roles)
    {
        var effectiveRoles = roles.Length > 0 ? roles : new[] { RoleNames.User };
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("sub", userId.ToString())
        };

        foreach (var role in effectiveRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in RolePermissions.GetPermissionsForRoles(effectiveRoles))
        {
            claims.Add(new Claim(Permissions.ClaimType, permission));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task WithDbContextAsync(Func<CatalogDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<CatalogDbContext>());
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}
