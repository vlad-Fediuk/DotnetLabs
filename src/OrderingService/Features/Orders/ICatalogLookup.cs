using ECommerce.Shared.Protos;

namespace OrderingService.Features.Orders;

public interface ICatalogLookup
{
    Task<IReadOnlyDictionary<Guid, CatalogItemResponse>> GetItemsAsync(IEnumerable<Guid> itemIds, CancellationToken cancellationToken);
    Task<bool> IsReachableAsync(CancellationToken cancellationToken);
}
