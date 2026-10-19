using OrderingService.Domain;
using OrderingService.Features.Orders.DTOs;

namespace OrderingService.Features.Orders;

public interface IOrderingService
{
    Task<CreateOrderResult> CreateOrderAsync(Guid userId, string? idempotencyKey, CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderResponse>> GetUserOrdersAsync(Guid userId, OrderStatus? status, int? page, int? pageSize, CancellationToken cancellationToken = default);
    Task<OrderResponse> GetOrderByIdAsync(Guid orderId, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<OrderResponse> CancelOrderAsync(Guid orderId, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<OrderResponse> UpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus, CancellationToken cancellationToken = default);
}
