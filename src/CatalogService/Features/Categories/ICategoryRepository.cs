using CatalogService.Data;
using CatalogService.Models;

namespace CatalogService.Features.Categories;

public interface ICategoryRepository : IBaseRepository<Category>
{
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}
