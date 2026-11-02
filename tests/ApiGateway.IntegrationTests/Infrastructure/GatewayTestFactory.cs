using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using ApiGateway.Clients;
using ApiGateway.DTOs;
using ApiGateway.Services;
using ECommerce.Shared.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace ApiGateway.IntegrationTests.Infrastructure;

public class GatewayTestFactory : WebApplicationFactory<Program>
{
    public const string TestJwtKey = "SuperSecretKeyForJwtAuthenticationMustBeAtLeast32BytesLong!";
    public const string TestIssuer = "ECommerceApi";
    public const string TestAudience = "ECommerceApi";

    public TestCatalogClient CatalogStub { get; } = new();
    public TestOrderingClient OrderingStub { get; } = new();
    public TestAuthClient AuthStub { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var catalogDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICatalogClient));
            if (catalogDescriptor is not null)
            {
                services.Remove(catalogDescriptor);
            }

            var orderingDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IOrderingClient));
            if (orderingDescriptor is not null)
            {
                services.Remove(orderingDescriptor);
            }

            var authDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAuthClient));
            if (authDescriptor is not null)
            {
                services.Remove(authDescriptor);
            }

            services.AddSingleton<ICatalogClient>(sp =>
            {
                CatalogStub.Metrics = sp.GetRequiredService<ICacheMetricsService>();
                return CatalogStub;
            });
            services.AddSingleton<IOrderingClient>(OrderingStub);
            services.AddSingleton<IAuthClient>(AuthStub);
        });
    }

    public void Reset()
    {
        CatalogStub.Reset();
        OrderingStub.Reset();
        AuthStub.Reset();

        using var scope = Services.CreateScope();
        var cache = scope.ServiceProvider.GetService<IMemoryCache>();
        if (cache is MemoryCache concreteCache)
        {
            concreteCache.Compact(1.0);
        }
    }

    public string GenerateToken(Guid userId, string role = "User")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(TestJwtKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("sub", userId.ToString()),
            new(ClaimTypes.Role, role)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = TestIssuer,
            Audience = TestAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public HttpClient CreateAuthorizedClient(Guid userId, string role = "User")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.0.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        var token = GenerateToken(userId, role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

public class TestCatalogClient : ICatalogClient
{
    private readonly ConcurrentDictionary<Guid, CatalogItemSummaryDto> _items = new();
    private readonly ConcurrentDictionary<string, CatalogItemSummaryDto> _cache = new();
    private int _callCount;

    public ICacheMetricsService? Metrics { get; set; }
    public bool ShouldFail { get; set; }
    public bool IsHealthy { get; set; } = true;
    public int CallCount => _callCount;

    public void Reset()
    {
        _items.Clear();
        _cache.Clear();
        _callCount = 0;
        ShouldFail = false;
        IsHealthy = true;
    }

    public void SetItem(CatalogItemSummaryDto item)
    {
        _items[item.Id] = item;
    }

    public Task<CatalogItemSummaryDto?> GetItemAsync(Guid id, CancellationToken cancellationToken)
    {
        var cacheKey = $"catalog:item:{id}";

        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            Metrics?.RecordHit();
            return Task.FromResult<CatalogItemSummaryDto?>(cached);
        }

        Metrics?.RecordMiss();
        Interlocked.Increment(ref _callCount);

        if (ShouldFail)
        {
            throw new ServiceUnavailableException("Сервіс каталогу товарів тимчасово недоступний.", retryAfterSeconds: 5);
        }

        _items.TryGetValue(id, out var item);
        if (item is not null)
        {
            _cache[cacheKey] = item;
        }

        return Task.FromResult(item);
    }

    public void InvalidateItem(Guid id)
    {
        _cache.TryRemove($"catalog:item:{id}", out _);
        Metrics?.RecordEviction();
    }

    public void InvalidateAll()
    {
        _cache.Clear();
        Metrics?.RecordEviction();
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(IsHealthy);
    }
}

public class TestOrderingClient : IOrderingClient
{
    private readonly ConcurrentDictionary<Guid, List<OrderSummaryDto>> _ordersByUser = new();
    private int _callCount;

    public bool ShouldFail { get; set; }
    public bool IsHealthy { get; set; } = true;
    public int CallCount => _callCount;

    public void Reset()
    {
        _ordersByUser.Clear();
        _callCount = 0;
        ShouldFail = false;
        IsHealthy = true;
    }

    public void SetUserOrders(Guid userId, List<OrderSummaryDto> orders)
    {
        _ordersByUser[userId] = orders;
    }

    public Task<UserOrdersResult> GetUserOrdersAsync(Guid userId, string? bearerToken, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _callCount);

        if (ShouldFail)
        {
            return Task.FromResult(new UserOrdersResult(false, Array.Empty<OrderSummaryDto>(), "Сервіс замовлень тимчасово недоступний."));
        }

        _ordersByUser.TryGetValue(userId, out var orders);
        return Task.FromResult(new UserOrdersResult(true, orders ?? new List<OrderSummaryDto>()));
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(IsHealthy);
    }
}

public class TestAuthClient : IAuthClient
{
    public bool IsHealthy { get; set; } = true;

    public void Reset()
    {
        IsHealthy = true;
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(IsHealthy);
    }
}
