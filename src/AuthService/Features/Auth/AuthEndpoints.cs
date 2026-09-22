using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AuthService.Features.Auth.DTOs;
using ECommerce.Shared.Filters;

namespace AuthService.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest request, IAuthService authService, CancellationToken ct) =>
        {
            var response = await authService.RegisterAsync(request, ct);
            return Results.Created("/api/v1/users/me", response);
        })
        .AllowAnonymous()
        .WithName("Register")
        .WithSummary("User registration")
        .WithDescription("Creates a new user account with default User role and returns JWT token pair.")
        .Produces<AuthResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .AddEndpointFilter<ValidationFilter<RegisterRequest>>();

        group.MapPost("/login", async (LoginRequest request, IAuthService authService, CancellationToken ct) =>
        {
            var response = await authService.LoginAsync(request, ct);
            return Results.Ok(response);
        })
        .AllowAnonymous()
        .WithName("Login")
        .WithSummary("User authentication")
        .WithDescription("Authenticates user with email and password and returns JWT token pair.")
        .Produces<AuthResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .AddEndpointFilter<ValidationFilter<LoginRequest>>();

        group.MapPost("/refresh", async (RefreshTokenRequest request, IAuthService authService, CancellationToken ct) =>
        {
            var response = await authService.RefreshTokenAsync(request, ct);
            return Results.Ok(response);
        })
        .AllowAnonymous()
        .WithName("RefreshToken")
        .WithSummary("Refresh JWT access token")
        .WithDescription("Performs refresh token rotation and issues a new pair of tokens.")
        .Produces<AuthResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .AddEndpointFilter<ValidationFilter<RefreshTokenRequest>>();

        group.MapPost("/logout", async (ClaimsPrincipal principal, RefreshTokenRequest? request, IAuthService authService, CancellationToken ct) =>
        {
            var userIdString = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
            {
                return Results.Unauthorized();
            }

            await authService.LogoutAsync(userId, request?.RefreshToken, ct);
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("Logout")
        .WithSummary("User logout")
        .WithDescription("Revokes active refresh token for the authorized user.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }
}
