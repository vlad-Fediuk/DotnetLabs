using Microsoft.Extensions.Logging;
using AuthService.Data;
using AuthService.Features.Auth.DTOs;
using AuthService.Features.Users;
using AuthService.Features.Users.DTOs;
using AuthService.Models;
using ECommerce.Shared.Exceptions;

namespace AuthService.Features.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IBaseRepository<Role> _roleRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IBaseRepository<Role> roleRepository,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _roleRepository = roleRepository;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.ExistsByEmailAsync(normalizedEmail, ct))
        {
            _logger.LogWarning("Registration failed: email {Email} already exists", request.Email);
            throw new ConflictException($"User with email '{request.Email}' already exists.");
        }

        var defaultRole = (await _roleRepository.FindAsync(r => r.Name == "User", ct)).FirstOrDefault();
        if (defaultRole == null)
        {
            defaultRole = new Role { Id = 1, Name = "User" };
            await _roleRepository.AddAsync(defaultRole, ct);
            await _roleRepository.SaveChangesAsync(ct);
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = passwordHash,
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = defaultRole.Id
        });

        await _userRepository.AddAsync(user, ct);
        await _userRepository.SaveChangesAsync(ct);

        _logger.LogInformation("User registered successfully with ID: {UserId}", user.Id);

        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshTokenExpiry = _tokenService.GetRefreshTokenExpiry();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAtUtc = refreshTokenExpiry,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(refreshToken, ct);
        await _refreshTokenRepository.SaveChangesAsync(ct);

        var userDto = new UserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            new[] { defaultRole.Name },
            user.CreatedAtUtc,
            user.IsActive
        );

        return new AuthResponse(accessToken, refreshTokenValue, expiresAt, userDto);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _userRepository.GetByEmailAsync(normalizedEmail, ct);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for email {Email}", request.Email);
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for deactivated user {UserId}", user.Id);
            throw new UnauthorizedException("User account is deactivated.");
        }

        _logger.LogInformation("User logged in successfully: {UserId}", user.Id);

        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshTokenExpiry = _tokenService.GetRefreshTokenExpiry();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenValue,
            ExpiresAtUtc = refreshTokenExpiry,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(refreshToken, ct);
        await _refreshTokenRepository.SaveChangesAsync(ct);

        var userDto = new UserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            user.CreatedAtUtc,
            user.IsActive
        );

        return new AuthResponse(accessToken, refreshTokenValue, expiresAt, userDto);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var existingToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, ct);

        if (existingToken == null || !existingToken.IsActive)
        {
            _logger.LogWarning("Invalid or expired refresh token attempt");
            throw new UnauthorizedException("Invalid, expired, or revoked refresh token.");
        }

        var user = existingToken.User;
        if (!user.IsActive)
        {
            throw new UnauthorizedException("User account is deactivated.");
        }

        var newRefreshTokenValue = _tokenService.GenerateRefreshToken();
        var newRefreshTokenExpiry = _tokenService.GetRefreshTokenExpiry();

        existingToken.RevokedAtUtc = DateTime.UtcNow;
        existingToken.ReplacedByToken = newRefreshTokenValue;
        _refreshTokenRepository.Update(existingToken);

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = newRefreshTokenValue,
            ExpiresAtUtc = newRefreshTokenExpiry,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(newRefreshToken, ct);
        await _refreshTokenRepository.SaveChangesAsync(ct);

        var (accessToken, expiresAt) = _tokenService.GenerateAccessToken(user);

        var userDto = new UserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            user.CreatedAtUtc,
            user.IsActive
        );

        _logger.LogInformation("Refreshed token rotated successfully for user {UserId}", user.Id);

        return new AuthResponse(accessToken, newRefreshTokenValue, expiresAt, userDto);
    }

    public async Task LogoutAsync(Guid userId, string? refreshToken, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken, ct);
            if (token != null && token.UserId == userId)
            {
                token.RevokedAtUtc = DateTime.UtcNow;
                _refreshTokenRepository.Update(token);
                await _refreshTokenRepository.SaveChangesAsync(ct);
            }
        }
        else
        {
            await _refreshTokenRepository.RevokeUserTokensAsync(userId, ct);
        }

        _logger.LogInformation("User logged out: {UserId}", userId);
    }
}
