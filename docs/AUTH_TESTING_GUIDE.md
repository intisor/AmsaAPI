# Auth Layer Testing Guide

## Prerequisites
Before testing, ensure:

1. **Database is set up** with:
   - `AppRegistrations` table
   - At least one registered app
   - `Members` table with test data
   - `MemberLevelDepartments` for role-based scopes

2. **Application is running** on `https://localhost:7278`

3. **JWT Configuration** in `appsettings.json` or user-secrets:
   ```json
   {
     "Jwt": {
       "SecretKey": "your-32-character-secret-key-here",
       "Issuer": "AmsaAPI",
       "Audience": "ReportingApp"
     }
   }
   ```

---

## Test 1: Token Generation (Anonymous)

### Endpoint
```
POST https://localhost:7278/api/auth/token
```

### Request Body
```json
{
  "appId": "ReportingApp",
  "appSecret": "your-app-secret",
  "mkanId": 12345,
  "requestedScopes": ["read:members", "read:statistics"]
}
```

### Using PowerShell
```powershell
$body = @{
    appId = "ReportingApp"
    appSecret = "your-app-secret"
    mkanId = 12345
    requestedScopes = @("read:members", "read:statistics")
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:7278/api/auth/token" `
    -Method POST `
    -ContentType "application/json" `
    -Body $body `
    -SkipCertificateCheck
```

### Using curl
```bash
curl -X POST https://localhost:7278/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{
    "appId": "ReportingApp",
    "appSecret": "your-app-secret",
    "mkanId": 12345,
    "requestedScopes": ["read:members", "read:statistics"]
  }' \
  --insecure
```

### Expected Success Response (200)
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer"
}
```

### Expected Error Responses

**404 - App Not Found**
```json
{
  "error": "App 'ReportingApp' not found"
}
```

**400 - Invalid Scopes**
```json
{
  "error": "Invalid scopes: invalid:scope, fake:permission"
}
```

**404 - Member Not Found**
```json
{
  "error": "Member 12345 not found"
}
```

**400 - App Inactive**
```json
{
  "error": "App 'ReportingApp' inactive"
}
```

---

## Test 2: Create App Registration (Admin Only)

### Endpoint
```
POST https://localhost:7278/api/auth/apps
Authorization: Bearer {admin-jwt-token}
```

### Request Body
```json
{
  "appId": "NewTestApp",
  "appName": "New Test Application",
  "appSecretHash": "$2a$11$hashed-bcrypt-secret",
  "allowedScopes": "[\"read:members\", \"read:organization\"]",
  "tokenExpirationHours": 2
}
```

### Using PowerShell
```powershell
$body = @{
    appId = "NewTestApp"
    appName = "New Test Application"
    appSecretHash = '$2a$11$hashed-bcrypt-secret'
    allowedScopes = '["read:members", "read:organization"]'
    tokenExpirationHours = 2
} | ConvertTo-Json

$headers = @{
    "Authorization" = "Bearer your-admin-jwt-token"
}

Invoke-RestMethod -Uri "https://localhost:7278/api/auth/apps" `
    -Method POST `
    -Headers $headers `
    -ContentType "application/json" `
    -Body $body `
    -SkipCertificateCheck
```

### Expected Success Response (201)
```json
{
  "appId": "NewTestApp",
  "appName": "New Test Application"
}
```

### Expected Error Responses

**401 - Unauthorized**
```json
{
  "error": "Unauthorized"
}
```

**403 - Forbidden (Not Admin)**
```json
{
  "error": "Insufficient permissions"
}
```

**409 - Conflict (App Exists)**
```json
{
  "error": "App 'NewTestApp' already exists"
}
```

---

## Test 3: Get App Details (Admin Only)

### Endpoint
```
GET https://localhost:7278/api/auth/apps/{appId}
Authorization: Bearer {admin-jwt-token}
```

### Using PowerShell
```powershell
$headers = @{
    "Authorization" = "Bearer your-admin-jwt-token"
}

Invoke-RestMethod -Uri "https://localhost:7278/api/auth/apps/ReportingApp" `
    -Method GET `
    -Headers $headers `
    -SkipCertificateCheck
```

### Expected Success Response (200)
```json
{
  "appId": "ReportingApp",
  "appName": "Reporting Dashboard",
  "isActive": true,
  "createdAt": "2024-01-15T10:30:00Z",
  "lastUsedAt": "2024-01-20T14:22:00Z"
}
```

---

## Test 4: Update App Configuration (Admin Only)

### Endpoint
```
PUT https://localhost:7278/api/auth/apps/{appId}
Authorization: Bearer {admin-jwt-token}
```

### Request Body
```json
{
  "appId": "ReportingApp",
  "appName": "Updated Reporting Dashboard",
  "appSecretHash": "$2a$11$new-hashed-secret",
  "allowedScopes": "[\"read:members\", \"read:statistics\", \"read:organization\"]",
  "tokenExpirationHours": 4,
  "isActive": true
}
```

---

## Test 5: Delete App (Admin Only)

### Endpoint
```
DELETE https://localhost:7278/api/auth/apps/{appId}
Authorization: Bearer {admin-jwt-token}
```

### Using PowerShell
```powershell
$headers = @{
    "Authorization" = "Bearer your-admin-jwt-token"
}

Invoke-RestMethod -Uri "https://localhost:7278/api/auth/apps/TestApp" `
    -Method DELETE `
    -Headers $headers `
    -SkipCertificateCheck
```

---

## Available Scopes

### Member Scopes
- `read:members` - Read member information
- `export:members` - Export member data
- `verify:membership` - Verify membership status

### Organization Scopes
- `read:organization` - Read organizational structure (units, states, departments)

### Analytics Scopes (Require Roles)
- `read:statistics` - Read statistical data (filtered by department/level)
- `read:exco` - Read executive committee information

---

## Database Setup

### 1. Create AppRegistrations Table
```sql
CREATE TABLE AppRegistrations (
    AppId NVARCHAR(100) PRIMARY KEY,
    AppName NVARCHAR(200) NOT NULL,
    AppSecretHash NVARCHAR(500) NOT NULL,
    AllowedScopes NVARCHAR(MAX) NOT NULL,
    TokenExpirationHours INT NOT NULL DEFAULT 1,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastUsedAt DATETIME2 NULL
);
```

### 2. Seed Test App
```sql
INSERT INTO AppRegistrations (AppId, AppName, AppSecretHash, AllowedScopes, TokenExpirationHours, IsActive)
VALUES (
    'ReportingApp',
    'Reporting Dashboard',
    '$2a$11$test.hashed.secret',
    '["read:members", "read:statistics", "read:organization"]',
    2,
    1
);
```

### 3. Verify Member Exists
```sql
SELECT MemberId, Mkanid, FirstName, LastName, Email
FROM Members
WHERE Mkanid = 12345;
```

---

## Quick PowerShell Test

Run the automated test script:
```powershell
.\test-auth.ps1
```

This will test:
- ? Token generation with valid credentials
- ? Token generation with invalid appId
- ? Token generation with invalid scopes
- ? Admin endpoints (will fail without auth, as expected)
- ? JWT token decoding

---

## Troubleshooting

### Error: "JWT SecretKey must be at least 32 characters"
```powershell
dotnet user-secrets set "Jwt:SecretKey" "your-very-long-secret-key-at-least-32-chars"
dotnet user-secrets set "Jwt:Issuer" "AmsaAPI"
dotnet user-secrets set "Jwt:Audience" "ReportingApp"
```

### Error: "App 'ReportingApp' not found"
1. Check if app exists in database
2. Verify connection string
3. Run database migrations

### Error: "Member 12345 not found"
1. Check Members table for valid MKAN ID
2. Use existing member ID from your database

### Error: "Invalid AllowedScopes"
Ensure AllowedScopes is valid JSON string:
```json
"[\"read:members\", \"read:statistics\"]"
```

---

## Next Steps

1. ? Test token generation
2. ? Decode JWT to verify claims
3. ? Use token to access protected endpoints
4. ? Test scope-based authorization
5. ? Test role-based data filtering
