namespace AuthService.Features.Users.DTOs;

public record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    DateTime CreatedAtUtc,
    bool IsActive
);

public record UpdateRolesRequest(
    IReadOnlyList<string> Roles
);
