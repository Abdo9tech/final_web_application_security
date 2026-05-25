# Bookify Hotel - Endpoint Testing Script
# Tests all major endpoints to verify functionality

$baseUrl = "http://localhost:5280"
$results = @()

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [string]$Method = "GET",
        [hashtable]$Headers = @{},
        [string]$Body = $null,
        [int]$ExpectedStatus = 200
    )
    
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "Testing: $Name" -ForegroundColor Cyan
    Write-Host "URL: $Url" -ForegroundColor Gray
    Write-Host "Method: $Method" -ForegroundColor Gray
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            Headers = $Headers
            TimeoutSec = 10
            UseBasicParsing = $true
        }
        
        if ($Body) {
            $params.Body = $Body
            $params.ContentType = "application/json"
        }
        
        $response = Invoke-WebRequest @params -ErrorAction Stop
        
        $status = $response.StatusCode
        $success = ($status -eq $ExpectedStatus)
        
        if ($success) {
            Write-Host "✅ PASS - Status: $status" -ForegroundColor Green
        } else {
            Write-Host "⚠️  WARN - Status: $status (Expected: $ExpectedStatus)" -ForegroundColor Yellow
        }
        
        $results += [PSCustomObject]@{
            Name = $Name
            URL = $Url
            Method = $Method
            Status = $status
            Expected = $ExpectedStatus
            Result = if ($success) { "PASS" } else { "WARN" }
            ResponseLength = $response.Content.Length
        }
        
        return $response
    }
    catch {
        $status = if ($_.Exception.Response) { $_.Exception.Response.StatusCode.value__ } else { "ERROR" }
        Write-Host "❌ FAIL - Status: $status" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        
        $results += [PSCustomObject]@{
            Name = $Name
            URL = $Url
            Method = $Method
            Status = $status
            Expected = $ExpectedStatus
            Result = "FAIL"
            ResponseLength = 0
        }
        
        return $null
    }
}

Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║     BOOKIFY HOTEL - ENDPOINT TESTING SUITE                ║" -ForegroundColor Magenta
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Magenta

# Wait for application to be ready
Write-Host "`nWaiting for application to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# ============================================
# PUBLIC ENDPOINTS (No Authentication)
# ============================================

Write-Host "`n┌────────────────────────────────────────┐" -ForegroundColor Blue
Write-Host "│  PUBLIC ENDPOINTS                      │" -ForegroundColor Blue
Write-Host "└────────────────────────────────────────┘" -ForegroundColor Blue

Test-Endpoint -Name "Homepage" -Url "$baseUrl/"
Test-Endpoint -Name "About Page" -Url "$baseUrl/About"
Test-Endpoint -Name "Contact Page" -Url "$baseUrl/Contact"
Test-Endpoint -Name "Room Listing" -Url "$baseUrl/Room"
Test-Endpoint -Name "Room Search with Filters" -Url "$baseUrl/Room?location=Manchester&guests=2"
Test-Endpoint -Name "Login Page" -Url "$baseUrl/Login/Login"

# ============================================
# HEALTH CHECK ENDPOINTS
# ============================================

Write-Host "`n┌────────────────────────────────────────┐" -ForegroundColor Blue
Write-Host "│  HEALTH CHECK ENDPOINTS                │" -ForegroundColor Blue
Write-Host "└────────────────────────────────────────┘" -ForegroundColor Blue

Test-Endpoint -Name "Health Check" -Url "$baseUrl/health"

# ============================================
# API ENDPOINTS (May require auth)
# ============================================

Write-Host "`n┌────────────────────────────────────────┐" -ForegroundColor Blue
Write-Host "│  API ENDPOINTS                         │" -ForegroundColor Blue
Write-Host "└────────────────────────────────────────┘" -ForegroundColor Blue

# These will return 401 if not authenticated, which is expected
Test-Endpoint -Name "Smart Search API" -Url "$baseUrl/api/SmartSearch" -ExpectedStatus 401
Test-Endpoint -Name "Watch API" -Url "$baseUrl/api/Watch?email=test@example.com" -ExpectedStatus 401
Test-Endpoint -Name "Agent Report API" -Url "$baseUrl/api/AgentReport" -ExpectedStatus 401

# ============================================
# STATIC RESOURCES
# ============================================

Write-Host "`n┌────────────────────────────────────────┐" -ForegroundColor Blue
Write-Host "│  STATIC RESOURCES                      │" -ForegroundColor Blue
Write-Host "└────────────────────────────────────────┘" -ForegroundColor Blue

Test-Endpoint -Name "Design System CSS" -Url "$baseUrl/css/design-system.css"
Test-Endpoint -Name "Theme Switcher JS" -Url "$baseUrl/js/theme-switcher.js"
Test-Endpoint -Name "Toast JS" -Url "$baseUrl/js/toast.js"

# ============================================
# PROTECTED ENDPOINTS (Should redirect or 401)
# ============================================

Write-Host "`n┌────────────────────────────────────────┐" -ForegroundColor Blue
Write-Host "│  PROTECTED ENDPOINTS (Auth Required)   │" -ForegroundColor Blue
Write-Host "└────────────────────────────────────────┘" -ForegroundColor Blue

# These should redirect to login (302) or return 401
Test-Endpoint -Name "Profile Page" -Url "$baseUrl/Profile" -ExpectedStatus 302
Test-Endpoint -Name "My Bookings" -Url "$baseUrl/Booking/MyBookings" -ExpectedStatus 302
Test-Endpoint -Name "Favorites" -Url "$baseUrl/Favorite" -ExpectedStatus 302
Test-Endpoint -Name "Dashboard (Admin)" -Url "$baseUrl/dashboard" -ExpectedStatus 302

# ============================================
# RESULTS SUMMARY
# ============================================

Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║     TEST RESULTS SUMMARY                                   ║" -ForegroundColor Magenta
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Magenta

$totalTests = $results.Count
$passedTests = ($results | Where-Object { $_.Result -eq "PASS" }).Count
$warnTests = ($results | Where-Object { $_.Result -eq "WARN" }).Count
$failedTests = ($results | Where-Object { $_.Result -eq "FAIL" }).Count

Write-Host "`nTotal Tests: $totalTests" -ForegroundColor White
Write-Host "✅ Passed: $passedTests" -ForegroundColor Green
Write-Host "⚠️  Warnings: $warnTests" -ForegroundColor Yellow
Write-Host "❌ Failed: $failedTests" -ForegroundColor Red

$successRate = [math]::Round(($passedTests / $totalTests) * 100, 2)
Write-Host "`nSuccess Rate: $successRate%" -ForegroundColor $(if ($successRate -ge 80) { "Green" } elseif ($successRate -ge 60) { "Yellow" } else { "Red" })

# Display detailed results table
Write-Host "`n┌─────────────────────────────────────────────────────────────────────────────┐" -ForegroundColor Cyan
Write-Host "│  DETAILED RESULTS                                                           │" -ForegroundColor Cyan
Write-Host "└─────────────────────────────────────────────────────────────────────────────┘" -ForegroundColor Cyan

$results | Format-Table -Property Name, Method, Status, Expected, Result, @{Label="Size(bytes)"; Expression={$_.ResponseLength}} -AutoSize

# Export results to JSON
$results | ConvertTo-Json | Out-File "endpoint-test-results.json"
Write-Host "`n📄 Detailed results exported to: endpoint-test-results.json" -ForegroundColor Cyan

Write-Host "`n✅ Testing Complete!" -ForegroundColor Green
Write-Host "========================================`n" -ForegroundColor Cyan
