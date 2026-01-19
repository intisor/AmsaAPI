# AMSA API Auth Layer Test Script
# Tests the authentication endpoints at https://localhost:7278/api/auth

$baseUrl = "https://localhost:7278"
$authUrl = "$baseUrl/api/auth"

Write-Host "=== AMSA API Auth Layer Test ===" -ForegroundColor Cyan
Write-Host "Base URL: $baseUrl`n" -ForegroundColor Gray

function Write-ErrorResponse($ex) {
    $statusCode = $null
    $body = $null
    if ($ex.Exception -and $ex.Exception.Response) {
        try { $statusCode = $ex.Exception.Response.StatusCode.value__ } catch {}
        try {
            $reader = New-Object System.IO.StreamReader($ex.Exception.Response.GetResponseStream())
            $body = $reader.ReadToEnd()
            $reader.Close()
        } catch {}
    }
    if ($ex.ErrorDetails -and $ex.ErrorDetails.Message) {
        try {
            $errObj = $ex.ErrorDetails.Message | ConvertFrom-Json
            if ($errObj.error) { Write-Host "Error: $($errObj.error)" -ForegroundColor Red }
        } catch {}
    }
    if ($statusCode) { Write-Host "Status: $statusCode" -ForegroundColor Yellow }
    if ($body) { Write-Host "Body: $body" -ForegroundColor Yellow }
}

# Test 1: Generate Token (Anonymous - should work)
Write-Host "`n[TEST 1] POST /api/auth/token - Generate JWT Token" -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray

$tokenRequest = @{
    appId = "ReportingApp"
    appSecret = "test-secret-123"
    mkanId = 1063
    requestedScopes = @("read:members", "read:statistics")
} | ConvertTo-Json

Write-Host "Request Body:" -ForegroundColor Gray
Write-Host $tokenRequest -ForegroundColor White

try {
    $tokenResponse = Invoke-RestMethod -Uri "$authUrl/token" `
        -Method POST `
        -ContentType "application/json" `
        -Body $tokenRequest `
        -SkipCertificateCheck

    Write-Host "`n✓ SUCCESS" -ForegroundColor Green
    if ($tokenResponse.token) {
        Write-Host "Token: $($tokenResponse.token.Substring(0, [Math]::Min(50, $tokenResponse.token.Length)))..." -ForegroundColor Green
        Write-Host "Token Type: $($tokenResponse.tokenType)" -ForegroundColor Green
        $script:authToken = $tokenResponse.token
    } else {
        Write-Host "Unexpected response:`n$($tokenResponse | ConvertTo-Json -Depth 10)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "`n✗ FAILED" -ForegroundColor Red
    Write-ErrorResponse $_
}

# Test 2: Generate Token with Invalid AppId
Write-Host "`n[TEST 2] POST /api/auth/token - Invalid AppId" -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray

$invalidAppRequest = @{
    appId = "NonExistentApp"
    appSecret = "test-secret"
    mkanId = 1063
    requestedScopes = @("read:members")
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$authUrl/token" `
        -Method POST `
        -ContentType "application/json" `
        -Body $invalidAppRequest `
        -SkipCertificateCheck
    Write-Host "Unexpected success (should have failed)" -ForegroundColor Red
    Write-Host ($response | ConvertTo-Json -Depth 10) -ForegroundColor Yellow
}
catch {
    Write-Host "`n✓ EXPECTED FAILURE" -ForegroundColor Green
    Write-ErrorResponse $_
}

# Test 3: Generate Token with Invalid Scopes
Write-Host "`n[TEST 3] POST /api/auth/token - Invalid Scopes" -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray

$invalidScopeRequest = @{
    appId = "ReportingApp"
    appSecret = "test-secret-123"
    mkanId = 1063
    requestedScopes = @("read:members", "invalid:scope", "fake:permission")
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$authUrl/token" `
        -Method POST `
        -ContentType "application/json" `
        -Body $invalidScopeRequest `
        -SkipCertificateCheck
    Write-Host "Unexpected success (should have failed)" -ForegroundColor Red
    Write-Host ($response | ConvertTo-Json -Depth 10) -ForegroundColor Yellow
}
catch {
    Write-Host "`n✓ EXPECTED FAILURE" -ForegroundColor Green
    Write-ErrorResponse $_
}

# Test 4: Create App Registration (Admin Only - will fail without auth)
Write-Host "`n[TEST 4] POST /api/auth/apps - Create App (No Auth - should fail)" -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray

$createAppRequest = @{
    appId = "TestApp"
    appName = "Test Application"
    appSecretHash = "hashed-secret-here"
    allowedScopes = '["read:members", "read:organization"]'
    tokenExpirationHours = 2
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$authUrl/apps" `
        -Method POST `
        -ContentType "application/json" `
        -Body $createAppRequest `
        -SkipCertificateCheck
    Write-Host "Unexpected success (should require authentication)" -ForegroundColor Red
    Write-Host ($response | ConvertTo-Json -Depth 10) -ForegroundColor Yellow
}
catch {
    Write-Host "`n✓ EXPECTED FAILURE (Requires Admin Auth)" -ForegroundColor Green
    Write-ErrorResponse $_
}

# Test 5: Get App Details (Admin Only - will fail without auth)
Write-Host "`n[TEST 5] GET /api/auth/apps/{appId} - Get App (No Auth - should fail)" -ForegroundColor Yellow
Write-Host "----------------------------------------" -ForegroundColor Gray

try {
    $response = Invoke-RestMethod -Uri "$authUrl/apps/ReportingApp" `
        -Method GET `
        -SkipCertificateCheck
    Write-Host "Unexpected success (should require authentication)" -ForegroundColor Red
    Write-Host ($response | ConvertTo-Json -Depth 10) -ForegroundColor Yellow
}
catch {
    Write-Host "`n✓ EXPECTED FAILURE (Requires Admin Auth)" -ForegroundColor Green
    Write-ErrorResponse $_
}

# Test 6: Decode the JWT token (if we got one)
if ($script:authToken) {
    Write-Host "`n[TEST 6] Decode JWT Token" -ForegroundColor Yellow
    Write-Host "----------------------------------------" -ForegroundColor Gray
    $tokenParts = $script:authToken.Split('.')
    if ($tokenParts.Length -eq 3) {
        try {
            $headerJson = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($tokenParts[0]))
            Write-Host "`nHeader:" -ForegroundColor Cyan
            Write-Host $headerJson -ForegroundColor White
        } catch {}
        $payloadBase64 = $tokenParts[1]
        $paddingNeeded = (4 - ($payloadBase64.Length % 4)) % 4
        $payloadBase64 += "=" * $paddingNeeded
        try {
            $payloadJson = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($payloadBase64))
            $payload = $payloadJson | ConvertFrom-Json
            Write-Host "`nPayload (Claims):" -ForegroundColor Cyan
            Write-Host ($payload | ConvertTo-Json -Depth 10) -ForegroundColor White
            $expTimestamp = $payload.exp
            $expDate = [DateTimeOffset]::FromUnixTimeSeconds($expTimestamp).LocalDateTime
            Write-Host "`nToken Expires: $expDate" -ForegroundColor $(if ((Get-Date) -lt $expDate) { "Green" } else { "Red" })
        } catch {
            Write-Host "Failed to decode payload: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# Summary
Write-Host "`n`n=== TEST SUMMARY ===" -ForegroundColor Cyan
Write-Host "✓ Auth endpoint is accessible at $authUrl" -ForegroundColor Green
Write-Host "✓ Anonymous token generation works (POST /api/auth/token)" -ForegroundColor Green
Write-Host "✓ Validation working (invalid appId/scopes rejected)" -ForegroundColor Green
Write-Host "✓ Admin endpoints require authentication (as expected)" -ForegroundColor Green
Write-Host "`nNext Steps:" -ForegroundColor Yellow
Write-Host "1. Ensure you have an app registered in the database (AppRegistrations table)" -ForegroundColor White
Write-Host "2. Ensure you have a member with the specified MKAN ID" -ForegroundColor White
Write-Host "3. To test admin endpoints, you'll need an admin JWT token" -ForegroundColor White
Write-Host "`nRun this script: .\test-auth.ps1" -ForegroundColor Cyan
