using LR0.Common.Filters;
using LR0.Features.Catalog.DTOs;

namespace LR0.Features.Catalog;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categories")
            .WithTags("Catalog");

        group.MapGet("/", async (ICatalogService catalogService, CancellationToken ct) =>
        {
            var categories = await catalogService.GetAllCategoriesAsync(ct);
            return Results.Ok(categories);
        })
        .WithName("GetCategories")
        .WithSummary("Получение всех категорий каталога");

        group.MapGet("/{id:guid}", async (Guid id, ICatalogService catalogService, CancellationToken ct) =>
        {
            var category = await catalogService.GetCategoryByIdAsync(id, ct);
            return Results.Ok(category);
        })
        .WithName("GetCategoryById")
        .WithSummary("Получение категории по ID");

        group.MapPost("/", async (CreateCategoryRequest request, ICatalogService catalogService, CancellationToken ct) =>
        {
            var created = await catalogService.CreateCategoryAsync(request, ct);
            return Results.Created($"/api/categories/{created.Id}", created);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<CreateCategoryRequest>>()
        .WithName("CreateCategory")
        .WithSummary("Создание новой категории (только для Admin)");

        group.MapPut("/{id:guid}", async (Guid id, UpdateCategoryRequest request, ICatalogService catalogService, CancellationToken ct) =>
        {
            var updated = await catalogService.UpdateCategoryAsync(id, request, ct);
            return Results.Ok(updated);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<UpdateCategoryRequest>>()
        .WithName("UpdateCategory")
        .WithSummary("Обновление категории (только для Admin)");

        group.MapDelete("/{id:guid}", async (Guid id, ICatalogService catalogService, CancellationToken ct) =>
        {
            await catalogService.DeleteCategoryAsync(id, ct);
            return Results.NoContent();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("DeleteCategory")
        .WithSummary("Удаление категории (только для Admin)");

        return app;
    }
}
