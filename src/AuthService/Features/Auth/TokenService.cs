using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using AuthService.Models;

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
        var secretKey = jwtSettings["Key"] ?? jwtSettings["SecretKey"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
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

        foreach (var userRole in user.UserRoles)
        {
            var roleName = userRole.Role?.Name ?? (userRole.RoleId == 2 ? "Admin" : "User");
            claims.Add(new Claim(ClaimTypes.Role, roleName));
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
