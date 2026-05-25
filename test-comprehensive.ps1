# Comprehensive Endpoint Testing
$baseUrl = "http://localhost:5280"

Write-Host "`nBOOKIFY HOTEL - COMPREHENSIVE ENDPOINT TEST" -ForegroundColor Magenta
Write-Host "============================================`n" -ForegroundColor Magenta

function Test-Endpoint {
    param($Name, $Url, $ExpectedStatus = 200)
    
    try {
        $response = Invoke-WebRequest -Uri "$baseUrl$Url" -UseBasicParsing -TimeoutSec 5 -MaximumRedirection 0 -ErrorAction Stop
        $status = $response.StatusCode
        if ($status -eq $ExpectedStatus) {
            Write-Host "[PASS] $Name - Status: $status" -ForegroundColor Green
            return $true
        } else {
            Write-Host "[WARN] $Name - Status: $status (Expected: $ExpectedStatus)" -ForegroundColor Yellow
            return $false
        }
    }
    catch {
        $status = if ($_.Exception.Response) { $_.Exception.Response.StatusCode.value__ } else { "ERROR" }
        if ($status -eq $ExpectedStatus) {
            Write-Host "[PASS] $Name - Status: $status (Expected redirect/auth)" -ForegroundColor Green
            return $true
        } else {
            Write-Host "[FAIL] $Name - Status: $status" -ForegroundColor Red
            return $false
        }
    }
}

$passed = 0
$total = 0

# PUBLIC PAGES
Write-Host "PUBLIC PAGES:" -ForegroundColor Cyan
$total++; if (Test-Endpoint "Homepage" "/") { $passed++ }
$total++; if (Test-Endpoint "About Page" "/About") { $passed++ }
$total++; if (Test-Endpoint "Contact Page" "/Contact") { $passed++ }
$total++; if (Test-Endpoint "Room Listing" "/Room") { $passed++ }
$total++; if (Test-Endpoint "Room Search" "/Room?location=Manchester") { $passed++ }
$total++; if (Test-Endpoint "Login Page" "/Login/Login") { $passed++ }

# HEALTH & API
Write-Host "`nHEALTH & API ENDPOINTS:" -ForegroundColor Cyan
$total++; if (Test-Endpoint "Health Check" "/health") { $passed++ }

# STATIC RESOURCES
Write-Host "`nSTATIC RESOURCES:" -ForegroundColor Cyan
$total++; if (Test-Endpoint "Design System CSS" "/css/design-system.css") { $passed++ }
$total++; if (Test-Endpoint "Glass CSS" "/css/glass.css") { $passed++ }
$total++; if (Test-Endpoint "Modern Animations CSS" "/css/modern-animations.css") { $passed++ }
$total++; if (Test-Endpoint "Theme Switcher JS" "/js/theme-switcher.js") { $passed++ }
$total++; if (Test-Endpoint "Toast JS" "/js/toast.js") { $passed++ }

# PROTECTED PAGES (Should redirect to login - 302)
Write-Host "`nPROTECTED PAGES (Should redirect):" -ForegroundColor Cyan
$total++; if (Test-Endpoint "Profile" "/Profile" 302) { $passed++ }
$total++; if (Test-Endpoint "My Bookings" "/Booking/MyBookings" 302) { $passed++ }
$total++; if (Test-Endpoint "Favorites" "/Favorite" 302) { $passed++ }
$total++; if (Test-Endpoint "Dashboard" "/dashboard" 302) { $passed++ }

# ROOM DETAILS (Should work)
Write-Host "`nROOM OPERATIONS:" -ForegroundColor Cyan
$total++; if (Test-Endpoint "Room Details" "/Room/Details/1") { $passed++ }

Write-Host "`n============================================" -ForegroundColor Magenta
Write-Host "RESULTS: $passed/$total tests passed" -ForegroundColor White
$percentage = [math]::Round(($passed / $total) * 100, 2)
Write-Host "Success Rate: $percentage%" -ForegroundColor $(if ($percentage -ge 90) { "Green" } elseif ($percentage -ge 70) { "Yellow" } else { "Red" })
Write-Host "============================================`n" -ForegroundColor Magenta
