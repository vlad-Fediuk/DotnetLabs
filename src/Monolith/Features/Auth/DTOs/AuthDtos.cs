using LR0.Domain.Enums;

namespace LR0.Features.Auth.DTOs;

public record RegisterRequest(string Email, string Password, string FullName);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string Token, DateTime ExpiresAtUtc, UserResponse User);

public record UserResponse(Guid Id, string Email, string FullName, UserRole Role);
