using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AuthService.Features.Users.DTOs;
using ECommerce.Shared.Common;
using ECommerce.Shared.Filters;

namespace AuthService.Features.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users")
            .WithTags("Users");

        group.MapGet("/me", async (ClaimsPrincipal principal, IUserService userService, CancellationToken ct) =>
        {
            var userIdString = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
            {
                return Results.Unauthorized();
            }

            var profile = await userService.GetCurrentUserAsync(userId, ct);
            return Results.Ok(profile);
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .WithSummary("Get current user profile")
        .WithDescription("Retrieves the authenticated user's profile and roles using identity claims.")
        .Produces<UserDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/", async (int? page, int? pageSize, IUserService userService, CancellationToken ct) =>
        {
            var result = await userService.GetUsersPagedAsync(page ?? 1, pageSize ?? 10, ct);
            return Results.Ok(result);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("GetUsersPaged")
        .WithSummary("Get paginated user list")
        .WithDescription("Retrieves a paginated list of registered users. Requires Admin role.")
        .Produces<PagedResult<UserDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPut("/{id:guid}/roles", async (Guid id, UpdateRolesRequest request, IUserService userService, CancellationToken ct) =>
        {
            await userService.UpdateRolesAsync(id, request.Roles, ct);
            return Results.NoContent();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithName("UpdateUserRoles")
        .WithSummary("Update user roles")
        .WithDescription("Updates the role set for the specified user. Requires Admin role.")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .AddEndpointFilter<ValidationFilter<UpdateRolesRequest>>();

        return app;
    }
}
