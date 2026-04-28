# BookifyHotel - Monster ASP.NET Advanced Deployment Script (PowerShell)
# Run with: ./deploy-monster.ps1

param(
    [string]$Environment = "Production",
    [string]$MonsterFtpHost = "",
    [string]$MonsterFtpUser = "",
    [string]$MonsterFtpPass = "",
    [switch]$SkipBuild = $false,
    [switch]$SkipPublish = $false,
    [switch]$OpenExplorer = $true
)

$ErrorActionPreference = "Stop"

Write-Host "`n===============================================" -ForegroundColor Cyan
Write-Host "  BookifyHotel - Monster ASP.NET Publishing" -ForegroundColor Cyan
Write-Host "===============================================`n" -ForegroundColor Cyan

# Function to run a command and check for errors
function Run-Command {
    param(
        [string]$Command,
        [string]$Description
    )
    
    Write-Host "[*] $Description..." -ForegroundColor Yellow
    Write-Host "    Command: $Command" -ForegroundColor Gray
    
    Invoke-Expression $Command
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[?] FAILED: $Description" -ForegroundColor Red
        Write-Host "    Exit Code: $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "[?] $Description successful" -ForegroundColor Green
}

# Step 1: Clean
if (-not $SkipBuild) {
    Run-Command "dotnet clean" "Cleaning previous build"
    Write-Host ""
}

# Step 2: Restore
if (-not $SkipBuild) {
    Run-Command "dotnet restore" "Restoring NuGet packages"
    Write-Host ""
}

# Step 3: Build
if (-not $SkipBuild) {
    Run-Command "dotnet build -c Release" "Building in Release mode"
    Write-Host ""
}

# Step 4: Publish
if (-not $SkipPublish) {
    Run-Command "dotnet publish -c Release -o ./publish" "Publishing to ./publish folder"
    Write-Host ""
}

# Verify publish folder exists
if (-not (Test-Path "./publish")) {
    Write-Host "[?] ERROR: Publish folder not found!" -ForegroundColor Red
    exit 1
}

$PublishPath = Resolve-Path "./publish"
$PublishSize = (Get-ChildItem -Path $PublishPath -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  Deployment Ready!" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Published Location: $PublishPath" -ForegroundColor Yellow
Write-Host "Published Size: $([math]::Round($PublishSize, 2)) MB" -ForegroundColor Yellow
Write-Host ""

# FTP Upload Option
if ($MonsterFtpHost -and $MonsterFtpUser -and $MonsterFtpPass) {
    Write-Host "[*] Uploading to Monster ASP.NET via FTP..." -ForegroundColor Yellow
    
    # Create WebClient for FTP
    $FtpAddress = "ftp://$($MonsterFtpHost)/"
    $Credential = New-Object System.Net.NetworkCredential($MonsterFtpUser, $MonsterFtpPass)
    
    Get-ChildItem -Path $PublishPath -Recurse -File | ForEach-Object {
        $RelativePath = $_.FullName.Substring($PublishPath.Length + 1)
        $FtpFile = "$FtpAddress$RelativePath".Replace('\', '/')
        
        Write-Host "    Uploading: $($_.Name)" -ForegroundColor Gray
        
        try {
            $FtpRequest = [System.Net.FtpWebRequest]::Create($FtpFile)
            $FtpRequest.Credentials = $Credential
            $FtpRequest.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
            $FtpRequest.UseBinary = $true
            
            $FileStream = $_.OpenRead()
            $FtpStream = $FtpRequest.GetRequestStream()
            $FileStream.CopyTo($FtpStream)
            $FtpStream.Close()
            $FileStream.Close()
        }
        catch {
            Write-Host "    [!] Failed to upload: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
    
    Write-Host "[?] FTP upload complete" -ForegroundColor Green
}

# Next Steps
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Upload ./publish contents to Monster ASP.NET FTP" -ForegroundColor Gray
Write-Host "   OR push Git: git push monster main" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Verify deployment:" -ForegroundColor Gray
Write-Host "   - Visit: https://yourdomain.com" -ForegroundColor Gray
Write-Host "   - Check Monster Control Panel" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Test features:" -ForegroundColor Gray
Write-Host "   - Login page" -ForegroundColor Gray
Write-Host "   - Admin dashboard" -ForegroundColor Gray
Write-Host "   - Room booking" -ForegroundColor Gray
Write-Host "   - Payment processing" -ForegroundColor Gray
Write-Host ""

# Open Explorer if requested
if ($OpenExplorer) {
    Write-Host "[*] Opening publish folder in Explorer..." -ForegroundColor Yellow
    explorer $PublishPath
}

Write-Host "===============================================" -ForegroundColor Green
Write-Host ""
