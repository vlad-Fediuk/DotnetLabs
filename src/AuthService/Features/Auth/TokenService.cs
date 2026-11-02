using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthService.Models;
using ECommerce.Shared.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Features.Auth;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresAt) GenerateAccessToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["Key"]
                        ?? jwtSettings["SecretKey"]
                        ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = jwtSettings["Issuer"] ?? "ECommerceApi";
        var audience = jwtSettings["Audience"] ?? "ECommerceApi";
        var accessTokenMinutes = int.TryParse(jwtSettings["AccessTokenMinutes"], out var minutes) ? minutes : 15;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var roleNames = user.UserRoles
            .Where(ur => ur.Role != null && !string.IsNullOrWhiteSpace(ur.Role.Name))
            .Select(ur => ur.Role.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (roleNames.Count == 0)
        {
            roleNames.Add(RoleNames.User);
        }

        foreach (var roleName in roleNames)
        {
            claims.Add(new Claim(ClaimTypes.Role, roleName));
        }

        var permissions = RolePermissions.GetPermissionsForRoles(roleNames);
        foreach (var permission in permissions)
        {
            claims.Add(new Claim(Permissions.ClaimType, permission));
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(accessTokenMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public DateTime GetRefreshTokenExpiry()
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var days = int.TryParse(jwtSettings["RefreshTokenDays"], out var d) ? d : 7;
        return DateTime.UtcNow.AddDays(days);
    }
}
