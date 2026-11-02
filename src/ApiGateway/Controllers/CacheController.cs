using ApiGateway.Clients;
using ApiGateway.DTOs;
using ApiGateway.Services;
using ECommerce.Shared.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/v1/cache")]
[Tags("Cache Management")]
public class CacheController : ControllerBase
{
    private readonly ICacheMetricsService _metrics;
    private readonly ICatalogClient _catalogClient;

    public CacheController(ICacheMetricsService metrics, ICatalogClient catalogClient)
    {
        _metrics = metrics;
        _catalogClient = catalogClient;
    }

    [HttpGet("metrics")]
    [HasPermission(Permissions.System.ManageCache)]
    [EnableRateLimiting("general")]
    [ProducesResponseType(typeof(CacheMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public IActionResult GetMetrics()
    {
        return Ok(_metrics.GetMetrics());
    }

    [HttpPost("invalidate")]
    [HasPermission(Permissions.System.ManageCache)]
    [EnableRateLimiting("general")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public IActionResult Invalidate()
    {
        _catalogClient.InvalidateAll();
        return NoContent();
    }
}
