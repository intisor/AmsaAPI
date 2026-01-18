# AMSA API - Architecture & Design

## System Overview

AMSA API is a **complete, production-ready authentication layer** for external applications. It provides scope-based authorization, role-based filtering, and JWT token generation.

---

## ??? Architecture (7 Layers)

```
Layer 7: External Apps (FinTech, Audit, Analytics)
   ?
Layer 6: HTTP Controllers & Endpoints
   ?
Layer 5: Result<T> Pattern (Functional Error Handling)
   ?
Layer 4: Services (TokenService, AppRegistrationService)
   ?
Layer 3: Validators & Registry (Validator, ScopeDefinitions)
   ?
Layer 2: Scope Modules (Member, Organization, Analytics)
   ?
Layer 1: Database (AppRegistrations, Members, Roles)
```

---

## ?? Core Concepts

### RequestedScopes vs GrantedScopes

**Critical Concept:** What client asks for ? what they get!

```
Client requests: [read:statistics, read:members, export:members]
App allowed:     [read:statistics, read:organization]
                          ?
Granted:         [read:statistics]  ? Only this in token!
```

**Why?** Least privilege principle. Apps can only use what they're explicitly granted.

### Three Scope Modules

| Module | Scopes | Requires Roles |
|--------|--------|----------------|
| **MemberScopes** | read:members, export:members, verify:membership | ? No |
| **OrganizationScopes** | read:organization | ? No |
| **AnalyticsScopes** | read:statistics, read:exco | ? Yes |

### Role Claims Format

```
role: "Finance:Level1"    (Department:Level)
role: "Audit:Level2"
role: "HR:Manager"
```

Role claims are **only added when a granted scope requires them** (read:statistics, read:exco).

---

## ?? Request Flow (8 Steps)

```
1. Client requests token
   ?
2. Validate requested scopes exist
   ?
3. Load app registration
   ?
4. Calculate granted scopes (intersection)
   ?
5. Load member & roles
   ?
6. Build JWT claims (with granted scopes + roles if needed)
   ?
7. Sign JWT token
   ?
8. Return token to client
```

---

## ?? Security Model

### Layer 1: Scope Validation
- Only known scopes allowed
- Invalid scopes rejected immediately

### Layer 2: App Authorization
- App only gets intersection of requested + allowed
- Can't escalate privileges

### Layer 3: Role-Based Filtering
- Only added for sensitive scopes
- Provides organizational hierarchy

### Layer 4: Endpoint Authorization
- [Authorize] attribute required
- Scope + role validation in endpoint

---

## ?? Scope Validation Process

```
Step 1: Validate RequestedScopes
?? Check against AllValidScopes
?? Fail if any invalid

Step 2: Load AppRegistration
?? Check app exists
?? Check app is active
?? Parse AllowedScopes JSON

Step 3: Calculate GRANTED = Requested ? Allowed
?? Keep intersection only
?? Fail if empty

Step 4: Check Role Requirements
?? RequiresRoles(grantedScopes)?
?? If yes ? load member roles

Step 5: Build Claims
?? Standard claims (sub, email, etc)
?? Granted scope claims
?? Role claims (if needed)

Step 6: Sign & Return JWT
```

---

## ?? File Structure

```
Services/
??? TokenService.cs           ? Token generation (8-step flow)
??? AppRegistrationService.cs ? CRUD + validation
??? AppRegistrationValidator.cs ? Validation rules
??? ScopeDefinitions.cs       ? Scope registry
??? Scopes/
    ??? MemberScopesModule.cs
    ??? OrganizationScopesModule.cs
    ??? AnalyticsScopesModule.cs

Endpoints/
??? (Auth endpoints in Program.cs)

DTOs/
??? AuthDTOs.cs               ? Request/response types

Common/
??? Result.cs                 ? Result<T> pattern
```

---

## ?? API Endpoints

### Token Generation
```
POST /api/auth/token

Request:
{
  "appId": "fintech-dashboard",
  "appSecret": "secret123",
  "mkanId": 12345,
  "requestedScopes": ["read:statistics", "read:members"]
}

Response:
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "tokenType": "Bearer"
}
```

### App Management (Admin Only)
```
POST   /api/auth/apps              ? Create
GET    /api/auth/apps/{appId}      ? Read
PUT    /api/auth/apps/{appId}      ? Update
DELETE /api/auth/apps/{appId}      ? Delete
```

---

## ??? Error Handling

### Result<T> Pattern

Instead of exceptions, all operations return:

```csharp
Result<T> {
  IsSuccess: bool,
  Value: T,
  ErrorType: ErrorType (Validation, NotFound, Conflict, etc),
  ErrorMessage: string
}
```

### Common Errors

| Error | Cause |
|-------|-------|
| Invalid scopes | Scope not in AllValidScopes |
| App not found | AppId doesn't exist |
| App inactive | IsActive = false |
| No allowed scopes | Intersection is empty |
| Member not found | MkanId invalid |
| Invalid JSON | Corrupted AllowedScopes |

---

## ?? Example: FinTech Dashboard

### Setup (Admin)
```csharp
var app = new AppRegistration
{
  AppId = "fintech-dashboard",
  AppName = "FinTech Dashboard",
  AllowedScopes = '["read:statistics", "read:organization"]',
  TokenExpirationHours = 2,
  IsActive = true
};
await appService.CreateAsync(app);
```

### Runtime (FinTech App)
```csharp
var request = new TokenGenerationRequest
{
  AppId = "fintech-dashboard",
  AppSecret = "secret123",
  MkanId = 12345,
  RequestedScopes = new[] { 
    "read:statistics", 
    "read:members",       // Will be DENIED
    "export:members"      // Will be DENIED
  }
};
var token = await tokenService.GenerateTokenAsync(request);
```

### Result
- ? Granted: read:statistics (with role claims: Finance:Level1)
- ? Denied: read:members, export:members

### Usage
```csharp
[Authorize]
[HttpGet("api/statistics")]
public IActionResult GetStatistics()
{
  var scopes = User.FindAll("scope");
  var roles = User.FindAll("role");
  
  if (!scopes.Any(c => c.Value == "read:statistics"))
    return Forbid();
  
  // Filter statistics by user's roles
  // (Finance:Level1, etc)
}
```

---

## ?? Design Principles

| Principle | Implementation |
|-----------|-----------------|
| **Requested ? Granted** | Intersection-based filtering |
| **Least Privilege** | Apps get minimum necessary access |
| **Early Validation** | Fail at registration time, not token time |
| **Role-Based Filtering** | Only when scope requires it |
| **Immutable Scopes** | Defined in code, can't change at runtime |
| **No Exceptions** | Result pattern for all errors |
| **Stateless Auth** | JWT-based, no session needed |

---

## ?? How It Scales

### Single App
```
1 External App ? 1 AppRegistration ? 1 JWT ? Access controlled
```

### Multiple Apps
```
App A ? AllowedScopes: [read:members]
App B ? AllowedScopes: [read:statistics]
App C ? AllowedScopes: [read:members, read:statistics]

Each app sees only what they're allowed.
```

### Role Hierarchy
```
Member has multiple roles:
  - Finance:Level1
  - Audit:Level2
  
read:statistics scope ? includes ALL roles in token
? Endpoint filters by role
```

---

## ?? Deployment Checklist

- [ ] JWT secret key configured (min 32 chars)
- [ ] JWT issuer configured
- [ ] JWT audience configured
- [ ] Database schema migrated
- [ ] First admin user created
- [ ] First external app registered
- [ ] Token generation tested
- [ ] Protected endpoints tested

---

## ?? How to Extend

### Add New Scope

1. Create new ScopeModule in `Services/Scopes/`
2. Define constants and Scopes array
3. Set ScopesRequiringRoles if needed
4. ScopeDefinitions will automatically pick it up

### Add New Endpoint

1. Use minimal APIs in Program.cs
2. Inject TokenService or AppRegistrationService
3. Call Result-returning methods
4. Match Result to HTTP response

### Add Validation Rule

1. Extend AppRegistrationValidator
2. Add new validation method
3. Return Result<T> with error
4. Call from CreateAsync/UpdateAsync

---

## ?? See Also

- **QUICK_REFERENCE.md** - Cheat sheet
- **COMPLETE_CODE_FLOW.md** - Step-by-step implementation
- **AUTH_LAYER_COMPLETE.md** - Setup & testing guide

---

**Architecture Status:** ? Production-Ready  
**Build Status:** ? Successful  
**Documentation:** ? Complete
