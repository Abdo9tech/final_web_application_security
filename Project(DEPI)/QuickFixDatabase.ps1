# Quick Fix for Password Hash Error
# This script will reset the database and recreate users with proper password hashes

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  BookifyHotel - Database Quick Fix" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Stop running processes
Write-Host "[1/4] Stopping running application..." -ForegroundColor Yellow
try {
    Get-Process -Name "Project(DEPI)" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 2
    Write-Host "✓ Application stopped" -ForegroundColor Green
} catch {
    Write-Host "✓ No running processes found" -ForegroundColor Green
}
Write-Host ""

# Step 2: Drop the database
Write-Host "[2/4] Dropping existing database..." -ForegroundColor Yellow
try {
    $result = sqlcmd -S ".\SQLEXPRESS" -Q "DROP DATABASE IF EXISTS BookifyHotelDB" 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Database dropped successfully" -ForegroundColor Green
    } else {
        Write-Host "⚠ Database might not exist (this is OK)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠ Could not drop database (this is OK if it doesn't exist)" -ForegroundColor Yellow
}
Write-Host ""

# Step 3: Clean build artifacts
Write-Host "[3/4] Cleaning build artifacts..." -ForegroundColor Yellow
if (Test-Path "bin") {
    Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path "obj") {
    Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host "✓ Build artifacts cleaned" -ForegroundColor Green
Write-Host ""

# Step 4: Instructions
Write-Host "[4/4] Next Steps:" -ForegroundColor Yellow
Write-Host ""
Write-Host "Run this command to start the application:" -ForegroundColor White
Write-Host "  dotnet run" -ForegroundColor Cyan
Write-Host ""
Write-Host "The application will:" -ForegroundColor White
Write-Host "  • Create a fresh database" -ForegroundColor Gray
Write-Host "  • Generate proper password hashes" -ForegroundColor Gray
Write-Host "  • Display admin credentials" -ForegroundColor Gray
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Login Credentials (after restart)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Email:    admin@bookify.com" -ForegroundColor Green
Write-Host "Password: Admin@123" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press any key to start the application now, or Ctrl+C to exit..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

Write-Host ""
Write-Host "Starting application..." -ForegroundColor Cyan
Write-Host ""

# Start the application
dotnet run
