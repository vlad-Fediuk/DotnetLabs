using Microsoft.Extensions.Logging;
using CatalogService.Features.Categories;
using CatalogService.Features.Categories.DTOs;
using CatalogService.Features.Items.DTOs;
using CatalogService.Models;
using ECommerce.Shared.Common;
using ECommerce.Shared.Exceptions;

namespace CatalogService.Features.Items;

public class CatalogService : ICatalogService
{
    private readonly ICatalogRepository _catalogRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<CatalogService> _logger;

    public CatalogService(
        ICatalogRepository catalogRepository,
        ICategoryRepository categoryRepository,
        ILogger<CatalogService> logger)
    {
        _catalogRepository = catalogRepository;
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public async Task<PagedResult<ItemDto>> GetItemsAsync(CatalogFilterParams filter, CancellationToken ct = default)
    {
        var (items, totalCount) = await _catalogRepository.GetPagedItemsAsync(filter, ct);

        var dtos = items.Select(MapToDto).ToList();
        return new PagedResult<ItemDto>(dtos, totalCount, filter.Page ?? 1, filter.PageSize ?? 10);
    }

    public async Task<ItemDto> GetItemByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _catalogRepository.GetItemByIdWithCategoryAsync(id, ct);
        if (item == null || !item.IsActive)
        {
            throw new NotFoundException($"Catalog item with ID '{id}' was not found.");
        }

        return MapToDto(item);
    }

    public async Task<ItemDto> CreateItemAsync(CreateItemRequest request, CancellationToken ct = default)
    {
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, ct);
        if (category == null)
        {
            throw new BadRequestException($"Category with ID '{request.CategoryId}' does not exist.");
        }

        if (await _catalogRepository.ExistsSkuAsync(request.Sku, null, ct))
        {
            throw new ConflictException($"Item with SKU '{request.Sku}' already exists.");
        }

        var item = new Item
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Sku = request.Sku.Trim().ToUpperInvariant(),
            Description = request.Description?.Trim(),
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            CategoryId = request.CategoryId,
            Category = category,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _catalogRepository.AddAsync(item, ct);
        await _catalogRepository.SaveChangesAsync(ct);

        _logger.LogInformation("Catalog item created with ID {ItemId} and SKU {Sku}", item.Id, item.Sku);

        return MapToDto(item);
    }

    public async Task UpdateItemAsync(Guid id, UpdateItemRequest request, CancellationToken ct = default)
    {
        var item = await _catalogRepository.GetItemByIdWithCategoryAsync(id, ct);
        if (item == null || !item.IsActive)
        {
            throw new NotFoundException($"Catalog item with ID '{id}' was not found.");
        }

        if (item.CategoryId != request.CategoryId)
        {
            var categoryExists = await _categoryRepository.ExistsAsync(request.CategoryId, ct);
            if (!categoryExists)
            {
                throw new BadRequestException($"Category with ID '{request.CategoryId}' does not exist.");
            }
        }

        if (await _catalogRepository.ExistsSkuAsync(request.Sku, id, ct))
        {
            throw new ConflictException($"Item with SKU '{request.Sku}' already exists.");
        }

        item.Title = request.Title.Trim();
        item.Sku = request.Sku.Trim().ToUpperInvariant();
        item.Description = request.Description?.Trim();
        item.Price = request.Price;
        item.StockQuantity = request.StockQuantity;
        item.CategoryId = request.CategoryId;

        _catalogRepository.Update(item);
        await _catalogRepository.SaveChangesAsync(ct);

        _logger.LogInformation("Catalog item updated: {ItemId}", id);
    }

    public async Task DeleteItemAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _catalogRepository.GetByIdAsync(id, ct);
        if (item == null || !item.IsActive)
        {
            throw new NotFoundException($"Catalog item with ID '{id}' was not found.");
        }

        item.IsActive = false;
        _catalogRepository.Update(item);
        await _catalogRepository.SaveChangesAsync(ct);

        _logger.LogInformation("Catalog item soft-deleted: {ItemId}", id);
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var categories = await _categoryRepository.GetAllAsync(ct);
        return categories.Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.Description)).ToList();
    }

    private static ItemDto MapToDto(Item item)
    {
        return new ItemDto(
            item.Id,
            item.Title,
            item.Sku,
            item.Description,
            item.Price,
            item.StockQuantity,
            item.CategoryId,
            item.Category?.Name ?? string.Empty,
            item.IsActive,
            item.CreatedAtUtc
        );
    }
}
