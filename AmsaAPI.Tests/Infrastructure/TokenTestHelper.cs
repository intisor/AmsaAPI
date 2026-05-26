using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AmsaAPI.Tests.Infrastructure;

/// <summary>
/// Helper for generating test JWT tokens
/// </summary>
public static class TokenTestHelper
{
    private const string TestSecretKey = "kDV7RZElNYBOvL0hQqmxcj9SwtIbsT3z";
    private const string TestIssuer = "AMSAAPI";

    public static string GenerateTestToken(
        string appId = "ReportingApp",
        TimeSpan? expiresIn = null,
        Dictionary<string, string>? additionalClaims = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, appId),
            new Claim("app_id", appId),
            new Claim(ClaimTypes.Role, "app")
        };

        if (additionalClaims != null)
        {
            foreach (var claim in additionalClaims)
            {
                claims.Add(new Claim(claim.Key, claim.Value));
            }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(expiresIn ?? TimeSpan.FromHours(1)),
            Issuer = TestIssuer,
            Audience = appId,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public static string GenerateExpiredToken(string appId = "ReportingApp")
    {
        return GenerateTestToken(appId, TimeSpan.FromHours(-1));
    }
}
