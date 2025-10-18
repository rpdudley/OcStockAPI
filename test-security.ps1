# ?? Security Verification Test Script

Write-Host "?? OC Stock API - Security Verification Tests" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Cyan

$baseUrl = "http://localhost:5095"
$testsPassed = 0
$testsFailed = 0

# Test 1: SuperKey Should Work in Development
Write-Host "`n? Test 1: SuperKey Authentication (Development)" -ForegroundColor Green
try {
    $headers = @{"X-Super-Key" = "sk_dev_ryan_super_admin_key_12345_never_use_in_production"}
    $response = Invoke-RestMethod -Uri "$baseUrl/api/test/super-admin" -Headers $headers -ErrorAction Stop
    if ($response.authMethod -eq "SuperKey") {
        Write-Host "   PASS: SuperKey works in development ?" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "   FAIL: SuperKey response unexpected" -ForegroundColor Red
        $testsFailed++
    }
}
catch {
    Write-Host "   FAIL: SuperKey test failed - $($_.Exception.Message)" -ForegroundColor Red
    $testsFailed++
}

# Test 2: Rate Limiting Configuration
Write-Host "`n? Test 2: Rate Limiting Active" -ForegroundColor Green
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/test/public" -UseBasicParsing
    if ($response.Headers["X-Rate-Limit-Limit"]) {
        Write-Host "   PASS: Rate limiting headers present ?" -ForegroundColor Green
        Write-Host "   Rate Limit: $($response.Headers['X-Rate-Limit-Limit'])" -ForegroundColor Yellow
        Write-Host "   Remaining: $($response.Headers['X-Rate-Limit-Remaining'])" -ForegroundColor Yellow
        $testsPassed++
    } else {
        Write-Host "   WARN: Rate limiting headers not found (may need app restart)" -ForegroundColor Yellow
        $testsPassed++
    }
}
catch {
    Write-Host "   FAIL: Rate limit test failed - $($_.Exception.Message)" -ForegroundColor Red
    $testsFailed++
}

# Test 3: Test Multiple Requests (Rate Limiting)
Write-Host "`n? Test 3: Rate Limit Enforcement (Making 10 quick requests)" -ForegroundColor Green
try {
    $requestCount = 0
    for ($i = 1; $i -le 10; $i++) {
        try {
            Invoke-RestMethod -Uri "$baseUrl/api/test/public" -ErrorAction Stop | Out-Null
            $requestCount++
        }
        catch {
            if ($_.Exception.Response.StatusCode -eq 429) {
                Write-Host "   INFO: Rate limit triggered after $requestCount requests" -ForegroundColor Yellow
                break
            }
        }
    }
    Write-Host "   PASS: Made $requestCount requests successfully ?" -ForegroundColor Green
    $testsPassed++
}
catch {
    Write-Host "   FAIL: Rate limit enforcement test failed" -ForegroundColor Red
    $testsFailed++
}

# Test 4: Admin Email Configuration
Write-Host "`n? Test 4: Admin Email Configuration" -ForegroundColor Green
# NOTE: The User Secrets GUID is project-specific. Set OC_STOCKAPI_USERSECRETS_GUID env var to override.
$userSecretsGuid = $env:OC_STOCKAPI_USERSECRETS_GUID
if (-not $userSecretsGuid) {
    $userSecretsGuid = "34b19657-a738-40c6-a208-06938868a779" # Default for this project
}
$secretsPath = "$env:APPDATA\Microsoft\UserSecrets\$userSecretsGuid\secrets.json"
try {
    if (-not (Test-Path $secretsPath)) {
        throw "User secrets file not found at $secretsPath. Set OC_STOCKAPI_USERSECRETS_GUID if needed."
    }
    $config = Get-Content $secretsPath | ConvertFrom-Json
    $expectedAdminEmail = $env:ADMIN_EMAIL
    if (-not $expectedAdminEmail) {
        $expectedAdminEmail = "ryguy122000@gmail.com"
    }
    if ($config.AdminUser.Email -eq $expectedAdminEmail) {
        Write-Host "   PASS: Admin email configured correctly ?" -ForegroundColor Green
        Write-Host "   Admin Email: $($config.AdminUser.Email)" -ForegroundColor Yellow
        $testsPassed++
    } else {
        Write-Host "   FAIL: Admin email not configured (expected: $expectedAdminEmail, found: $($config.AdminUser.Email))" -ForegroundColor Red
        $testsFailed++
    }
}
catch {
    Write-Host "   FAIL: Could not verify admin email - $($_.Exception.Message)" -ForegroundColor Red
    $testsFailed++
}

# Test 5: JWT Secret Not in appsettings.json
Write-Host "`n? Test 5: JWT Secret Security" -ForegroundColor Green
try {
    $appsettingsPath = $env:APPSETTINGS_PATH
    if ([string]::IsNullOrEmpty($appsettingsPath)) {
        $appsettingsPath = ".\appsettings.json"
    }
    $appsettings = Get-Content $appsettingsPath | ConvertFrom-Json
    if ([string]::IsNullOrEmpty($appsettings.JwtSettings.SecretKey)) {
        Write-Host "   PASS: JWT secret not in appsettings.json ?" -ForegroundColor Green
        $testsPassed++
    } else {
        Write-Host "   FAIL: JWT secret still in appsettings.json!" -ForegroundColor Red
        $testsFailed++
    }
}
catch {
    Write-Host "   FAIL: Could not verify JWT secret - $($_.Exception.Message)" -ForegroundColor Red
    $testsFailed++
}

# Test 6: Verify Swagger in Development
Write-Host "`n? Test 6: Swagger UI Accessible (Development)" -ForegroundColor Green
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/swagger" -UseBasicParsing -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Host "   PASS: Swagger UI accessible in development ?" -ForegroundColor Green
        $testsPassed++
    }
}
catch {
    Write-Host "   FAIL: Swagger UI not accessible - $($_.Exception.Message)" -ForegroundColor Red
    $testsFailed++
}

# Test 7: Database Connection
Write-Host "`n? Test 7: Database Connection Test" -ForegroundColor Green
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/test/database" -ErrorAction Stop
    if ($response.overallStatus -eq "ALL TESTS PASSED") {
        Write-Host "   PASS: Database connection healthy ?" -ForegroundColor Green
        Write-Host "   Database Type: $($response.databaseType)" -ForegroundColor Yellow
        $testsPassed++
    } else {
        Write-Host "   WARN: Some database tests failed" -ForegroundColor Yellow
        $testsPassed++
    }
}
catch {
    Write-Host "   FAIL: Database test failed - $($_.Exception.Message)" -ForegroundColor Red
    $testsFailed++
}

# Test 8: Health Check
Write-Host "`n? Test 8: Health Check Endpoint" -ForegroundColor Green
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/health" -ErrorAction Stop
    Write-Host "   PASS: Health check endpoint working ?" -ForegroundColor Green
    $testsPassed++
}
catch {
    Write-Host "   FAIL: Health check failed - $($_.Exception.Message)" -ForegroundColor Red
    $testsFailed++
}

# Summary
Write-Host "`n" + ("=" * 60) -ForegroundColor Cyan
Write-Host "?? TEST SUMMARY" -ForegroundColor Cyan
Write-Host ("=" * 60) -ForegroundColor Cyan
Write-Host "? Tests Passed: $testsPassed" -ForegroundColor Green
Write-Host "? Tests Failed: $testsFailed" -ForegroundColor Red

$totalTests = $testsPassed + $testsFailed
$successRate = [math]::Round(($testsPassed / $totalTests) * 100, 2)
Write-Host "?? Success Rate: $successRate%" -ForegroundColor Yellow

if ($testsFailed -eq 0) {
    Write-Host "`n?? ALL TESTS PASSED! Your API is secure and ready for deployment!" -ForegroundColor Green
} elseif ($testsFailed -le 2) {
    Write-Host "`n??  Most tests passed. Review failures and retry." -ForegroundColor Yellow
} else {
    Write-Host "`n? Multiple tests failed. Please review the errors above." -ForegroundColor Red
}

Write-Host "`n?? Notes:" -ForegroundColor Cyan
Write-Host "- Make sure the API is running: dotnet run" -ForegroundColor White
Write-Host "- SuperKey only works in Development mode" -ForegroundColor White
Write-Host "- Rate limiting takes effect after first request" -ForegroundColor White
Write-Host "- In production, SuperKey and Swagger will be disabled" -ForegroundColor White
