using System.Security.Claims;
using ECommerce.Shared.Filters;
using Microsoft.AspNetCore.Mvc;
using OrderingService.Domain;
using OrderingService.Features.Orders.DTOs;

namespace OrderingService.Features.Orders;

public static class OrderEndpoints
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";
    private const string IdempotencyReplayedHeader = "Idempotency-Replayed";

    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders")
            .RequireAuthorization();

        group.MapPost("/", async (
            CreateOrderRequest request,
            [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
            ClaimsPrincipal principal,
            HttpContext httpContext,
            IOrderingService orderingService,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            var result = await orderingService.CreateOrderAsync(userId, idempotencyKey, request, ct);

            if (result.Replayed)
            {
                httpContext.Response.Headers[IdempotencyReplayedHeader] = "true";
            }

            return Results.Created($"/api/orders/{result.Order.Id}", result.Order);
        })
        .AddEndpointFilter<ValidationFilter<CreateOrderRequest>>()
        .WithName("CreateOrder")
        .WithSummary("Оформление нового заказа (требуется авторизация)")
        .WithDescription("Создает заказ текущего пользователя. UserId берется из JWT. Наличие и цена товаров проверяются в CatalogService по gRPC (дедлайн 2 с). Повторный запрос с тем же заголовком Idempotency-Key возвращает уже созданный заказ без создания дубликата.")
        .Produces<OrderResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapGet("/", async (
            OrderStatus? status,
            int? page,
            int? pageSize,
            ClaimsPrincipal principal,
            IOrderingService orderingService,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            var orders = await orderingService.GetUserOrdersAsync(userId, status, page, pageSize, ct);
            return Results.Ok(orders);
        })
        .WithName("GetUserOrders")
        .WithSummary("Получение списка заказов текущего пользователя")
        .WithDescription("Возвращает заказы текущего пользователя от новых к старым. Необязательные параметры: status — фильтр по статусу, page и pageSize — постраничный вывод (без pageSize возвращаются все заказы).")
        .Produces<IReadOnlyList<OrderResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal principal,
            IOrderingService orderingService,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            var order = await orderingService.GetOrderByIdAsync(id, userId, principal.IsInRole("Admin"), ct);
            return Results.Ok(order);
        })
        .WithName("GetOrderById")
        .WithSummary("Получение информации о заказе по ID")
        .WithDescription("Доступно владельцу заказа или администратору. Для чужого заказа возвращается 404, чтобы не раскрывать его существование.")
        .Produces<OrderResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/cancel", async (
            Guid id,
            ClaimsPrincipal principal,
            IOrderingService orderingService,
            CancellationToken ct) =>
        {
            if (!TryGetUserId(principal, out var userId))
            {
                return Results.Unauthorized();
            }

            var order = await orderingService.CancelOrderAsync(id, userId, principal.IsInRole("Admin"), ct);
            return Results.Ok(order);
        })
        .WithName("CancelOrder")
        .WithSummary("Отмена заказа")
        .WithDescription("Переводит заказ в статус Cancelled (запись не удаляется). Повторная отмена возвращает 200 с текущим состоянием. Отправленный или доставленный заказ отменить нельзя (409).")
        .Produces<OrderResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPatch("/{id:guid}/status", async (
            Guid id,
            UpdateOrderStatusRequest request,
            IOrderingService orderingService,
            CancellationToken ct) =>
        {
            var updated = await orderingService.UpdateOrderStatusAsync(id, request.Status, ct);
            return Results.Ok(updated);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<UpdateOrderStatusRequest>>()
        .WithName("UpdateOrderStatus")
        .WithSummary("Обновление статуса заказа (только для Admin)")
        .WithDescription("Изменяет статус любого заказа. Статус отмененного заказа изменить нельзя.")
        .Produces<OrderResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
    {
        return Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}
