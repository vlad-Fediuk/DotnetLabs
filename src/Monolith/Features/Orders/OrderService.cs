using Microsoft.EntityFrameworkCore;
using Grpc.Core;
using ECommerce.Shared.Protos;
using LR0.Common.Exceptions;
using LR0.Database;
using LR0.Domain;
using LR0.Domain.Enums;
using LR0.Features.Orders.DTOs;

namespace LR0.Features.Orders;

public class OrderService : IOrderService
{
    private static readonly TimeSpan CatalogCallTimeout = TimeSpan.FromSeconds(2);

    private readonly AppDbContext _dbContext;
    private readonly CatalogGrpcService.CatalogGrpcServiceClient _catalogClient;

    public OrderService(AppDbContext dbContext, CatalogGrpcService.CatalogGrpcServiceClient catalogClient)
    {
        _dbContext = dbContext;
        _catalogClient = catalogClient;
    }

    public async Task<OrderResponse> CreateOrderAsync(Guid userId, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var aggregatedItems = request.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new OrderItemRequest(g.Key, g.Sum(x => x.Quantity)))
            .ToList();

        var catalogItems = await LoadCatalogItemsAsync(aggregatedItems.Select(i => i.ProductId), cancellationToken);

        foreach (var item in aggregatedItems)
        {
            if (!catalogItems.TryGetValue(item.ProductId, out var catalogItem) || !catalogItem.IsActive)
            {
                throw new NotFoundException($"Товар с Id '{item.ProductId}' не найден.");
            }

            if (catalogItem.StockQuantity < item.Quantity)
            {
                throw new ConflictException($"Недостаточно товара '{catalogItem.Title}' на складе. Доступно: {catalogItem.StockQuantity}, запрошено: {item.Quantity}.");
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
            var catalogItem = catalogItems[item.ProductId];

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = item.ProductId,
                ProductName = catalogItem.Title,
                Quantity = item.Quantity,
                UnitPrice = Convert.ToDecimal(catalogItem.Price)
            };

            orderItems.Add(orderItem);
            total += orderItem.UnitPrice * orderItem.Quantity;
        }

        order.TotalAmount = total;
        order.Items = orderItems;

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(order);
    }

    public async Task<IReadOnlyList<OrderResponse>> GetUserOrdersAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var orders = await _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
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
        var order = await _dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
        {
            throw new NotFoundException($"Заказ с Id '{orderId}' не найден.");
        }

        if (order.Status == OrderStatus.Cancelled && newStatus != OrderStatus.Cancelled)
        {
            throw new ConflictException("Невозможно изменить статус уже отмененного заказа.");
        }

        order.Status = newStatus;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(order);
    }

    private async Task<Dictionary<Guid, CatalogItemResponse>> LoadCatalogItemsAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken)
    {
        var request = new GetItemsRequest();
        request.Ids.AddRange(productIds.Select(id => id.ToString()));

        try
        {
            var response = await _catalogClient.GetItemsAsync(
                request,
                deadline: DateTime.UtcNow.Add(CatalogCallTimeout),
                cancellationToken: cancellationToken);

            return response.Items.ToDictionary(i => Guid.Parse(i.Id));
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            throw new NotFoundException("Товар не найден.");
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.DeadlineExceeded or StatusCode.Unavailable)
        {
            throw new ServiceUnavailableException("Каталог товаров временно недоступен. Повторите запрос позже.");
        }
    }

    private static OrderResponse MapToResponse(Order order)
    {
        var items = order.Items.Select(i => new OrderItemResponse(
            i.ProductId,
            i.ProductName,
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
