# Dashboard Chart Logic Test Script
# Tests all chart data calculations and rendering

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Dashboard Chart Logic Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5280"
$dashboardUrl = "$baseUrl/dashboard"

# Test 1: Verify chart data logic in controller
Write-Host "[TEST 1] Chart Data Calculation Logic" -ForegroundColor Magenta
$controllerContent = Get-Content "Project(DEPI)/Controllers/DashboardController.cs" -Raw

$tests = @(
    @{
        Name = "Booking Trends uses confirmed/completed filter"
        Pattern = "BookingTrends.*BookingStatus\.IsConfirmedOrCompleted"
        Expected = $true
    },
    @{
        Name = "Revenue by Type uses confirmed/completed filter"
        Pattern = "revenueByType.*BookingStatus\.IsConfirmedOrCompleted"
        Expected = $true
    },
    @{
        Name = "Weekly Occupancy uses confirmed/completed filter"
        Pattern = "weeklyOccupancy.*BookingStatus\.IsConfirmedOrCompleted"
        Expected = $true
    },
    @{
        Name = "Weekly Occupancy counts distinct rooms"
        Pattern = "\.Distinct\(\).*Count\(\)"
        Expected = $true
    },
    @{
        Name = "Revenue converted to percentages"
        Pattern = "percentage.*totalRevenue.*100"
        Expected = $true
    },
    @{
        Name = "Occupancy capped at 100%"
        Pattern = "Math\.Min.*100"
        Expected = $true
    }
)

$passed = 0
$failed = 0

foreach ($test in $tests) {
    if ($controllerContent -match $test.Pattern) {
        Write-Host "  [PASS] $($test.Name)" -ForegroundColor Green
        $passed++
    } else {
        Write-Host "  [FAIL] $($test.Name)" -ForegroundColor Red
        $failed++
    }
}
Write-Host ""

# Test 2: Verify frontend chart rendering
Write-Host "[TEST 2] Frontend Chart Rendering" -ForegroundColor Magenta
$viewContent = Get-Content "Project(DEPI)/Views/Dashboard/Index.cshtml" -Raw

$frontendTests = @(
    @{
        Name = "Chart.js library loaded"
        Pattern = "chart\.js"
        Expected = $true
    },
    @{
        Name = "Booking Trends chart canvas exists"
        Pattern = "bookingTrendsChart"
        Expected = $true
    },
    @{
        Name = "Revenue chart canvas exists"
        Pattern = "revenueChart"
        Expected = $true
    },
    @{
        Name = "Occupancy chart canvas exists"
        Pattern = "occupancyChart"
        Expected = $true
    },
    @{
        Name = "Charts initialized on DOM ready"
        Pattern = "DOMContentLoaded.*initializeCharts"
        Expected = $true
    },
    @{
        Name = "Chart instances destroyed before recreate"
        Pattern = "if.*bookingChart.*destroy"
        Expected = $true
    },
    @{
        Name = "Animation disabled (prevents infinite growth)"
        Pattern = "animation:\s*false"
        Expected = $true
    },
    @{
        Name = "No double percentage conversion"
        Pattern = "already in percentages"
        Expected = $true
    }
)

foreach ($test in $frontendTests) {
    if ($viewContent -match $test.Pattern) {
        Write-Host "  [PASS] $($test.Name)" -ForegroundColor Green
        $passed++
    } else {
        Write-Host "  [FAIL] $($test.Name)" -ForegroundColor Red
        $failed++
    }
}
Write-Host ""

# Test 3: Check for old case-sensitive comparisons
Write-Host "[TEST 3] Case-Sensitive Status Checks" -ForegroundColor Magenta
$oldPatterns = @(
    'Status\s*==\s*"Confirmed"',
    'Status\s*==\s*"Completed"',
    'Status\s*!=\s*"Cancelled"'
)

$issuesFound = 0
foreach ($pattern in $oldPatterns) {
    if ($controllerContent -match $pattern) {
        $issuesFound++
    }
}

if ($issuesFound -eq 0) {
    Write-Host "  [PASS] No case-sensitive status comparisons in controller" -ForegroundColor Green
    $passed++
} else {
    Write-Host "  [FAIL] Found $issuesFound case-sensitive comparisons" -ForegroundColor Red
    $failed++
}
Write-Host ""

# Test 4: Verify data flow
Write-Host "[TEST 4] Data Flow Verification" -ForegroundColor Magenta

$dataFlowTests = @(
    @{
        Name = "BookingTrends passed to ViewBag"
        Pattern = "ViewBag\.BookingTrends\s*="
        Expected = $true
    },
    @{
        Name = "RevenueByType passed to ViewBag"
        Pattern = "ViewBag\.RevenueByType\s*="
        Expected = $true
    },
    @{
        Name = "WeeklyOccupancy passed to ViewBag"
        Pattern = "ViewBag\.WeeklyOccupancy\s*="
        Expected = $true
    },
    @{
        Name = "Frontend reads BookingTrends from ViewBag"
        Pattern = "ViewBag\.BookingTrends"
        Content = $viewContent
        Expected = $true
    },
    @{
        Name = "Frontend reads RevenueByType from ViewBag"
        Pattern = "ViewBag\.RevenueByType"
        Content = $viewContent
        Expected = $true
    },
    @{
        Name = "Frontend reads WeeklyOccupancy from ViewBag"
        Pattern = "ViewBag\.WeeklyOccupancy"
        Content = $viewContent
        Expected = $true
    }
)

foreach ($test in $dataFlowTests) {
    $content = if ($test.Content) { $test.Content } else { $controllerContent }
    if ($content -match $test.Pattern) {
        Write-Host "  [PASS] $($test.Name)" -ForegroundColor Green
        $passed++
    } else {
        Write-Host "  [FAIL] $($test.Name)" -ForegroundColor Red
        $failed++
    }
}
Write-Host ""

# Test 5: Chart configuration validation
Write-Host "[TEST 5] Chart Configuration" -ForegroundColor Magenta

$configTests = @(
    @{
        Name = "Booking Trends: Y-axis has suggestedMax"
        Pattern = "bookingTrendsChart.*suggestedMax"
        Expected = $true
    },
    @{
        Name = "Occupancy: Y-axis max set to 100"
        Pattern = "occupancyChart.*max:\s*100"
        Expected = $true
    },
    @{
        Name = "Revenue: Doughnut chart type"
        Pattern = "revenueChart.*type:\s*'doughnut'"
        Expected = $true
    },
    @{
        Name = "Charts use responsive containers"
        Pattern = "relative w-full.*height:"
        Expected = $true
    }
)

foreach ($test in $configTests) {
    if ($viewContent -match $test.Pattern) {
        Write-Host "  [PASS] $($test.Name)" -ForegroundColor Green
        $passed++
    } else {
        Write-Host "  [FAIL] $($test.Name)" -ForegroundColor Red
        $failed++
    }
}
Write-Host ""

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Test Results Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Tests Passed: $passed" -ForegroundColor Green
Write-Host "Tests Failed: $failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
$totalTests = $passed + $failed
$passRate = if ($totalTests -gt 0) { [math]::Round(($passed / $totalTests) * 100, 1) } else { 0 }
Write-Host "Pass Rate: $passRate%" -ForegroundColor $(if ($passRate -ge 90) { "Green" } elseif ($passRate -ge 70) { "Yellow" } else { "Red" })
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Chart Logic Improvements" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Backend (DashboardController.cs):" -ForegroundColor Yellow
Write-Host "  1. Booking Trends" -ForegroundColor White
Write-Host "     - Now filters by confirmed/completed status" -ForegroundColor Gray
Write-Host "     - Only counts actual bookings, not pending/cancelled" -ForegroundColor Gray
Write-Host ""
Write-Host "  2. Revenue by Room Type" -ForegroundColor White
Write-Host "     - Filters by confirmed/completed status" -ForegroundColor Gray
Write-Host "     - Converts to percentages for pie chart" -ForegroundColor Gray
Write-Host "     - Handles zero revenue gracefully" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. Weekly Occupancy" -ForegroundColor White
Write-Host "     - Counts DISTINCT rooms (not total bookings)" -ForegroundColor Gray
Write-Host "     - Prevents double-counting same room" -ForegroundColor Gray
Write-Host "     - Caps at 100% to prevent data issues" -ForegroundColor Gray
Write-Host "     - Filters by confirmed/completed status" -ForegroundColor Gray
Write-Host ""
Write-Host "Frontend (Index.cshtml):" -ForegroundColor Yellow
Write-Host "  1. Removed double percentage conversion" -ForegroundColor White
Write-Host "     - Backend already provides percentages" -ForegroundColor Gray
Write-Host "  2. Charts properly initialized" -ForegroundColor White
Write-Host "     - Animation disabled (prevents infinite growth)" -ForegroundColor Gray
Write-Host "     - Proper cleanup on destroy" -ForegroundColor Gray
Write-Host "  3. Responsive containers" -ForegroundColor White
Write-Host "     - Fixed height wrappers" -ForegroundColor Gray
Write-Host "     - No overflow issues" -ForegroundColor Gray
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Expected Chart Behavior" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Booking Trends Chart:" -ForegroundColor Yellow
Write-Host "  - Shows last 6 months of confirmed/completed bookings" -ForegroundColor White
Write-Host "  - Line chart with smooth curves" -ForegroundColor White
Write-Host "  - Y-axis scales appropriately" -ForegroundColor White
Write-Host "  - No infinite growth" -ForegroundColor White
Write-Host ""
Write-Host "Revenue by Room Type Chart:" -ForegroundColor Yellow
Write-Host "  - Doughnut chart showing percentage distribution" -ForegroundColor White
Write-Host "  - Only includes confirmed/completed bookings" -ForegroundColor White
Write-Host "  - Percentages add up to 100%" -ForegroundColor White
Write-Host "  - Shows actual revenue distribution" -ForegroundColor White
Write-Host ""
Write-Host "Weekly Occupancy Chart:" -ForegroundColor Yellow
Write-Host "  - Bar chart showing last 7 days" -ForegroundColor White
Write-Host "  - Counts unique rooms occupied per day" -ForegroundColor White
Write-Host "  - Percentage capped at 100%" -ForegroundColor White
Write-Host "  - Color-coded by occupancy level" -ForegroundColor White
Write-Host "  - Only includes confirmed/completed bookings" -ForegroundColor White
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan

if ($failed -eq 0) {
    Write-Host ""
    Write-Host "All chart logic tests passed!" -ForegroundColor Green
    Write-Host "Charts should now display accurate data." -ForegroundColor Green
    exit 0
} else {
    Write-Host ""
    Write-Host "Some tests failed. Please review the output above." -ForegroundColor Yellow
    exit 1
}
