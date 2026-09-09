using LR0.Domain.Enums;
using LR0.Features.Orders.DTOs;

namespace LR0.Features.Orders;

public interface IOrderService
{
    Task<OrderResponse> CreateOrderAsync(Guid userId, CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderResponse>> GetUserOrdersAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<OrderResponse> GetOrderByIdAsync(Guid orderId, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<OrderResponse> UpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus, CancellationToken cancellationToken = default);
}
