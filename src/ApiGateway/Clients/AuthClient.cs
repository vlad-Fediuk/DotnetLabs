namespace ApiGateway.Clients;

public interface IAuthClient
{
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken);
}

public class AuthClient : IAuthClient
{
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromMilliseconds(1500);

    private readonly HttpClient _httpClient;

    public AuthClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
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
