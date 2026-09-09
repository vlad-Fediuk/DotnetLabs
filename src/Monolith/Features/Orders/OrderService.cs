using System.Data;
using Microsoft.EntityFrameworkCore;
using LR0.Common.Exceptions;
using LR0.Database;
using LR0.Domain;
using LR0.Domain.Enums;
using LR0.Features.Orders.DTOs;

namespace LR0.Features.Orders;

public class OrderService : IOrderService
{
    private readonly AppDbContext _dbContext;

    public OrderService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OrderResponse> CreateOrderAsync(Guid userId, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var userExists = await _dbContext.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists)
        {
            throw new NotFoundException("Пользователь не найден.");
        }

        var aggregatedItems = request.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new OrderItemRequest(g.Key, g.Sum(x => x.Quantity)))
            .ToList();

        var productIds = aggregatedItems.Select(i => i.ProductId).ToList();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);

        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var item in aggregatedItems)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
            {
                throw new NotFoundException($"Товар с Id '{item.ProductId}' не найден.");
            }

            if (product.StockQuantity < item.Quantity)
            {
                throw new ConflictException($"Недостаточно товара '{product.Name}' на складе. Доступно: {product.StockQuantity}, запрошено: {item.Quantity}.");
            }
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            Status = OrderStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        decimal total = 0;
        var orderItems = new List<OrderItem>();

        foreach (var item in aggregatedItems)
        {
            var product = products[item.ProductId];

            product.StockQuantity -= item.Quantity;

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            };

            orderItems.Add(orderItem);
            total += orderItem.UnitPrice * orderItem.Quantity;
        }

        order.TotalAmount = total;
        order.Items = orderItems;

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var itemDtos = order.Items.Select(oi =>
        {
            var prod = products[oi.ProductId];
            return new OrderItemResponse(
                oi.ProductId,
                prod.Name,
                oi.Quantity,
                oi.UnitPrice,
                oi.UnitPrice * oi.Quantity
            );
        }).ToList();

        return new OrderResponse(
            order.Id,
            order.OrderNumber,
            order.UserId,
            order.Status,
            order.TotalAmount,
            order.CreatedAtUtc,
            itemDtos
        );
    }

    public async Task<IReadOnlyList<OrderResponse>> GetUserOrdersAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return orders.Select(MapToResponse).ToList();
    }

    public async Task<OrderResponse> GetOrderByIdAsync(Guid orderId, Guid userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Where(o => o.Id == orderId);

        if (!isAdmin)
        {
            query = query.Where(o => o.UserId == userId);
        }

        var order = await query.FirstOrDefaultAsync(cancellationToken);
        if (order is null)
        {
            throw new NotFoundException($"Заказ с Id '{orderId}' не найден.");
        }

        return MapToResponse(order);
    }

    public async Task<OrderResponse> UpdateOrderStatusAsync(Guid orderId, OrderStatus newStatus, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);

        var order = await _dbContext.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException($"Заказ с Id '{orderId}' не найден.");
        }

        if (order.Status == OrderStatus.Cancelled && newStatus != OrderStatus.Cancelled)
        {
            throw new ConflictException("Невозможно изменить статус уже отмененного заказа.");
        }

        if (order.Status != OrderStatus.Cancelled && newStatus == OrderStatus.Cancelled)
        {
            foreach (var item in order.Items)
            {
                item.Product.StockQuantity += item.Quantity;
            }
        }

        order.Status = newStatus;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return MapToResponse(order);
    }

    private static OrderResponse MapToResponse(Order order)
    {
        var items = order.Items.Select(i => new OrderItemResponse(
            i.ProductId,
            i.Product.Name,
            i.Quantity,
            i.UnitPrice,
            i.UnitPrice * i.Quantity
        )).ToList();

        return new OrderResponse(
            order.Id,
            order.OrderNumber,
            order.UserId,
            order.Status,
            order.TotalAmount,
            order.CreatedAtUtc,
            items
        );
    }
}
