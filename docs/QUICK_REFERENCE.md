# Quick Reference: Scope Validation & Role Claims

## Three Scope Modules

### 1?? MemberScopesModule (No Roles)
```
read:members         ? Read member profiles
export:members       ? Export CSV
verify:membership    ? Check membership status

ScopesRequiringRoles = []  (doesn't need roles)
```

### 2?? OrganizationScopesModule (No Roles)
```
read:organization    ? Read org structure

ScopesRequiringRoles = []  (doesn't need roles)
```

### 3?? AnalyticsScopesModule (Requires Roles!)
```
read:statistics      ? Read statistics
read:exco            ? Read executive committee

ScopesRequiringRoles = [read:statistics, read:exco]  ?? Must have roles!
```

---

## Scope Validation in 5 Steps

```
1. Validate RequestedScopes exist
   ?? If any invalid ? FAIL

2. Load AppRegistration & check if active
   ?? If not found/inactive ? FAIL

3. Parse AllowedScopes JSON
   ?? If malformed ? FAIL

4. Calculate GRANTED = RequestedScopes ? AllowedScopes
   ?? If empty ? FAIL

5. Check if any GrantedScope requires roles
   ?? If yes ? MUST load member roles
```

---

## When Role Claims Are Added

```
if (ScopeDefinitions.RequiresRoles(grantedScopes))
{
    // Load member roles from database
    var roles = await LoadMemberRolesAsync(memberId);
    
    // Add EACH role as a claim: "Department:Level"
    foreach (var (department, levelType) in roles)
        claims.Add(new Claim("role", $"{department}:{levelType}"));
}
```

**Example:**
- Member has roles: Finance:Level1, Audit:Level2
- If grantedScopes = ["read:statistics"], then ADD role claims
- JWT will have: `claims.role = ["Finance:Level1", "Audit:Level2"]`

---

## Real Example

```csharp
// APP SETUP
AllowedScopes = ["read:statistics"]  // Only this

// REQUEST
RequestedScopes = ["read:statistics", "read:members", "export:members"]

// RESULT
GrantedScopes = ["read:statistics"]  ? Only intersection
RequiresRoles = true                 ? read:statistics needs roles
RolesClaims = ["Finance:Level1"]     ? Added automatically

// JWT WILL HAVE
? scope: read:statistics
? role: Finance:Level1
? scope: read:members (DENIED - not in allowed)
? scope: export:members (DENIED - not in allowed)
```

---

## Endpoint Permission Check

```csharp
[Authorize]
[HttpGet("api/statistics")]
public async Task<IActionResult> GetStatistics()
{
    var claims = HttpContext.User.Claims;
    var scopes = claims.Where(c => c.Type == "scope").Select(c => c.Value).ToList();
    var roles = claims.Where(c => c.Type == "role").Select(c => c.Value).ToList();
    
    // Check scope
    if (!scopes.Contains("read:statistics"))
        return Forbid($"Missing read:statistics scope");
    
    // Check role (if needed)
    if (roles.Count == 0)
        return Forbid("No role claims found");
    
    // Filter by role
    var userRoles = roles.Select(r => r.Split(':'));
    var data = await db.Stats
        .Where(s => userRoles.Any(r => r[0] == s.Department))
        .ToListAsync();
    
    return Ok(data);
}
```

---

## Error Scenarios

| Scenario | Error | Why |
|----------|-------|-----|
| Request `read:invalid` | ? Invalid scopes | Scope not in AllValidScopes |
| App doesn't exist | ? App not found | AppId mismatch |
| App inactive | ? App inactive | IsActive = false |
| Invalid JSON in AllowedScopes | ? Invalid JSON | App config corrupted |
| No overlap of scopes | ? No allowed scopes | RequestedScopes ? AllowedScopes = ? |
| Member not found | ? Member not found | MkanId invalid |
| read:statistics but no roles | ? Role error | Member has no roles |
| JWT key not configured | ? SigningKey missing | Config error |

---

## One-Liner Explanations

- **RequestedScopes**: What the client ASKS for
- **GrantedScopes**: What the client ACTUALLY gets (intersection)
- **AllowedScopes**: App's permission list (set by admin)
- **Role Claims**: Additional filtering layer for sensitive scopes
- **Result Pattern**: Failures are data, not exceptions
