using ECommerce.Shared.Security;
using Microsoft.AspNetCore.Http;

namespace ApiGateway.Middleware;

public class TokenValidationAndEnrichmentMiddleware
{
    private readonly RequestDelegate _next;

    public TokenValidationAndEnrichmentMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.Headers.Remove("X-User-Id");
        context.Request.Headers.Remove("X-User-Roles");
        context.Request.Headers.Remove("X-User-Permissions");

        if (context.User.Identity?.IsAuthenticated == true &&
            context.User.TryGetUserId(out var userId))
        {
            context.Request.Headers["X-User-Id"] = userId.ToString();

            var roles = context.User.GetRoles();
            if (roles.Count > 0)
            {
                context.Request.Headers["X-User-Roles"] = string.Join(",", roles);
            }

            var permissions = context.User.GetPermissions();
            if (permissions.Count > 0)
            {
                context.Request.Headers["X-User-Permissions"] = string.Join(",", permissions);
            }
        }

        await _next(context);
    }
}
