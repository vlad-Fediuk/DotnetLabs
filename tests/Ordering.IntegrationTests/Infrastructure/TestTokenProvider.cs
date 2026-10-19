using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Ordering.IntegrationTests.Infrastructure;

public class TestTokenProvider
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;

    public TestTokenProvider(IConfiguration configuration)
    {
        var jwt = configuration.GetSection("Jwt");
        _key = jwt["SecretKey"] ?? jwt["Key"] ?? throw new InvalidOperationException("Jwt key is not configured.");
        _issuer = jwt["Issuer"] ?? "ECommerceApi";
        _audience = jwt["Audience"] ?? "ECommerceApi";
    }

    public string CreateToken(Guid userId, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, $"{userId:N}@tests.local")
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _issuer,
            Audience = _audience,
            Expires = DateTime.UtcNow.AddMinutes(10),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)),
                SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
