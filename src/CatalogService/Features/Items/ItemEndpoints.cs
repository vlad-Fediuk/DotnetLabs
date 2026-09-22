using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using CatalogService.Features.Items.DTOs;
using ECommerce.Shared.Common;
using ECommerce.Shared.Filters;

namespace CatalogService.Features.Items;

public static class ItemEndpoints
{
    public static IEndpointRouteBuilder MapItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalog/items")
            .WithTags("Catalog Items");

        group.MapGet("/", async ([AsParameters] CatalogFilterParams filter, ICatalogService catalogService, CancellationToken ct) =>
        {
            var result = await catalogService.GetItemsAsync(filter, ct);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("GetCatalogItems")
        .WithSummary("Get paginated catalog items")
        .WithDescription("Retrieves a paginated list of active catalog items with optional filtering by search text, category, and sorting.")
        .Produces<PagedResult<ItemDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .AddEndpointFilter<ValidationFilter<CatalogFilterParams>>();

        group.MapGet("/{id:guid}", async (Guid id, ICatalogService catalogService, CancellationToken ct) =>
        {
            var item = await catalogService.GetItemByIdAsync(id, ct);
            return Results.Ok(item);
        })
        .AllowAnonymous()
        .WithName("GetCatalogItemById")
        .WithSummary("Get catalog item by ID")
        .WithDescription("Retrieves detailed information about a single catalog item.")
        .Produces<ItemDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (CreateItemRequest request, ICatalogService catalogService, CancellationToken ct) =>
        {
            var created = await catalogService.CreateItemAsync(request, ct);
            return Results.Created($"/api/v1/catalog/items/{created.Id}", created);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("CreateCatalogItem")
        .WithSummary("Create catalog item")
        .WithDescription("Creates a new catalog item. Requires Admin role.")
        .Produces<ItemDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .AddEndpointFilter<ValidationFilter<CreateItemRequest>>();

        group.MapPut("/{id:guid}", async (Guid id, UpdateItemRequest request, ICatalogService catalogService, CancellationToken ct) =>
        {
            await catalogService.UpdateItemAsync(id, request, ct);
            return Results.NoContent();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("UpdateCatalogItem")
        .WithSummary("Update catalog item")
        .WithDescription("Updates an existing catalog item. Requires Admin role.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .AddEndpointFilter<ValidationFilter<UpdateItemRequest>>();

        group.MapDelete("/{id:guid}", async (Guid id, ICatalogService catalogService, CancellationToken ct) =>
        {
            await catalogService.DeleteItemAsync(id, ct);
            return Results.NoContent();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("DeleteCatalogItem")
        .WithSummary("Delete catalog item (soft delete)")
        .WithDescription("Soft-deletes a catalog item by marking it inactive. Requires Admin role.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
