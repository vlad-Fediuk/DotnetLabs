using System.Net.Http.Headers;
using System.Net.Http.Json;
using ApiGateway.DTOs;

namespace ApiGateway.Clients;

public interface IOrderingClient
{
    Task<UserOrdersResult> GetUserOrdersAsync(Guid userId, string? bearerToken, CancellationToken cancellationToken);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken);
}

public class OrderingClient : IOrderingClient
{
    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromMilliseconds(1500);

    private readonly HttpClient _httpClient;
    private readonly ILogger<OrderingClient> _logger;

    public OrderingClient(HttpClient httpClient, ILogger<OrderingClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserOrdersResult> GetUserOrdersAsync(Guid userId, string? bearerToken, CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(CallTimeout);

            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/orders");
            request.Headers.Add("X-User-Id", userId.ToString());

            if (!string.IsNullOrWhiteSpace(bearerToken))
            {
                if (AuthenticationHeaderValue.TryParse(bearerToken, out var authHeader))
                {
                    request.Headers.Authorization = authHeader;
                }
                else
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                }
            }

            using var response = await _httpClient.SendAsync(request, timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OrderingService responded with status {StatusCode}", response.StatusCode);
                return new UserOrdersResult(false, Array.Empty<OrderSummaryDto>(), "Сервіс замовлень тимчасово недоступний.");
            }

            var orders = await response.Content.ReadFromJsonAsync<List<OrderSummaryDto>>(cancellationToken: timeoutCts.Token)
                         ?? new List<OrderSummaryDto>();

            return new UserOrdersResult(true, orders);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to query OrderingService: {Message}", ex.Message);
            return new UserOrdersResult(false, Array.Empty<OrderSummaryDto>(), "Сервіс замовлень тимчасово недоступний.");
        }
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
}
