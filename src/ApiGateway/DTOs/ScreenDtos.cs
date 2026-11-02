namespace ApiGateway.DTOs;

public record CatalogItemSummaryDto(
    Guid Id,
    string Title,
    string Sku,
    string? Description,
    decimal Price,
    int StockQuantity,
    string CategoryName,
    bool IsActive
);

public record OrderItemSummaryDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice
);

public record OrderSummaryDto(
    Guid Id,
    string OrderNumber,
    int Status,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderItemSummaryDto> Items
);

public record ActiveOperationDto(
    Guid OrderId,
    string OrderNumber,
    int Status,
    int Quantity,
    decimal TotalPrice,
    DateTime CreatedAtUtc
);

public record ItemDetailsScreenDto(
    CatalogItemSummaryDto Item,
    ActiveOperationDto? MyActiveOperation,
    string ActiveOperationStatus,
    string? ActiveOperationMessage
);

public record UserOrdersResult(
    bool IsSuccess,
    IReadOnlyList<OrderSummaryDto> Orders,
    string? ErrorMessage = null
);

public record CacheMetricsDto(
    long Hits,
    long Misses,
    long TotalRequests,
    double HitRatio,
    long Evictions
);
