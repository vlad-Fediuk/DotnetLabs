using CatalogService.Features.Categories.DTOs;
using CatalogService.Features.Items.DTOs;
using ECommerce.Shared.Common;

namespace CatalogService.Features.Items;

public interface ICatalogService
{
    Task<PagedResult<ItemDto>> GetItemsAsync(CatalogFilterParams filter, CancellationToken ct = default);
    Task<ItemDto> GetItemByIdAsync(Guid id, CancellationToken ct = default);
    Task<ItemDto> CreateItemAsync(CreateItemRequest request, CancellationToken ct = default);
    Task UpdateItemAsync(Guid id, UpdateItemRequest request, CancellationToken ct = default);
    Task DeleteItemAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
}
