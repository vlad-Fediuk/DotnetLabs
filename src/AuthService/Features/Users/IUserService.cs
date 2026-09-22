using AuthService.Features.Users.DTOs;
using ECommerce.Shared.Common;

namespace AuthService.Features.Users;

public interface IUserService
{
    Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
    Task<PagedResult<UserDto>> GetUsersPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task UpdateRolesAsync(Guid userId, IReadOnlyList<string> roleNames, CancellationToken ct = default);
}
