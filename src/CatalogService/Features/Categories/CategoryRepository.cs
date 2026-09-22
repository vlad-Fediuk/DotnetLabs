using Microsoft.EntityFrameworkCore;
using CatalogService.Data;
using CatalogService.Models;

namespace CatalogService.Features.Categories;

public class CategoryRepository : BaseRepository<Category>, ICategoryRepository
{
    public CategoryRepository(CatalogDbContext context) : base(context)
    {
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        return await DbSet.AnyAsync(c => c.Id == id, ct);
    }
}
