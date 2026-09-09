using LR0.Features.Products.DTOs;

namespace LR0.Features.Products;

public interface IProductService
{
    Task<PagedResult<ProductResponse>> GetProductsAsync(ProductFilterParams filter, CancellationToken cancellationToken = default);
    Task<ProductResponse> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductResponse> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default);
}
