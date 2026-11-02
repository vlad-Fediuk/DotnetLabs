using CatalogService.Data;
using CatalogService.Models;
using Microsoft.EntityFrameworkCore;

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

    public async Task<bool> ExistsSlugAsync(string slug, Guid? excludeId = null, CancellationToken ct = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        var query = DbSet.AsNoTracking().Where(c => c.Slug.ToLower() == normalized);
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync(ct);
    }

    public async Task<bool> HasItemsAsync(Guid categoryId, CancellationToken ct = default)
    {
        return await Context.Set<Item>().AnyAsync(i => i.CategoryId == categoryId && i.IsActive, ct);
    }
}
