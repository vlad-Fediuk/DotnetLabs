using System.Security.Claims;
using ECommerce.Shared.Exceptions;

namespace ECommerce.Shared.Security;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        if (principal.TryGetUserId(out var userId))
        {
            return userId;
        }

        throw new UnauthorizedException("User identifier claim is missing or invalid.");
    }

    public static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId)
    {
        var rawId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? principal.FindFirstValue("sub");

        return Guid.TryParse(rawId, out userId);
    }

    public static IReadOnlyList<string> GetRoles(this ClaimsPrincipal principal)
    {
        return principal.FindAll(ClaimTypes.Role)
            .Concat(principal.FindAll("role"))
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<string> GetPermissions(this ClaimsPrincipal principal)
    {
        var directPermissions = principal.FindAll(Permissions.ClaimType)
            .Select(c => c.Value);

        var rolePermissions = RolePermissions.GetPermissionsForRoles(principal.GetRoles());

        return directPermissions
            .Concat(rolePermissions)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool HasPermission(this ClaimsPrincipal principal, string permission)
    {
        var hasDirect = principal.FindAll(Permissions.ClaimType)
            .Any(c => string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase));

        if (hasDirect)
        {
            return true;
        }

        return RolePermissions.HasPermission(principal.GetRoles(), permission);
    }
}
