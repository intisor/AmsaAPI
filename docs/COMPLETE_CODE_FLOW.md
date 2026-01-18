# Complete Code Flow Example

## Scenario: FinTech App Requests Token

### 1. HTTP Request Comes In

```csharp
POST /api/auth/generate-token
Content-Type: application/json

{
  "appId": "fintech-dashboard",
  "appSecret": "secret123",
  "mkanId": 12345,
  "requestedScopes": ["read:statistics", "read:members", "invalid:scope"]
}
```

---

### 2. Controller Receives Request

```csharp
[HttpPost("generate-token")]
public async Task<IActionResult> GenerateToken(TokenGenerationRequest request)
{
    var result = await _tokenService.GenerateTokenAsync(request);
    
    return result.Match(
        onSuccess: token => Ok(new TokenResponse 
        { 
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            TokenType = "Bearer",
            Scopes = ["read:statistics"]  // Only GRANTED scopes
        }),
        onFailure: (errorType, message) => BadRequest(new { error = message })
    );
}
```

---

### 3. TokenService.GenerateTokenAsync Executes

```csharp
public async Task<Result<string>> GenerateTokenAsync(TokenGenerationRequest request)
{
    // STEP 1: Validate requested scopes
    var invalidScopes = ScopeDefinitions.GetInvalidScopes(request.RequestedScopes);
    if (invalidScopes.Length > 0)
        return Result.Validation<string>($"Invalid scopes: {string.Join(", ", invalidScopes)}");
    // ? Fails here! ["invalid:scope"]
}
```

**Output at this point:**
```
Result.IsSuccess = false
Result.ErrorType = Validation
Result.ErrorMessage = "Invalid scopes: invalid:scope"
```

**Response to client:**
```json
{
  "error": "Invalid scopes: invalid:scope"
}
```

---

### 4. If Invalid Scopes Are Fixed, Retry

```csharp
// NEW REQUEST (fixed)
{
  "requestedScopes": ["read:statistics", "read:members"]  // removed invalid:scope
}
```

Now TokenService continues:

```csharp
// STEP 2: Load app registration
var appResult = await _db.AppRegistrations
    .FirstOrDefaultAsync(a => a.AppId == request.AppId);
if (appResult == null)
    return Result.NotFound<string>($"App 'fintech-dashboard' not found");

// ? Found! App exists with:
// AppId: "fintech-dashboard"
// IsActive: true
// AllowedScopes: '["read:statistics", "read:organization"]'
// TokenExpirationHours: 2

// STEP 3: Parse AllowedScopes
string[] allowedScopes;
try
{
    allowedScopes = JsonSerializer.Deserialize<string[]>(appResult.AllowedScopes) ?? [];
    // ? ["read:statistics", "read:organization"]
}
catch (JsonException)
{
    return Result.BadRequest<string>($"Invalid AllowedScopes...");
}

// STEP 4: Calculate GRANTED scopes
var grantedScopes = request.RequestedScopes
    .Intersect(allowedScopes)
    .ToArray();
// ? ["read:statistics", "read:members"].Intersect(["read:statistics", "read:organization"])
// ? Result: ["read:statistics"]

if (grantedScopes.Length == 0)
    return Result.Validation<string>("No allowed scopes for app...");

// STEP 5: Load member
var member = await _db.Members
    .FirstOrDefaultAsync(m => m.Mkanid == request.MkanId);
if (member == null)
    return Result.NotFound<string>($"Member 12345 not found");

// ? Found member:
// MemberId: 100
// Mkanid: 12345
// FirstName: "Ahmed"
// LastName: "Hassan"
// Email: "ahmed@example.com"
// UnitId: 5

// STEP 6: Load member roles
var roles = await LoadMemberRolesAsync(member.MemberId);
// ? [(Finance, Level1), (Audit, Level2)]

// STEP 7: Build claims
var claims = BuildClaimsForApp(request.AppId, member, roles, grantedScopes);
// Details below...

// STEP 8: Create JWT
var tokenResult = CreateJwtToken(request.AppId, claims, appResult.TokenExpirationHours ?? 1);
// ? Returns eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

### 5. BuildClaimsForApp Details

```csharp
private List<Claim> BuildClaimsForApp(
    string appId, 
    Member member, 
    List<(string Department, string LevelType)> roles, 
    string[] grantedScopes)  // ? ["read:statistics"]
{
    var claims = new List<Claim>
    {
        new("sub", "100"),                    // subject (member ID)
        new("mkanId", "12345"),              // AMSA ID
        new("firstName", "Ahmed"),           // name
        new("lastName", "Hassan"),           // name
        new("email", "ahmed@example.com"),   // email
        new("unitId", "5"),                  // unit
        new("app", "fintech-dashboard"),     // which app
        new("isMember", "true")              // membership flag
    };

    // Add GRANTED scopes (not requested!)
    foreach (var scope in grantedScopes)  // ["read:statistics"]
    {
        claims.Add(new Claim("scope", "read:statistics"));
    }

    // Check if any granted scope requires roles
    if (ScopeDefinitions.RequiresRoles(grantedScopes))  // true
    {
        // read:statistics IS in ScopesRequiringRoles
        
        if (roles.Count > 0)  // [(Finance, Level1), (Audit, Level2)]
        {
            foreach (var (department, levelType) in roles)
            {
                claims.Add(new Claim("role", "Finance:Level1"));
                claims.Add(new Claim("role", "Audit:Level2"));
            }
        }
    }

    return claims;
    // ? Final claims list:
    // [sub=100, mkanId=12345, firstName=Ahmed, lastName=Hassan, 
    //  email=ahmed@example.com, unitId=5, app=fintech-dashboard, 
    //  isMember=true, scope=read:statistics, role=Finance:Level1, role=Audit:Level2]
}
```

---

### 6. CreateJwtToken Details

```csharp
private Result<string> CreateJwtToken(
    string appId, 
    List<Claim> claims,           // ? Full claims list from step 5
    int expirationHours)          // ? 2
{
    // Read config
    var jwtConfig = _configuration.GetSection("Jwt");
    var signingKey = jwtConfig["SigningKey"];        // "my-super-secret-key-min-32"
    var issuer = jwtConfig["Issuer"];               // "https://amsa-api.local"
    var audience = jwtConfig["Audience"];           // "amsa-clients"

    // Validate
    if (string.IsNullOrWhiteSpace(signingKey))
        return Result.BadRequest<string>("SigningKey not configured");
    if (string.IsNullOrWhiteSpace(issuer))
        return Result.BadRequest<string>("Issuer not configured");
    if (string.IsNullOrWhiteSpace(audience))
        return Result.BadRequest<string>("Audience not configured");

    try
    {
        // Create credentials
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Build descriptor
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(2),  // 2 hours from now
            Issuer = "https://amsa-api.local",
            Audience = "amsa-clients",
            SigningCredentials = credentials
        };

        // Create token
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);
        
        // Return signed JWT string
        return Result.Success(handler.WriteToken(token));
        // ? "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMDAiLCJta2FuSWQiOiIxMjM0NSIsImZpcnN0TmFtZSI6IkFobWVkIiwibGFzdE5hbWUiOiJIYXNzYW4iLCJlbWFpbCI6ImFobWVkQGV4YW1wbGUuY29tIiwidW5pdElkIjoiNSIsImFwcCI6ImZpbnRlY2gtZGFzaGJvYXJkIiwiaXNNZW1iZXIiOiJ0cnVlIiwic2NvcGUiOiJyZWFkOnN0YXRpc3RpY3MiLCJyb2xlIjoiRmluYW5jZTpMZXZlbDEiLCJyb2xlIjoiQXVkaXQ6TGV2ZWwyIn0.signature"
    }
    catch (Exception ex)
    {
        return Result.BadRequest<string>($"Token creation failed: {ex.Message}");
    }
}
```

---

### 7. Final Response

```csharp
// Controller returns success
Result<string>.Success("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...")

// HTTP Response
HTTP/1.1 200 OK
Content-Type: application/json

{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2024-01-15T16:30:00Z",
  "tokenType": "Bearer",
  "member": {
    "memberId": 100,
    "mkanId": 12345,
    "firstName": "Ahmed",
    "lastName": "Hassan",
    "email": "ahmed@example.com"
  },
  "roles": ["Finance:Level1", "Audit:Level2"],
  "scopes": ["read:statistics"]
}
```

---

### 8. Client Uses Token

```csharp
// FinTech app decodes token
var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";
var handler = new JwtSecurityTokenHandler();
var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

// Extract claims
foreach (var claim in jwtToken.Claims)
{
    Console.WriteLine($"{claim.Type}: {claim.Value}");
}

// Output:
// sub: 100
// mkanId: 12345
// firstName: Ahmed
// lastName: Hassan
// email: ahmed@example.com
// unitId: 5
// app: fintech-dashboard
// isMember: true
// scope: read:statistics
// role: Finance:Level1
// role: Audit:Level2
```

---

### 9. FinTech App Calls Protected Endpoint

```csharp
// Send token in header
GET /api/statistics
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

### 10. Server-Side Endpoint Handler

```csharp
[Authorize]
[HttpGet("api/statistics")]
public async Task<IActionResult> GetStatistics()
{
    var user = HttpContext.User;
    
    // Extract claims from token
    var scopes = user.FindAll("scope").Select(c => c.Value).ToArray();  // [read:statistics]
    var roles = user.FindAll("role").Select(c => c.Value).ToArray();    // [Finance:Level1, Audit:Level2]
    var app = user.FindFirst("app")?.Value;                              // fintech-dashboard
    var mkanId = user.FindFirst("mkanId")?.Value;                        // 12345

    // Verify scope
    if (!scopes.Contains("read:statistics"))
        return Forbid("Missing read:statistics scope");

    // Filter data by role
    var roleFilters = roles.Select(r => r.Split(':')).ToArray();
    var statistics = await _db.Statistics
        .Where(s => roleFilters.Any(rf => rf[0] == s.Department && rf[1] == s.Level))
        .ToListAsync();

    // Log access
    _logger.LogInformation(
        "App {AppId} accessed statistics for member {MkanId} with roles {Roles}", 
        app, mkanId, string.Join(", ", roles));

    return Ok(statistics);
}

// ? Returns filtered data based on Finance:Level1 and Audit:Level2 roles
```

---

## Summary Table

| Step | Component | Input | Process | Output |
|------|-----------|-------|---------|--------|
| 1 | Controller | HTTP Request | Deserialize JSON | TokenGenerationRequest |
| 2 | TokenService | Request | Validate scopes | Error or continue |
| 3 | TokenService | AppId | Load from DB | AppRegistration |
| 4 | TokenService | Requested+Allowed | Intersect | GrantedScopes |
| 5 | TokenService | MkanId | Load from DB | Member |
| 6 | TokenService | GrantedScopes | Check requirements | NeedsRoles? |
| 7 | TokenService | MemberId | Load from DB | Roles list |
| 8 | TokenService | Member+Roles | BuildClaimsForApp | Claim list |
| 9 | TokenService | Claims | CreateJwtToken | JWT string |
| 10 | Controller | Result<string> | Match pattern | HTTP response |
