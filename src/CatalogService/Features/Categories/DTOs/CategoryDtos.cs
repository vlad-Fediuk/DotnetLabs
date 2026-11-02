namespace CatalogService.Features.Categories.DTOs;

public record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description
);

public record CreateCategoryRequest(
    string Name,
    string Slug,
    string? Description
);

public record UpdateCategoryRequest(
    string Name,
    string Slug,
    string? Description
);
