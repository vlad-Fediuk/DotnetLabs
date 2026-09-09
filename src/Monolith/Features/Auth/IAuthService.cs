using LR0.Features.Auth.DTOs;

namespace LR0.Features.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
