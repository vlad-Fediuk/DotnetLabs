using CatalogService.Data;
using CatalogService.Features.Items.DTOs;
using CatalogService.Models;

namespace CatalogService.Features.Items;

public interface ICatalogRepository : IBaseRepository<Item>
{
    Task<(IReadOnlyList<Item> Items, int TotalCount)> GetPagedItemsAsync(CatalogFilterParams filter, CancellationToken ct = default);
    Task<Item?> GetItemByIdWithCategoryAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsSkuAsync(string sku, Guid? excludeId = null, CancellationToken ct = default);
}
