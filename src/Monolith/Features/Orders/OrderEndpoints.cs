using System.Security.Claims;
using LR0.Common.Filters;
using LR0.Features.Orders.DTOs;

namespace LR0.Features.Orders;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders")
            .RequireAuthorization();

        group.MapPost("/", async (
            CreateOrderRequest request,
            ClaimsPrincipal principal,
            IOrderService orderService,
            CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var order = await orderService.CreateOrderAsync(userId, request, ct);
            return Results.Created($"/api/orders/{order.Id}", order);
        })
        .AddEndpointFilter<ValidationFilter<CreateOrderRequest>>()
        .WithName("CreateOrder")
        .WithSummary("Оформление нового заказа (требуется авторизация)");

        group.MapGet("/", async (
            ClaimsPrincipal principal,
            IOrderService orderService,
            CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var orders = await orderService.GetUserOrdersAsync(userId, ct);
            return Results.Ok(orders);
        })
        .WithName("GetUserOrders")
        .WithSummary("Получение списка заказов текущего пользователя");

        group.MapGet("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal principal,
            IOrderService orderService,
            CancellationToken ct) =>
        {
            var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var isAdmin = principal.IsInRole("Admin");
            var order = await orderService.GetOrderByIdAsync(id, userId, isAdmin, ct);
            return Results.Ok(order);
        })
        .WithName("GetOrderById")
        .WithSummary("Получение информации о заказе по ID");

        group.MapPatch("/{id:guid}/status", async (
            Guid id,
            UpdateOrderStatusRequest request,
            IOrderService orderService,
            CancellationToken ct) =>
        {
            var updated = await orderService.UpdateOrderStatusAsync(id, request.Status, ct);
            return Results.Ok(updated);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<UpdateOrderStatusRequest>>()
        .WithName("UpdateOrderStatus")
        .WithSummary("Обновление статуса заказа (только для Admin)");

        return app;
    }
}
