using ApiGateway.Clients;
using ApiGateway.DTOs;
using ECommerce.Shared.Exceptions;

namespace ApiGateway.Aggregators;

public interface IItemDetailsScreenAggregator
{
    Task<ItemDetailsScreenDto> AggregateAsync(Guid itemId, Guid userId, string? bearerToken, CancellationToken cancellationToken);
}

public class ItemDetailsScreenAggregator : IItemDetailsScreenAggregator
{
    private readonly ICatalogClient _catalogClient;
    private readonly IOrderingClient _orderingClient;

    public ItemDetailsScreenAggregator(
        ICatalogClient catalogClient,
        IOrderingClient orderingClient)
    {
        _catalogClient = catalogClient;
        _orderingClient = orderingClient;
    }

    public async Task<ItemDetailsScreenDto> AggregateAsync(Guid itemId, Guid userId, string? bearerToken, CancellationToken cancellationToken)
    {
        var itemTask = _catalogClient.GetItemAsync(itemId, cancellationToken);
        var ordersTask = _orderingClient.GetUserOrdersAsync(userId, bearerToken, cancellationToken);

        await Task.WhenAll(itemTask, ordersTask);

        var item = await itemTask;
        if (item is null)
        {
            throw new NotFoundException($"Товар з Id '{itemId}' не знайдено.");
        }

        var ordersResult = await ordersTask;
        if (!ordersResult.IsSuccess)
        {
            return new ItemDetailsScreenDto(
                item,
                null,
                "Unavailable",
                ordersResult.ErrorMessage ?? "Сервіс замовлень тимчасово недоступний."
            );
        }

        var matchingOrder = ordersResult.Orders
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefault(o => o.Items.Any(i => i.ProductId == itemId));

        ActiveOperationDto? activeOperation = null;
        if (matchingOrder is not null)
        {
            var line = matchingOrder.Items.First(i => i.ProductId == itemId);
            activeOperation = new ActiveOperationDto(
                matchingOrder.Id,
                matchingOrder.OrderNumber,
                matchingOrder.Status,
                line.Quantity,
                line.TotalPrice,
                matchingOrder.CreatedAtUtc
            );
        }

        return new ItemDetailsScreenDto(
            item,
            activeOperation,
            "Available",
            null
        );
    }
}
