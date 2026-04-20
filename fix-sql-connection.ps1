# SQL Server Connection Fix Script
# Run this as Administrator

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SQL Server Connection Fix" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "⚠ Warning: Not running as Administrator" -ForegroundColor Yellow
    Write-Host "  Some fixes may require admin privileges" -ForegroundColor Yellow
    Write-Host ""
}

# Step 1: Check SQL Server services
Write-Host "[1/5] Checking SQL Server services..." -ForegroundColor Green
$sqlServices = Get-Service | Where-Object {$_.Name -like "*SQL*" -and $_.Name -notlike "*Agent*"}

if ($sqlServices) {
    Write-Host "  Found SQL Server services:" -ForegroundColor Gray
    $sqlServices | ForEach-Object {
        $status = if ($_.Status -eq "Running") { "✓" } else { "✗" }
        $color = if ($_.Status -eq "Running") { "Green" } else { "Red" }
        Write-Host "    $status $($_.DisplayName) - $($_.Status)" -ForegroundColor $color
    }
} else {
    Write-Host "  ✗ No SQL Server services found" -ForegroundColor Red
}

Write-Host ""

# Step 2: Try to start SQL Server Express
Write-Host "[2/5] Attempting to start SQL Server Express..." -ForegroundColor Green
try {
    $service = Get-Service -Name "MSSQL`$SQLEXPRESS" -ErrorAction Stop
    if ($service.Status -ne "Running") {
        Start-Service -Name "MSSQL`$SQLEXPRESS" -ErrorAction Stop
        Write-Host "  ✓ SQL Server Express started successfully!" -ForegroundColor Green
    } else {
        Write-Host "  ✓ SQL Server Express is already running" -ForegroundColor Green
    }
} catch {
    Write-Host "  ✗ Could not start SQL Server Express" -ForegroundColor Red
    Write-Host "    Reason: $_" -ForegroundColor Gray
}

Write-Host ""

# Step 3: Try to start SQL Server Browser
Write-Host "[3/5] Attempting to start SQL Server Browser..." -ForegroundColor Green
try {
    $browser = Get-Service -Name "SQLBrowser" -ErrorAction Stop
    if ($browser.Status -ne "Running") {
        Start-Service -Name "SQLBrowser" -ErrorAction Stop
        Write-Host "  ✓ SQL Server Browser started successfully!" -ForegroundColor Green
    } else {
        Write-Host "  ✓ SQL Server Browser is already running" -ForegroundColor Green
    }
} catch {
    Write-Host "  ⚠ SQL Server Browser not available (optional)" -ForegroundColor Yellow
}

Write-Host ""

# Step 4: Test connection
Write-Host "[4/5] Testing SQL Server connection..." -ForegroundColor Green

# Test SQLEXPRESS
Write-Host "  Testing .\SQLEXPRESS..." -ForegroundColor Gray
$testExpress = sqlcmd -S ".\SQLEXPRESS" -E -Q "SELECT @@VERSION" 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✓ Connection to .\SQLEXPRESS successful!" -ForegroundColor Green
    $workingConnection = ".\SQLEXPRESS"
} else {
    Write-Host "  ✗ Connection to .\SQLEXPRESS failed" -ForegroundColor Red
    
    # Test LocalDB
    Write-Host "  Testing (localdb)\mssqllocaldb..." -ForegroundColor Gray
    $testLocalDB = sqlcmd -S "(localdb)\mssqllocaldb" -E -Q "SELECT @@VERSION" 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ Connection to LocalDB successful!" -ForegroundColor Green
        $workingConnection = "(localdb)\mssqllocaldb"
    } else {
        Write-Host "  ✗ Connection to LocalDB failed" -ForegroundColor Red
        $workingConnection = $null
    }
}

Write-Host ""

# Step 5: Provide recommendations
Write-Host "[5/5] Recommendations" -ForegroundColor Green
Write-Host ""

if ($workingConnection) {
    Write-Host "✓ Found working connection: $workingConnection" -ForegroundColor Green
    Write-Host ""
    Write-Host "Update your appsettings.json with:" -ForegroundColor Yellow
    Write-Host ""
    if ($workingConnection -eq ".\SQLEXPRESS") {
        Write-Host '"ConnectionStrings": {' -ForegroundColor White
        Write-Host '  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=BookifyHotelDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"' -ForegroundColor White
        Write-Host '}' -ForegroundColor White
    } else {
        Write-Host '"ConnectionStrings": {' -ForegroundColor White
        Write-Host '  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BookifyHotelDB;Trusted_Connection=True;MultipleActiveResultSets=true"' -ForegroundColor White
        Write-Host '}' -ForegroundColor White
    }
    Write-Host ""
    Write-Host "Then run:" -ForegroundColor Yellow
    Write-Host "  dotnet ef database update --project DAL --startup-project Project(DEPI)" -ForegroundColor White
} else {
    Write-Host "✗ No working SQL Server connection found" -ForegroundColor Red
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "1. Install SQL Server Express:" -ForegroundColor White
    Write-Host "   https://www.microsoft.com/sql-server/sql-server-downloads" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "2. Use SQLite (easiest for development):" -ForegroundColor White
    Write-Host "   a. Install package:" -ForegroundColor Gray
    Write-Host "      dotnet add Project(DEPI) package Microsoft.EntityFrameworkCore.Sqlite" -ForegroundColor Cyan
    Write-Host "   b. Update appsettings.json:" -ForegroundColor Gray
    Write-Host '      "DefaultConnection": "Data Source=BookifyHotel.db"' -ForegroundColor Cyan
    Write-Host "   c. Update Program.cs to use UseSqlite instead of UseSqlServer" -ForegroundColor Gray
    Write-Host ""
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Diagnostic Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Offer to update appsettings.json
if ($workingConnection) {
    $update = Read-Host "Would you like to update appsettings.json automatically? (Y/N)"
    if ($update -eq "Y" -or $update -eq "y") {
        $appsettingsPath = "Project(DEPI)/appsettings.json"
        if (Test-Path $appsettingsPath) {
            try {
                $content = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
                
                if ($workingConnection -eq ".\SQLEXPRESS") {
                    $content.ConnectionStrings.DefaultConnection = "Server=.\\SQLEXPRESS;Database=BookifyHotelDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
                } else {
                    $content.ConnectionStrings.DefaultConnection = "Server=(localdb)\\mssqllocaldb;Database=BookifyHotelDB;Trusted_Connection=True;MultipleActiveResultSets=true"
                }
                
                $content | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath
                Write-Host ""
                Write-Host "✓ appsettings.json updated successfully!" -ForegroundColor Green
                Write-Host ""
                Write-Host "Next steps:" -ForegroundColor Yellow
                Write-Host "  1. Run migrations: dotnet ef database update --project DAL --startup-project Project(DEPI)" -ForegroundColor White
                Write-Host "  2. Run application: dotnet run --project Project(DEPI)" -ForegroundColor White
                Write-Host ""
            } catch {
                Write-Host ""
                Write-Host "✗ Failed to update appsettings.json: $_" -ForegroundColor Red
                Write-Host "  Please update manually" -ForegroundColor Yellow
                Write-Host ""
            }
        } else {
            Write-Host ""
            Write-Host "✗ appsettings.json not found at: $appsettingsPath" -ForegroundColor Red
            Write-Host ""
        }
    }
}
