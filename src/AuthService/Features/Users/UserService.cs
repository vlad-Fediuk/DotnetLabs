using Microsoft.Extensions.Logging;
using AuthService.Data;
using AuthService.Features.Users.DTOs;
using AuthService.Models;
using ECommerce.Shared.Common;
using ECommerce.Shared.Exceptions;

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

        return new UserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            user.CreatedAtUtc,
            user.IsActive
        );
    }

    public async Task<PagedResult<UserDto>> GetUsersPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var (users, totalCount) = await _userRepository.GetPagedWithRolesAsync(page, pageSize, ct);

        var dtos = users.Select(user => new UserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            user.CreatedAtUtc,
            user.IsActive
        )).ToList();

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
        var targetRoles = availableRoles.Where(r => roleNames.Contains(r.Name, StringComparer.OrdinalIgnoreCase)).ToList();

        if (targetRoles.Count != roleNames.Count)
        {
            throw new BadRequestException("One or more specified roles do not exist.");
        }

        user.UserRoles.Clear();
        foreach (var role in targetRoles)
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            });
        }

        await _userRepository.SaveChangesAsync(ct);
        _logger.LogInformation("Roles updated for user {UserId}: {Roles}", userId, string.Join(", ", roleNames));
    }
}
