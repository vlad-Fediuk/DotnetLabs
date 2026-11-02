using System.Net;
using System.Net.Http.Json;
using ApiGateway.DTOs;
using ApiGateway.Services;
using ECommerce.Shared.Exceptions;
using Microsoft.Extensions.Caching.Memory;

namespace ApiGateway.Clients;

public interface ICatalogClient
{
    Task<CatalogItemSummaryDto?> GetItemAsync(Guid id, CancellationToken cancellationToken);
    void InvalidateItem(Guid id);
    void InvalidateAll();
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken);
}

public class CatalogClient : ICatalogClient
{
    private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromMilliseconds(1500);

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ICacheMetricsService _metrics;
    private readonly ILogger<CatalogClient> _logger;

    public CatalogClient(
        HttpClient httpClient,
        IMemoryCache cache,
        ICacheMetricsService metrics,
        ILogger<CatalogClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<CatalogItemSummaryDto?> GetItemAsync(Guid id, CancellationToken cancellationToken)
    {
        var cacheKey = $"catalog:item:{id}";

        if (_cache.TryGetValue(cacheKey, out CatalogItemSummaryDto? cachedItem) && cachedItem is not null)
        {
            _metrics.RecordHit();
            return cachedItem;
        }

        _metrics.RecordMiss();

        var fetchedItem = await ExecuteWithRetryAsync(async () =>
        {
            using var response = await _httpClient.GetAsync($"/api/v1/catalog/items/{id}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("CatalogService returned {StatusCode}: {Error}", response.StatusCode, errorContent);
                throw new ServiceUnavailableException("Сервіс каталогу товарів тимчасово недоступний.", retryAfterSeconds: 5);
            }

            return await response.Content.ReadFromJsonAsync<CatalogItemSummaryDto>(cancellationToken: cancellationToken);
        }, cancellationToken);

        if (fetchedItem is not null)
        {
            _cache.Set(cacheKey, fetchedItem, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = DefaultCacheTtl
            });
        }

        return fetchedItem;
    }

    public void InvalidateItem(Guid id)
    {
        var cacheKey = $"catalog:item:{id}";
        _cache.Remove(cacheKey);
        _metrics.RecordEviction();
    }

    public void InvalidateAll()
    {
        if (_cache is MemoryCache concreteCache)
        {
            concreteCache.Compact(1.0);
        }
        _metrics.RecordEviction();
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(HealthCheckTimeout);

            using var response = await _httpClient.GetAsync("/health", timeoutCts.Token);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        var attempts = 0;
        const int maxAttempts = 3;

        while (true)
        {
            attempts++;
            try
            {
                return await operation();
            }
            catch (Exception ex) when (attempts < maxAttempts && !cancellationToken.IsCancellationRequested && IsTransient(ex))
            {
                var delayMs = attempts * 100;
                _logger.LogWarning("Retrying CatalogService call after {Delay}ms due to transient error: {Message}", delayMs, ex.Message);
                await Task.Delay(delayMs, cancellationToken);
            }
            catch (Exception ex) when (ex is not ServiceUnavailableException)
            {
                _logger.LogError(ex, "CatalogService call failed after {Attempts} attempts", attempts);
                throw new ServiceUnavailableException("Сервіс каталогу товарів тимчасово недоступний.", retryAfterSeconds: 5);
            }
        }
    }

    private static bool IsTransient(Exception ex)
    {
        return ex is HttpRequestException or TimeoutException or TaskCanceledException or ServiceUnavailableException;
    }
}
