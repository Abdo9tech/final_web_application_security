# Dashboard Layout Fix Test Script
# Tests for horizontal overflow, responsive design, and proper container constraints

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Dashboard Layout Fix Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5280"
$dashboardUrl = "$baseUrl/dashboard"

Write-Host "Testing Dashboard at: $dashboardUrl" -ForegroundColor Yellow
Write-Host ""

# Test 1: Check if dashboard page loads
Write-Host "[TEST 1] Dashboard Page Load" -ForegroundColor Magenta
try {
    $response = Invoke-WebRequest -Uri $dashboardUrl -UseBasicParsing -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "  [PASS] Dashboard loads successfully (200 OK)" -ForegroundColor Green
    } else {
        Write-Host "  [FAIL] Unexpected status code: $($response.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "  [FAIL] Failed to load dashboard: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  Note: You may need to be logged in as admin" -ForegroundColor Yellow
}
Write-Host ""

# Test 2: Check for overflow-x-hidden class
Write-Host "[TEST 2] Overflow Prevention" -ForegroundColor Magenta
try {
    $response = Invoke-WebRequest -Uri $dashboardUrl -UseBasicParsing -TimeoutSec 10
    $content = $response.Content
    
    if ($content -match 'overflow-x-hidden') {
        Write-Host "  [PASS] overflow-x-hidden class found" -ForegroundColor Green
    } else {
        Write-Host "  [WARN] overflow-x-hidden class not found" -ForegroundColor Yellow
    }
    
    if ($content -match 'max-w-7xl') {
        Write-Host "  [PASS] max-w-7xl container constraint found" -ForegroundColor Green
    } else {
        Write-Host "  [FAIL] max-w-7xl container constraint missing" -ForegroundColor Red
    }
} catch {
    Write-Host "  [WARN] Could not verify overflow prevention" -ForegroundColor Yellow
}
Write-Host ""

# Test 3: Check for responsive grid classes
Write-Host "[TEST 3] Responsive Grid Layout" -ForegroundColor Magenta
try {
    $response = Invoke-WebRequest -Uri $dashboardUrl -UseBasicParsing -TimeoutSec 10
    $content = $response.Content
    
    if ($content -match 'grid-cols-1 md:grid-cols-2 lg:grid-cols-4') {
        Write-Host "  [PASS] Responsive stat cards grid found" -ForegroundColor Green
    } else {
        Write-Host "  [WARN] Responsive grid classes may be missing" -ForegroundColor Yellow
    }
    
    if ($content -match 'grid-cols-1 lg:grid-cols-2') {
        Write-Host "  [PASS] Responsive charts grid found" -ForegroundColor Green
    } else {
        Write-Host "  [WARN] Charts grid may not be responsive" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  [WARN] Could not verify responsive grid" -ForegroundColor Yellow
}
Write-Host ""

# Test 4: Check for chart canvas elements
Write-Host "[TEST 4] Chart Elements" -ForegroundColor Magenta
try {
    $response = Invoke-WebRequest -Uri $dashboardUrl -UseBasicParsing -TimeoutSec 10
    $content = $response.Content
    
    $charts = @('bookingTrendsChart', 'revenueChart', 'occupancyChart')
    foreach ($chart in $charts) {
        if ($content -match $chart) {
            Write-Host "  [PASS] $chart canvas found" -ForegroundColor Green
        } else {
            Write-Host "  [FAIL] $chart canvas missing" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "  [WARN] Could not verify chart elements" -ForegroundColor Yellow
}
Write-Host ""

# Test 5: Check for table overflow handling
Write-Host "[TEST 5] Table Overflow Handling" -ForegroundColor Magenta
try {
    $response = Invoke-WebRequest -Uri $dashboardUrl -UseBasicParsing -TimeoutSec 10
    $content = $response.Content
    
    if ($content -match 'overflow-x-auto') {
        Write-Host "  [PASS] Table overflow-x-auto wrapper found" -ForegroundColor Green
    } else {
        Write-Host "  [WARN] Table may not handle overflow properly" -ForegroundColor Yellow
    }
    
    if ($content -match 'min-w-full') {
        Write-Host "  [WARN] Table uses min-w-full (may cause overflow)" -ForegroundColor Yellow
        Write-Host "    Recommendation: Changed to w-full for better responsiveness" -ForegroundColor Cyan
    } else {
        Write-Host "  [PASS] Table width properly constrained" -ForegroundColor Green
    }
} catch {
    Write-Host "  [WARN] Could not verify table overflow handling" -ForegroundColor Yellow
}
Write-Host ""

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Layout Fixes Applied:" -ForegroundColor Yellow
Write-Host "  - Added overflow-x-hidden to main dashboard container" -ForegroundColor White
Write-Host "  - Added w-full to all chart containers and grids" -ForegroundColor White
Write-Host "  - Changed table from min-w-full to w-full" -ForegroundColor White
Write-Host "  - Added max-width: 100% to canvas elements" -ForegroundColor White
Write-Host "  - Added global overflow-x: hidden to html/body" -ForegroundColor White
Write-Host "  - Added min-width: 0 to grid items" -ForegroundColor White
Write-Host "  - Wrapped chart canvases in fixed-height containers" -ForegroundColor White
Write-Host ""
Write-Host "Expected Results:" -ForegroundColor Yellow
Write-Host "  [OK] No horizontal scrolling" -ForegroundColor Green
Write-Host "  [OK] All content fits within viewport width" -ForegroundColor Green
Write-Host "  [OK] Charts render at proper size without overflow" -ForegroundColor Green
Write-Host "  [OK] Tables scroll horizontally within their container only" -ForegroundColor Green
Write-Host "  [OK] Responsive design works across all screen sizes" -ForegroundColor Green
Write-Host "  [OK] No blank/white sections or stretched containers" -ForegroundColor Green
Write-Host ""
Write-Host "Manual Testing Required:" -ForegroundColor Cyan
Write-Host "  1. Open dashboard in browser: $dashboardUrl" -ForegroundColor White
Write-Host "  2. Check for horizontal scrollbar (should be none)" -ForegroundColor White
Write-Host "  3. Resize browser window to test responsive behavior" -ForegroundColor White
Write-Host "  4. Verify all charts render correctly" -ForegroundColor White
Write-Host "  5. Check table scrolls within its container" -ForegroundColor White
Write-Host "  6. Test dark mode toggle" -ForegroundColor White
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
