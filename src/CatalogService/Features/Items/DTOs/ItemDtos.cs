namespace CatalogService.Features.Items.DTOs;

public record ItemDto(
    Guid Id,
    string Title,
    string Sku,
    string? Description,
    decimal Price,
    int StockQuantity,
    Guid CategoryId,
    string CategoryName,
    bool IsActive,
    DateTime CreatedAtUtc
);

public record CreateItemRequest(
    string Title,
    string Sku,
    string? Description,
    decimal Price,
    int StockQuantity,
    Guid CategoryId
);

public record UpdateItemRequest(
    string Title,
    string Sku,
    string? Description,
    decimal Price,
    int StockQuantity,
    Guid CategoryId
);

public record CatalogFilterParams
{
    public int? Page { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
    public string? Search { get; init; }
    public Guid? CategoryId { get; init; }
    public string? SortBy { get; init; }
    public bool? Desc { get; init; } = false;
}
