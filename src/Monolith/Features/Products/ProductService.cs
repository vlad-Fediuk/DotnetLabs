using Microsoft.EntityFrameworkCore;
using LR0.Common.Exceptions;
using LR0.Database;
using LR0.Domain;
using LR0.Features.Products.DTOs;

namespace LR0.Features.Products;

public class ProductService : IProductService
{
    private readonly AppDbContext _dbContext;

    public ProductService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ProductResponse>> GetProductsAsync(ProductFilterParams filter, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(search) || p.Sku.ToLower().Contains(search));
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
        }

        if (filter.MinPrice.HasValue && filter.MaxPrice.HasValue && filter.MinPrice > filter.MaxPrice)
        {
            throw new BadRequestException("Минимальная цена не может быть больше максимальной.");
        }

        if ((filter.MinPrice.HasValue && filter.MinPrice < 0) || (filter.MaxPrice.HasValue && filter.MaxPrice < 0))
        {
            throw new BadRequestException("Цена не может быть отрицательной.");
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= filter.MaxPrice.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductResponse(
                p.Id,
                p.Name,
                p.Sku,
                p.Description,
                p.Price,
                p.StockQuantity,
                p.CategoryId,
                p.Category.Name,
                p.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductResponse>(items, totalCount, page, pageSize);
    }

    public async Task<ProductResponse> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.Id == id)
            .Select(p => new ProductResponse(
                p.Id,
                p.Name,
                p.Sku,
                p.Description,
                p.Price,
                p.StockQuantity,
                p.CategoryId,
                p.Category.Name,
                p.CreatedAtUtc
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
        {
            throw new NotFoundException($"Товар с Id '{id}' не найден.");
        }

        return product;
    }

    public async Task<ProductResponse> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var sku = request.Sku.Trim().ToUpperInvariant();

        var skuExists = await _dbContext.Products.AnyAsync(p => p.Sku == sku, cancellationToken);
        if (skuExists)
        {
            throw new ConflictException($"Товар с артикулом (SKU) '{sku}' уже существует.");
        }

        var category = await _dbContext.Categories.FindAsync([request.CategoryId], cancellationToken);
        if (category is null)
        {
            throw new NotFoundException($"Категория с Id '{request.CategoryId}' не найдена.");
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Sku = sku,
            Description = request.Description?.Trim(),
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            CategoryId = request.CategoryId,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProductResponse(
            product.Id,
            product.Name,
            product.Sku,
            product.Description,
            product.Price,
            product.StockQuantity,
            product.CategoryId,
            category.Name,
            product.CreatedAtUtc
        );
    }

    public async Task<ProductResponse> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException($"Товар с Id '{id}' не найден.");
        }

        var sku = request.Sku.Trim().ToUpperInvariant();
        var skuExists = await _dbContext.Products
            .AnyAsync(p => p.Sku == sku && p.Id != id, cancellationToken);

        if (skuExists)
        {
            throw new ConflictException($"Товар с артикулом (SKU) '{sku}' уже существует.");
        }

        var category = await _dbContext.Categories.FindAsync([request.CategoryId], cancellationToken);
        if (category is null)
        {
            throw new NotFoundException($"Категория с Id '{request.CategoryId}' не найдена.");
        }

        product.Name = request.Name.Trim();
        product.Sku = sku;
        product.Description = request.Description?.Trim();
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.CategoryId = request.CategoryId;
        product.Category = category;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ProductResponse(
            product.Id,
            product.Name,
            product.Sku,
            product.Description,
            product.Price,
            product.StockQuantity,
            product.CategoryId,
            category.Name,
            product.CreatedAtUtc
        );
    }

    public async Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products.FindAsync([id], cancellationToken);
        if (product is null)
        {
            throw new NotFoundException($"Товар с Id '{id}' не найден.");
        }

        var hasOrders = await _dbContext.OrderItems.AnyAsync(oi => oi.ProductId == id, cancellationToken);
        if (hasOrders)
        {
            throw new ConflictException("Невозможно удалить товар, так как он присутствует в оформленных заказах.");
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
