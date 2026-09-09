namespace LR0.Features.Catalog.DTOs;

public record CreateCategoryRequest(string Name, string Slug, string? Description);

public record UpdateCategoryRequest(string Name, string Slug, string? Description);

public record CategoryResponse(Guid Id, string Name, string Slug, string? Description, int ProductsCount);
