using System.Security.Claims;
using LR0.Common.Filters;
using LR0.Features.Auth.DTOs;

namespace LR0.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest request, IAuthService authService, CancellationToken ct) =>
        {
            var result = await authService.RegisterAsync(request, ct);
            return Results.Ok(result);
        })
        .AddEndpointFilter<ValidationFilter<RegisterRequest>>()
        .WithName("Register")
        .WithSummary("Регистрация нового пользователя");

        group.MapPost("/login", async (LoginRequest request, IAuthService authService, CancellationToken ct) =>
        {
            var result = await authService.LoginAsync(request, ct);
            return Results.Ok(result);
        })
        .AddEndpointFilter<ValidationFilter<LoginRequest>>()
        .WithName("Login")
        .WithSummary("Вход в систему и получение JWT токена");

        group.MapGet("/me", async (ClaimsPrincipal user, IAuthService authService, CancellationToken ct) =>
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var profile = await authService.GetCurrentUserAsync(userId, ct);
            return Results.Ok(profile);
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .WithSummary("Получение профиля текущего авторизованного пользователя");

        return app;
    }
}
