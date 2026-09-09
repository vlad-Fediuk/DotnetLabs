namespace LR0.Features.Products.DTOs;

public record CreateProductRequest(
    string Name,
    string Sku,
    string? Description,
    decimal Price,
    int StockQuantity,
    Guid CategoryId
);

public record UpdateProductRequest(
    string Name,
    string Sku,
    string? Description,
    decimal Price,
    int StockQuantity,
    Guid CategoryId
);

public record ProductResponse(
    Guid Id,
    string Name,
    string Sku,
    string? Description,
    decimal Price,
    int StockQuantity,
    Guid CategoryId,
    string CategoryName,
    DateTime CreatedAtUtc
);

public record ProductFilterParams(
    string? Search,
    Guid? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    int Page = 1,
    int PageSize = 10
);

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
