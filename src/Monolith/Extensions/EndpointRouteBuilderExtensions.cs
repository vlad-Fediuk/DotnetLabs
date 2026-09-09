using LR0.Features.Auth;
using LR0.Features.Catalog;
using LR0.Features.Orders;
using LR0.Features.Products;

namespace LR0.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAppEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapAuthEndpoints();
        app.MapCatalogEndpoints();
        app.MapProductEndpoints();
        app.MapOrderEndpoints();

        return app;
    }
}
