using LR0.Common.Filters;
using LR0.Features.Products.DTOs;

namespace LR0.Features.Products;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products")
            .WithTags("Products");

        group.MapGet("/", async (
            [AsParameters] ProductFilterParams filter,
            IProductService productService,
            CancellationToken ct) =>
        {
            var result = await productService.GetProductsAsync(filter, ct);
            return Results.Ok(result);
        })
        .WithName("GetProducts")
        .WithSummary("Получение каталога товаров с пагинацией и фильтрами");

        group.MapGet("/{id:guid}", async (Guid id, IProductService productService, CancellationToken ct) =>
        {
            var product = await productService.GetProductByIdAsync(id, ct);
            return Results.Ok(product);
        })
        .WithName("GetProductById")
        .WithSummary("Получение детальной информации о товаре");

        group.MapPost("/", async (CreateProductRequest request, IProductService productService, CancellationToken ct) =>
        {
            var created = await productService.CreateProductAsync(request, ct);
            return Results.Created($"/api/products/{created.Id}", created);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<CreateProductRequest>>()
        .WithName("CreateProduct")
        .WithSummary("Добавление нового товара (только для Admin)");

        group.MapPut("/{id:guid}", async (Guid id, UpdateProductRequest request, IProductService productService, CancellationToken ct) =>
        {
            var updated = await productService.UpdateProductAsync(id, request, ct);
            return Results.Ok(updated);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<UpdateProductRequest>>()
        .WithName("UpdateProduct")
        .WithSummary("Обновление товара (только для Admin)");

        group.MapDelete("/{id:guid}", async (Guid id, IProductService productService, CancellationToken ct) =>
        {
            await productService.DeleteProductAsync(id, ct);
            return Results.NoContent();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("DeleteProduct")
        .WithSummary("Удаление товара (только для Admin)");

        return app;
    }
}
