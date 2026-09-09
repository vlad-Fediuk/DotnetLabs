using LR0.Features.Catalog.DTOs;

namespace LR0.Features.Catalog;

public interface ICatalogService
{
    Task<IReadOnlyList<CategoryResponse>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
    Task<CategoryResponse> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryResponse> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);
}
