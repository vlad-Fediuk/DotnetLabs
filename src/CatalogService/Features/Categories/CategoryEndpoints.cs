using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using CatalogService.Features.Categories.DTOs;
using CatalogService.Features.Items;

namespace CatalogService.Features.Categories;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalog/categories")
            .WithTags("Catalog Categories");

        group.MapGet("/", async (ICatalogService catalogService, CancellationToken ct) =>
        {
            var categories = await catalogService.GetCategoriesAsync(ct);
            return Results.Ok(categories);
        })
        .AllowAnonymous()
        .WithName("GetCatalogCategories")
        .WithSummary("Get catalog categories")
        .WithDescription("Retrieves the dictionary of product categories.")
        .Produces<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK);

        return app;
    }
}
