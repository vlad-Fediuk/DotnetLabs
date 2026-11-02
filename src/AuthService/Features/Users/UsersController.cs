using AuthService.Features.Users.DTOs;
using ECommerce.Shared.Common;
using ECommerce.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Features.Users;

[ApiController]
[Route("api/v1/users")]
[Tags("Users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        var userId = User.GetUserId();
        var profile = await _userService.GetCurrentUserAsync(userId, ct);
        return Ok(profile);
    }

    [HttpGet]
    [HasPermission(Permissions.Users.View)]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsersPaged([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct)
    {
        var result = await _userService.GetUsersPagedAsync(page ?? 1, pageSize ?? 10, ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}/roles")]
    [HasPermission(Permissions.Users.ManageRoles)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRoles(Guid id, [FromBody] UpdateRolesRequest request, CancellationToken ct)
    {
        await _userService.UpdateRolesAsync(id, request.Roles, ct);
        return NoContent();
    }
}
