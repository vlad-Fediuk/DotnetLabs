using CatalogService.Data;
using CatalogService.Features.Items.DTOs;
using CatalogService.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Features.Items;

public class CatalogRepository : BaseRepository<Item>, ICatalogRepository
{
    public CatalogRepository(CatalogDbContext context) : base(context)
    {
    }

    public async Task<(IReadOnlyList<Item> Items, int TotalCount)> GetPagedItemsAsync(CatalogFilterParams filter, CancellationToken ct = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(i => i.Category)
            .Where(i => i.IsActive);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLowerInvariant();
            query = query.Where(i => i.Title.ToLower().Contains(term) ||
                                     (i.Description != null && i.Description.ToLower().Contains(term)) ||
                                     i.Sku.ToLower().Contains(term));
        }

        if (filter.CategoryId.HasValue && filter.CategoryId.Value != Guid.Empty)
        {
            query = query.Where(i => i.CategoryId == filter.CategoryId.Value);
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(i => i.Price >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(i => i.Price <= filter.MaxPrice.Value);
        }

        var isDesc = filter.Desc == true;
        query = filter.SortBy?.ToLowerInvariant() switch
        {
            "price" => isDesc ? query.OrderByDescending(i => i.Price) : query.OrderBy(i => i.Price),
            "title" => isDesc ? query.OrderByDescending(i => i.Title) : query.OrderBy(i => i.Title),
            "created" => isDesc ? query.OrderByDescending(i => i.CreatedAtUtc) : query.OrderBy(i => i.CreatedAtUtc),
            _ => isDesc ? query.OrderByDescending(i => i.CreatedAtUtc) : query.OrderBy(i => i.Title)
        };

        var totalCount = await query.CountAsync(ct);

        var page = filter.Page is null or < 1 ? 1 : filter.Page.Value;
        var pageSize = filter.PageSize is null or < 1 ? 10 : (filter.PageSize.Value > 100 ? 100 : filter.PageSize.Value);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<Item?> GetItemByIdWithCategoryAsync(Guid id, CancellationToken ct = default)
    {
        return await DbSet
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    public async Task<bool> ExistsSkuAsync(string sku, Guid? excludeId = null, CancellationToken ct = default)
    {
        var query = DbSet.AsNoTracking().Where(i => i.Sku.ToLower() == sku.ToLower());
        if (excludeId.HasValue)
        {
            query = query.Where(i => i.Id != excludeId.Value);
        }

        return await query.AnyAsync(ct);
    }
}
