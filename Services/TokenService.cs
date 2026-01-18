using AmsaAPI.Common;
using AmsaAPI.Data;
using AmsaAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace AmsaAPI.Services;

public class TokenService(IConfiguration configuration, AmsaDbContext db)
{
    private readonly IConfiguration _configuration = configuration;
    private readonly AmsaDbContext _db = db;
    
    public async Task<Result<string>> GenerateTokenAsync(TokenGenerationRequest request)
    {
        if (request == null)
            return Result.Validation<string>("Request cannot be null");

        var invalidScopes = ScopeDefinitions.GetInvalidScopes(request.RequestedScopes);
        if (invalidScopes.Length > 0)
            return Result.Validation<string>($"Invalid scopes: {string.Join(", ", invalidScopes)}");

        var appResult = await _db.AppRegistrations
            .FirstOrDefaultAsync(a => a.AppId == request.AppId);
        if (appResult == null)
            return Result.NotFound<string>($"App '{request.AppId}' not found");
        if (!appResult.IsActive)
            return Result.Validation<string>($"App '{request.AppId}' inactive");

        string[] allowedScopes;
        try
        {
            allowedScopes = JsonSerializer.Deserialize<string[]>(appResult.AllowedScopes) ?? [];
        }
        catch (JsonException)
        {
            return Result.BadRequest<string>($"Invalid AllowedScopes for app '{request.AppId}'");
        }

        var grantedScopes = request.RequestedScopes
            .Intersect(allowedScopes)
            .ToArray();
        if (grantedScopes.Length == 0)
            return Result.Validation<string>($"No allowed scopes for app '{request.AppId}'");

        var member = await _db.Members
            .FirstOrDefaultAsync(m => m.Mkanid == request.MkanId);
        if (member == null)
            return Result.NotFound<string>($"Member {request.MkanId} not found");

        var roles = await LoadMemberRolesAsync(member.MemberId);
        var claims = BuildClaimsForApp(request.AppId, member, roles, grantedScopes);
        
        var tokenResult = CreateJwtToken(request.AppId, claims, appResult.TokenExpirationHours ?? 1);
        if (tokenResult.IsSuccess)
        {
            appResult.LastUsedAt = DateTime.UtcNow;
            _db.AppRegistrations.Update(appResult);
            await _db.SaveChangesAsync();
        }

        return tokenResult;
    }

    private async Task<List<(string Department, string LevelType)>> LoadMemberRolesAsync(int memberId)
    {
        var query = await _db.MemberLevelDepartments
            .Where(mld => mld.MemberId == memberId)
            .Include(mld => mld.LevelDepartment.Department)
            .Include(mld => mld.LevelDepartment.Level)
            .Select(mld => new 
            {
               Department = mld.LevelDepartment.Department.DepartmentName,
               mld.LevelDepartment.Level.LevelType
            })
            .ToListAsync();

        return query.Select(x => (x.Department, x.LevelType)).ToList();
    }

    private List<Claim> BuildClaimsForApp(string appId, Member member, 
        List<(string Department, string LevelType)> roles, string[] grantedScopes)
    {
        var claims = new List<Claim>
        {
            new("sub", member.MemberId.ToString()),
            new("mkanId", member.Mkanid.ToString()),
            new("firstName", member.FirstName),
            new("lastName", member.LastName),
            new("email", member.Email ?? string.Empty),
            new("unitId", member.UnitId.ToString()),
            new("app", appId),
            new("isMember", "true")
        };

        foreach (var scope in grantedScopes)
            claims.Add(new Claim("scope", scope));

        if (ScopeDefinitions.RequiresRoles(grantedScopes) && roles.Count > 0)
            foreach (var (department, levelType) in roles)
                claims.Add(new Claim("role", $"{department}:{levelType}"));

        return claims;
    }

    private Result<string> CreateJwtToken(string appId, List<Claim> claims, int expirationHours)
    {
        var jwtConfig = _configuration.GetSection("Jwt");
        var signingKey = jwtConfig["SigningKey"];
        var issuer = jwtConfig["Issuer"];
        var audience = jwtConfig["Audience"];

        if (string.IsNullOrWhiteSpace(signingKey))
            return Result.BadRequest<string>("SigningKey not configured");
        if (string.IsNullOrWhiteSpace(issuer))
            return Result.BadRequest<string>("Issuer not configured");
        if (string.IsNullOrWhiteSpace(audience))
            return Result.BadRequest<string>("Audience not configured");

        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(expirationHours),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = credentials
            };

            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateToken(descriptor);
            return Result.Success(handler.WriteToken(token));
        }
        catch (Exception ex)
        {
            return Result.BadRequest<string>($"Token creation failed: {ex.Message}");
        }
    }
}
