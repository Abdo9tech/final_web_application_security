# Bookify Frontend Testing Script
# Tests all pages for proper rendering, accessibility, and functionality

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  BOOKIFY FRONTEND TESTING SUITE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5280"
$results = @()

function Test-Page {
    param(
        [string]$Name,
        [string]$Url,
        [string[]]$ExpectedContent,
        [string[]]$ForbiddenContent,
        [bool]$RequiresAuth = $false
    )
    
    Write-Host "Testing: $Name" -ForegroundColor Yellow
    Write-Host "URL: $Url" -ForegroundColor Gray
    
    try {
        $response = Invoke-WebRequest -Uri $Url -Method GET -UseBasicParsing -TimeoutSec 10
        
        $result = @{
            Page = $Name
            URL = $Url
            Status = $response.StatusCode
            Success = $true
            Issues = @()
        }
        
        # Check for expected content
        foreach ($content in $ExpectedContent) {
            if ($response.Content -notmatch [regex]::Escape($content)) {
                $result.Issues += "Missing expected content: $content"
                $result.Success = $false
            }
        }
        
        # Check for forbidden content (Booking.com branding)
        foreach ($content in $ForbiddenContent) {
            if ($response.Content -match [regex]::Escape($content)) {
                $result.Issues += "Found forbidden content: $content"
                $result.Success = $false
            }
        }
        
        # Check for common issues
        if ($response.Content -match "404|Not Found" -and $response.StatusCode -eq 200) {
            $result.Issues += "Page contains 404 error"
            $result.Success = $false
        }
        
        if ($response.Content -match "Exception|Error:|Stack Trace") {
            $result.Issues += "Page contains error messages"
            $result.Success = $false
        }
        
        if ($result.Success) {
            Write-Host "  ✅ PASS" -ForegroundColor Green
        } else {
            Write-Host "  ❌ FAIL" -ForegroundColor Red
            foreach ($issue in $result.Issues) {
                Write-Host "    - $issue" -ForegroundColor Red
            }
        }
        
    } catch {
        $result = @{
            Page = $Name
            URL = $Url
            Status = "Error"
            Success = $false
            Issues = @("Failed to load: $($_.Exception.Message)")
        }
        Write-Host "  ❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host ""
    return $result
}

# Test Suite
Write-Host "Starting Frontend Tests..." -ForegroundColor Cyan
Write-Host ""

# Public Pages
$results += Test-Page -Name "Homepage" -Url "$baseUrl/" `
    -ExpectedContent @("Bookify", "Search") `
    -ForbiddenContent @("#003580", "#febb02", "booking.com", "Booking.com")

$results += Test-Page -Name "AI Assistant" -Url "$baseUrl/aiAssistant" `
    -ExpectedContent @("AI Assistant", "Bookify") `
    -ForbiddenContent @("#003580", "#febb02")

$results += Test-Page -Name "Room Listing" -Url "$baseUrl/Room" `
    -ExpectedContent @("Room", "Search") `
    -ForbiddenContent @("#003580", "#febb02")

$results += Test-Page -Name "About Page" -Url "$baseUrl/About" `
    -ExpectedContent @("About") `
    -ForbiddenContent @("#003580", "#febb02")

$results += Test-Page -Name "Contact Page" -Url "$baseUrl/Contact" `
    -ExpectedContent @("Contact") `
    -ForbiddenContent @("#003580", "#febb02")

$results += Test-Page -Name "Login Page" -Url "$baseUrl/Login/Login" `
    -ExpectedContent @("Sign In", "Login") `
    -ForbiddenContent @("#003580", "#febb02")

$results += Test-Page -Name "Register Page" -Url "$baseUrl/Register/Register" `
    -ExpectedContent @("Register", "Sign Up") `
    -ForbiddenContent @("#003580", "#febb02")

# Protected Pages (will redirect to login)
$results += Test-Page -Name "Dashboard (Admin)" -Url "$baseUrl/dashboard" `
    -ExpectedContent @("Sign In", "Login", "Dashboard") `
    -ForbiddenContent @()

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TEST SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$totalTests = $results.Count
$passedTests = ($results | Where-Object { $_.Success }).Count
$failedTests = $totalTests - $passedTests
$passRate = [math]::Round(($passedTests / $totalTests) * 100, 2)

Write-Host "Total Tests: $totalTests" -ForegroundColor White
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $failedTests" -ForegroundColor Red
Write-Host "Pass Rate: $passRate%" -ForegroundColor $(if ($passRate -ge 80) { "Green" } else { "Yellow" })
Write-Host ""

# Detailed Results
if ($failedTests -gt 0) {
    Write-Host "Failed Tests:" -ForegroundColor Red
    foreach ($result in $results | Where-Object { -not $_.Success }) {
        Write-Host "  ❌ $($result.Page)" -ForegroundColor Red
        foreach ($issue in $result.Issues) {
            Write-Host "     - $issue" -ForegroundColor Gray
        }
    }
    Write-Host ""
}

# Design System Check
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  DESIGN SYSTEM CHECK" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Checking for Booking.com branding..." -ForegroundColor Yellow
$bookingComColors = @("#003580", "#febb02", "booking-")
$foundIssues = @()

foreach ($result in $results) {
    if ($result.Status -eq 200 -or $result.Status -eq "OK") {
        try {
            $response = Invoke-WebRequest -Uri $result.URL -Method GET -UseBasicParsing
            foreach ($color in $bookingComColors) {
                if ($response.Content -match [regex]::Escape($color)) {
                    $foundIssues += "$($result.Page): Found '$color'"
                }
            }
        } catch {
            # Skip if page failed to load
        }
    }
}

if ($foundIssues.Count -eq 0) {
    Write-Host "✅ No Booking.com branding found!" -ForegroundColor Green
} else {
    Write-Host "❌ Found Booking.com branding:" -ForegroundColor Red
    foreach ($issue in $foundIssues) {
        Write-Host "  - $issue" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TESTING COMPLETE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Export results to JSON
$results | ConvertTo-Json -Depth 10 | Out-File "frontend-test-results.json"
Write-Host ""
Write-Host "Results exported to: frontend-test-results.json" -ForegroundColor Gray
