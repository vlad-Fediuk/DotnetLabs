using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ECommerce.Shared.Security;

public class GatewayIdentityMiddleware
{
    private readonly RequestDelegate _next;

    public GatewayIdentityMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            if (!context.User.TryGetUserId(out var existingUserId))
            {
                if (context.Request.Headers.TryGetValue("X-User-Id", out var headerId) &&
                    Guid.TryParse(headerId.FirstOrDefault(), out var parsedHeaderId))
                {
                    EnrichExistingIdentity(context, parsedHeaderId);
                }
                else
                {
                    context.User = new ClaimsPrincipal(new ClaimsIdentity());
                }
            }
            else
            {
                EnrichRolesAndPermissionsFromHeaders(context);
            }
        }
        else if (!context.Request.Headers.ContainsKey("Authorization") &&
                 context.Request.Headers.TryGetValue("X-User-Id", out var rawUserId) &&
                 Guid.TryParse(rawUserId.FirstOrDefault(), out var userId))
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new("sub", userId.ToString())
            };

            AppendHeaderRolesAndPermissions(context, claims);

            var identity = new ClaimsIdentity(claims, "GatewayHeader");
            context.User = new ClaimsPrincipal(identity);
        }

        await _next(context);
    }

    private static void EnrichExistingIdentity(HttpContext context, Guid userId)
    {
        if (context.User.Identity is ClaimsIdentity identity)
        {
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
            EnrichRolesAndPermissionsFromHeaders(context);
        }
    }

    private static void EnrichRolesAndPermissionsFromHeaders(HttpContext context)
    {
        if (context.User.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        var existingRoles = context.User.GetRoles();
        if (context.Request.Headers.TryGetValue("X-User-Roles", out var rolesHeader))
        {
            var headerRoles = rolesHeader.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var role in headerRoles)
            {
                if (!existingRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }
            }
        }

        if (context.Request.Headers.TryGetValue("X-User-Permissions", out var permsHeader))
        {
            var existingPerms = context.User.FindAll(Permissions.ClaimType).Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var headerPerms = permsHeader.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var perm in headerPerms)
            {
                if (!existingPerms.Contains(perm))
                {
                    identity.AddClaim(new Claim(Permissions.ClaimType, perm));
                }
            }
        }
    }

    private static void AppendHeaderRolesAndPermissions(HttpContext context, List<Claim> claims)
    {
        var roles = new List<string>();
        if (context.Request.Headers.TryGetValue("X-User-Roles", out var rolesHeader))
        {
            roles.AddRange(rolesHeader.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        if (roles.Count == 0)
        {
            roles.Add(RoleNames.User);
        }

        foreach (var role in roles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var permissions = new HashSet<string>(RolePermissions.GetPermissionsForRoles(roles), StringComparer.OrdinalIgnoreCase);
        if (context.Request.Headers.TryGetValue("X-User-Permissions", out var permsHeader))
        {
            foreach (var perm in permsHeader.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                permissions.Add(perm);
            }
        }

        foreach (var perm in permissions)
        {
            claims.Add(new Claim(Permissions.ClaimType, perm));
        }
    }
}
