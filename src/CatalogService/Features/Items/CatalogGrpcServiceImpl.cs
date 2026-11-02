using CatalogService.Models;
using ECommerce.Shared.Protos;
using Grpc.Core;

namespace CatalogService.Features.Items;

public class CatalogGrpcServiceImpl : CatalogGrpcService.CatalogGrpcServiceBase
{
    private readonly ICatalogRepository _repository;

    public CatalogGrpcServiceImpl(ICatalogRepository repository)
    {
        _repository = repository;
    }

    public override async Task<CatalogItemResponse> GetItem(GetItemRequest request, ServerCallContext context)
    {
        var itemId = ParseId(request.Id);
        var item = await _repository.GetByIdAsync(itemId, context.CancellationToken);
        if (item is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Item '{itemId}' was not found."));
        }

        return ToResponse(item);
    }

    public override async Task<CatalogItemsResponse> GetItems(GetItemsRequest request, ServerCallContext context)
    {
        var itemIds = request.Ids.Select(ParseId).Distinct().ToList();
        var items = await _repository.FindAsync(i => itemIds.Contains(i.Id), context.CancellationToken);

        var response = new CatalogItemsResponse();
        response.Items.AddRange(items.Select(ToResponse));
        return response;
    }

    public override async Task<CheckStockResponse> CheckStock(CheckStockRequest request, ServerCallContext context)
    {
        var itemId = ParseId(request.ItemId);
        var item = await _repository.GetByIdAsync(itemId, context.CancellationToken);
        if (item is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Item '{itemId}' was not found."));
        }

        return new CheckStockResponse
        {
            IsAvailable = item.IsActive && item.StockQuantity >= request.Quantity,
            AvailableQuantity = item.StockQuantity
        };
    }

    private static Guid ParseId(string value)
    {
        if (!Guid.TryParse(value, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"'{value}' is not a valid item identifier."));
        }

        return id;
    }

    private static CatalogItemResponse ToResponse(Item item) => new()
    {
        Id = item.Id.ToString(),
        Title = item.Title,
        Sku = item.Sku,
        Price = decimal.ToDouble(item.Price),
        StockQuantity = item.StockQuantity,
        IsActive = item.IsActive,
        CategoryId = item.CategoryId.ToString()
    };
}
