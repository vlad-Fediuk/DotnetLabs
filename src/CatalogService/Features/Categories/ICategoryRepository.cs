using CatalogService.Data;
using CatalogService.Models;

namespace CatalogService.Features.Categories;

public interface ICategoryRepository : IBaseRepository<Category>
{
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsSlugAsync(string slug, Guid? excludeId = null, CancellationToken ct = default);
    Task<bool> HasItemsAsync(Guid categoryId, CancellationToken ct = default);
}
