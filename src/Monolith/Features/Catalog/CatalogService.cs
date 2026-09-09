using Microsoft.EntityFrameworkCore;
using LR0.Common.Exceptions;
using LR0.Database;
using LR0.Domain;
using LR0.Features.Catalog.DTOs;

namespace LR0.Features.Catalog;

public class CatalogService : ICatalogService
{
    private readonly AppDbContext _dbContext;

    public CatalogService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .Select(c => new CategoryResponse(
                c.Id,
                c.Name,
                c.Slug,
                c.Description,
                c.Products.Count
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryResponse> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryResponse(
                c.Id,
                c.Name,
                c.Slug,
                c.Description,
                c.Products.Count
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (category is null)
        {
            throw new NotFoundException($"Категория с Id '{id}' не найдена.");
        }

        return category;
    }

    public async Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();

        var exists = await _dbContext.Categories.AnyAsync(c => c.Slug == slug, cancellationToken);
        if (exists)
        {
            throw new ConflictException($"Категория со slug '{slug}' уже существует.");
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Slug = slug,
            Description = request.Description?.Trim()
        };

        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CategoryResponse(category.Id, category.Name, category.Slug, category.Description, 0);
    }

    public async Task<CategoryResponse> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException($"Категория с Id '{id}' не найдена.");
        }

        var slug = request.Slug.Trim().ToLowerInvariant();
        var slugTaken = await _dbContext.Categories
            .AnyAsync(c => c.Slug == slug && c.Id != id, cancellationToken);

        if (slugTaken)
        {
            throw new ConflictException($"Категория со slug '{slug}' уже существует.");
        }

        category.Name = request.Name.Trim();
        category.Slug = slug;
        category.Description = request.Description?.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        var productsCount = await _dbContext.Products.CountAsync(p => p.CategoryId == id, cancellationToken);

        return new CategoryResponse(category.Id, category.Name, category.Slug, category.Description, productsCount);
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException($"Категория с Id '{id}' не найдена.");
        }

        var hasProducts = await _dbContext.Products.AnyAsync(p => p.CategoryId == id, cancellationToken);
        if (hasProducts)
        {
            throw new ConflictException("Невозможно удалить категорию, содержащую привязанные товары.");
        }

        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
