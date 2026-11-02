using ApiGateway.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("health")]
[Tags("Health")]
public class HealthController : ControllerBase
{
    private readonly IAuthClient _authClient;
    private readonly ICatalogClient _catalogClient;
    private readonly IOrderingClient _orderingClient;

    public HealthController(
        IAuthClient authClient,
        ICatalogClient catalogClient,
        IOrderingClient orderingClient)
    {
        _authClient = authClient;
        _catalogClient = catalogClient;
        _orderingClient = orderingClient;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth(CancellationToken ct)
    {
        var authTask = _authClient.IsHealthyAsync(ct);
        var catalogTask = _catalogClient.IsHealthyAsync(ct);
        var orderingTask = _orderingClient.IsHealthyAsync(ct);

        await Task.WhenAll(authTask, catalogTask, orderingTask);

        var authUp = await authTask;
        var catalogUp = await catalogTask;
        var orderingUp = await orderingTask;

        var allUp = authUp && catalogUp && orderingUp;

        var responseBody = new
        {
            service = "ApiGateway",
            status = allUp ? "Healthy" : "Degraded",
            port = 5000,
            dependencies = new
            {
                authService = authUp ? "Healthy" : "Unreachable",
                catalogService = catalogUp ? "Healthy" : "Unreachable",
                orderingService = orderingUp ? "Healthy" : "Unreachable"
            }
        };

        return allUp
            ? Ok(responseBody)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, responseBody);
    }
}
