using AuthService.Data;
using AuthService.Features.Users.DTOs;
using AuthService.Models;
using ECommerce.Shared.Common;
using ECommerce.Shared.Exceptions;
using ECommerce.Shared.Security;
using Microsoft.Extensions.Logging;

namespace AuthService.Features.Users;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IBaseRepository<Role> _roleRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IBaseRepository<Role> roleRepository,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(userId, ct);
        if (user == null)
        {
            throw new NotFoundException("User profile not found.");
        }

        return MapToDto(user);
    }

    public async Task<PagedResult<UserDto>> GetUsersPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var (users, totalCount) = await _userRepository.GetPagedWithRolesAsync(page, pageSize, ct);

        var dtos = users.Select(MapToDto).ToList();

        return new PagedResult<UserDto>(dtos, totalCount, page, pageSize);
    }

    public async Task UpdateRolesAsync(Guid userId, IReadOnlyList<string> roleNames, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdWithRolesAsync(userId, ct);
        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        var availableRoles = await _roleRepository.GetAllAsync(ct);
        var distinctRequested = roleNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var targetRoles = availableRoles
            .Where(r => distinctRequested.Contains(r.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (targetRoles.Count != distinctRequested.Count)
        {
            throw new BadRequestException("One or more specified roles do not exist.");
        }

        user.UserRoles.Clear();
        foreach (var role in targetRoles)
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                Role = role
            });
        }

        await _userRepository.SaveChangesAsync(ct);
        _logger.LogInformation("Roles updated for user {UserId}: {Roles}", userId, string.Join(", ", distinctRequested));
    }

    private static UserDto MapToDto(User user)
    {
        var roles = user.UserRoles
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var permissions = RolePermissions.GetPermissionsForRoles(roles);

        return new UserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            roles,
            permissions,
            user.CreatedAtUtc,
            user.IsActive
        );
    }
}
