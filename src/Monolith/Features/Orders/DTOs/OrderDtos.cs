using LR0.Domain.Enums;

namespace LR0.Features.Orders.DTOs;

public record OrderItemRequest(Guid ProductId, int Quantity);

public record CreateOrderRequest(IReadOnlyList<OrderItemRequest> Items);

public record UpdateOrderStatusRequest(OrderStatus Status);

public record OrderItemResponse(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal TotalPrice);

public record OrderResponse(
    Guid Id,
    string OrderNumber,
    Guid UserId,
    OrderStatus Status,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderItemResponse> Items
);
