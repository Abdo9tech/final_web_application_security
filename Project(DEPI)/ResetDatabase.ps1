# Reset Database Script
Write-Host "Stopping any running processes..." -ForegroundColor Yellow
Get-Process -Name "Project(DEPI)" -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "Deleting database files..." -ForegroundColor Yellow
$dbPath = "C:\Users\$env:USERNAME\BookifyHotelDB.mdf"
$logPath = "C:\Users\$env:USERNAME\BookifyHotelDB_log.ldf"

if (Test-Path $dbPath) {
    Remove-Item $dbPath -Force
    Write-Host "Deleted: $dbPath" -ForegroundColor Green
}

if (Test-Path $logPath) {
    Remove-Item $logPath -Force
    Write-Host "Deleted: $logPath" -ForegroundColor Green
}

Write-Host "Database reset complete. Run 'dotnet run' to recreate the database." -ForegroundColor Green
