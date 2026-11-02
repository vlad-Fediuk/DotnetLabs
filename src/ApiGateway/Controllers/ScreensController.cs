using ApiGateway.Aggregators;
using ApiGateway.DTOs;
using ECommerce.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/v1/screens")]
[Tags("Composed Screens")]
public class ScreensController : ControllerBase
{
    private readonly IItemDetailsScreenAggregator _aggregator;

    public ScreensController(IItemDetailsScreenAggregator aggregator)
    {
        _aggregator = aggregator;
    }

    [HttpGet("item-details/{id:guid}")]
    [Authorize]
    [EnableRateLimiting("general")]
    [ProducesResponseType(typeof(ItemDetailsScreenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetItemDetailsScreen(Guid id, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var bearerToken = Request.Headers.Authorization.FirstOrDefault();
        var screenData = await _aggregator.AggregateAsync(id, userId, bearerToken, ct);
        return Ok(screenData);
    }
}
