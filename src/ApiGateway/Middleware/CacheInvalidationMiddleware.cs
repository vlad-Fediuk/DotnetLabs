using System.Text.RegularExpressions;
using ApiGateway.Clients;

namespace ApiGateway.Middleware;

public partial class CacheInvalidationMiddleware
{
    private readonly RequestDelegate _next;

    public CacheInvalidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICatalogClient catalogClient)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        var isMutationMethod = HttpMethods.IsPost(method) ||
                               HttpMethods.IsPut(method) ||
                               HttpMethods.IsDelete(method) ||
                               HttpMethods.IsPatch(method);

        var isCatalogMutation = isMutationMethod &&
                                (path.StartsWith("/api/v1/catalog/items", StringComparison.OrdinalIgnoreCase) ||
                                 path.StartsWith("/api/v1/catalog/categories", StringComparison.OrdinalIgnoreCase));

        await _next(context);

        if (isCatalogMutation && context.Response.StatusCode is >= 200 and < 300)
        {
            var match = ItemIdRouteRegex().Match(path);
            if (match.Success && Guid.TryParse(match.Groups["id"].Value, out var itemId))
            {
                catalogClient.InvalidateItem(itemId);
            }
            else
            {
                catalogClient.InvalidateAll();
            }
        }
    }

    [GeneratedRegex(@"^/api/v1/catalog/items/(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})/?$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ItemIdRouteRegex();
}
