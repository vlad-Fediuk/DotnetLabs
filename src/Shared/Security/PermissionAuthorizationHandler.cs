using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ECommerce.Shared.Security;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var hasDirectClaim = context.User.Claims.Any(c =>
            string.Equals(c.Type, Permissions.ClaimType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase));

        if (hasDirectClaim)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var roles = context.User.FindAll(ClaimTypes.Role)
            .Concat(context.User.FindAll("role"))
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        if (RolePermissions.HasPermission(roles, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
