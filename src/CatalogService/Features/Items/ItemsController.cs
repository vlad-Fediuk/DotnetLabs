using CatalogService.Features.Items.DTOs;
using ECommerce.Shared.Common;
using ECommerce.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Features.Items;

[ApiController]
[Route("api/v1/catalog/items")]
[Tags("Catalog Items")]
public class ItemsController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public ItemsController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<ItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetItems([FromQuery] CatalogFilterParams filter, CancellationToken ct)
    {
        var result = await _catalogService.GetItemsAsync(filter, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetItemById(Guid id, CancellationToken ct)
    {
        var item = await _catalogService.GetItemByIdAsync(id, ct);
        return Ok(item);
    }

    [HttpPost]
    [HasPermission(Permissions.Catalog.Manage)]
    [ProducesResponseType(typeof(ItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateItem([FromBody] CreateItemRequest request, CancellationToken ct)
    {
        var created = await _catalogService.CreateItemAsync(request, ct);
        return Created($"/api/v1/catalog/items/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Catalog.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] UpdateItemRequest request, CancellationToken ct)
    {
        await _catalogService.UpdateItemAsync(id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Catalog.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItem(Guid id, CancellationToken ct)
    {
        await _catalogService.DeleteItemAsync(id, ct);
        return NoContent();
    }
}
