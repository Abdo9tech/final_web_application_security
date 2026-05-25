# Simple Endpoint Testing
$baseUrl = "http://localhost:5280"

Write-Host "Testing Bookify Hotel Endpoints..." -ForegroundColor Cyan
Write-Host "Base URL: $baseUrl`n" -ForegroundColor Gray

$tests = @(
    @{ Name = "Homepage"; Url = "/" }
    @{ Name = "About"; Url = "/About" }
    @{ Name = "Contact"; Url = "/Contact" }
    @{ Name = "Rooms"; Url = "/Room" }
    @{ Name = "Login"; Url = "/Login/Login" }
    @{ Name = "Health"; Url = "/health" }
    @{ Name = "Design CSS"; Url = "/css/design-system.css" }
    @{ Name = "Theme JS"; Url = "/js/theme-switcher.js" }
    @{ Name = "Toast JS"; Url = "/js/toast.js" }
)

$passed = 0
$failed = 0

foreach ($test in $tests) {
    $url = $baseUrl + $test.Url
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 5
        Write-Host "[PASS] $($test.Name) - Status: $($response.StatusCode)" -ForegroundColor Green
        $passed++
    }
    catch {
        Write-Host "[FAIL] $($test.Name) - Error: $($_.Exception.Message)" -ForegroundColor Red
        $failed++
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Results: $passed passed, $failed failed" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Cyan
