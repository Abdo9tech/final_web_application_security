# PowerShell Script to Rename Project(DEPI) to HotelCommerce
# Run this script from the project root directory

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Rename Project to HotelCommerce" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running from correct directory
if (-not (Test-Path "Project(DEPI).sln")) {
    Write-Host "ERROR: Project(DEPI).sln not found!" -ForegroundColor Red
    Write-Host "Please run this script from the project root directory." -ForegroundColor Yellow
    exit 1
}

# Confirm with user
Write-Host "This script will:" -ForegroundColor Yellow
Write-Host "  1. Create a backup" -ForegroundColor White
Write-Host "  2. Rename solution file to HotelCommerce.sln" -ForegroundColor White
Write-Host "  3. Rename Project(DEPI) folder to HotelCommerce" -ForegroundColor White
Write-Host "  4. Update all references and namespaces" -ForegroundColor White
Write-Host ""
$confirm = Read-Host "Continue? (Y/N)"
if ($confirm -ne "Y" -and $confirm -ne "y") {
    Write-Host "Operation cancelled." -ForegroundColor Yellow
    exit 0
}

Write-Host ""

# Step 1: Create backup
Write-Host "[1/8] Creating backup..." -ForegroundColor Green
$backupPath = "../ProjectBackup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
try {
    Copy-Item -Path "." -Destination $backupPath -Recurse -Force -Exclude @("bin", "obj", ".vs", "node_modules")
    Write-Host "  ✓ Backup created at: $backupPath" -ForegroundColor Gray
} catch {
    Write-Host "  ✗ Backup failed: $_" -ForegroundColor Red
    exit 1
}

# Step 2: Rename solution file
Write-Host "[2/8] Renaming solution file..." -ForegroundColor Green
try {
    Rename-Item "Project(DEPI).sln" "HotelCommerce.sln" -ErrorAction Stop
    Write-Host "  ✓ Solution file renamed" -ForegroundColor Gray
} catch {
    Write-Host "  ✗ Failed to rename solution file: $_" -ForegroundColor Red
    exit 1
}

# Step 3: Update solution file content
Write-Host "[3/8] Updating solution file content..." -ForegroundColor Green
try {
    $solutionContent = Get-Content "HotelCommerce.sln" -Raw
    $solutionContent = $solutionContent -replace 'Project\(DEPI\)', 'HotelCommerce'
    $solutionContent | Set-Content "HotelCommerce.sln" -NoNewline
    Write-Host "  ✓ Solution file updated" -ForegroundColor Gray
} catch {
    Write-Host "  ✗ Failed to update solution file: $_" -ForegroundColor Red
}

# Step 4: Rename project folder
Write-Host "[4/8] Renaming project folder..." -ForegroundColor Green
try {
    Rename-Item "Project(DEPI)" "HotelCommerce" -ErrorAction Stop
    Write-Host "  ✓ Project folder renamed" -ForegroundColor Gray
} catch {
    Write-Host "  ✗ Failed to rename project folder: $_" -ForegroundColor Red
    exit 1
}

# Step 5: Rename project file
Write-Host "[5/8] Renaming project file..." -ForegroundColor Green
try {
    Rename-Item "HotelCommerce/Project(DEPI).csproj" "HotelCommerce.csproj" -ErrorAction Stop
    Write-Host "  ✓ Project file renamed" -ForegroundColor Gray
} catch {
    Write-Host "  ✗ Failed to rename project file: $_" -ForegroundColor Red
    exit 1
}

# Step 6: Update namespaces in C# files
Write-Host "[6/8] Updating namespaces in C# files..." -ForegroundColor Green
$csFiles = Get-ChildItem -Path "HotelCommerce" -Filter "*.cs" -Recurse -Exclude @("bin", "obj")
$updatedCount = 0
foreach ($file in $csFiles) {
    try {
        $content = Get-Content $file.FullName -Raw
        $originalContent = $content
        
        # Update namespaces
        $content = $content -replace 'namespace Project_DEPI_', 'namespace HotelCommerce'
        $content = $content -replace 'using Project_DEPI_', 'using HotelCommerce'
        
        if ($content -ne $originalContent) {
            $content | Set-Content $file.FullName -NoNewline
            $updatedCount++
        }
    } catch {
        Write-Host "  ⚠ Warning: Could not update $($file.Name)" -ForegroundColor Yellow
    }
}
Write-Host "  ✓ Updated $updatedCount C# files" -ForegroundColor Gray

# Step 7: Update launchSettings.json
Write-Host "[7/8] Updating launch settings..." -ForegroundColor Green
$launchSettings = "HotelCommerce/Properties/launchSettings.json"
if (Test-Path $launchSettings) {
    try {
        $content = Get-Content $launchSettings -Raw
        $content = $content -replace 'Project\(DEPI\)', 'HotelCommerce'
        $content | Set-Content $launchSettings -NoNewline
        Write-Host "  ✓ Launch settings updated" -ForegroundColor Gray
    } catch {
        Write-Host "  ⚠ Warning: Could not update launch settings" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ⚠ Launch settings not found (optional)" -ForegroundColor Yellow
}

# Step 8: Update appsettings.json (optional - update app name)
Write-Host "[8/8] Updating appsettings.json..." -ForegroundColor Green
$appsettings = "HotelCommerce/appsettings.json"
if (Test-Path $appsettings) {
    try {
        $content = Get-Content $appsettings -Raw
        # You can add any app name updates here if needed
        Write-Host "  ✓ Appsettings checked" -ForegroundColor Gray
    } catch {
        Write-Host "  ⚠ Warning: Could not update appsettings" -ForegroundColor Yellow
    }
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Renaming Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Open HotelCommerce.sln in Visual Studio" -ForegroundColor White
Write-Host "  2. Build the solution: dotnet build HotelCommerce.sln" -ForegroundColor White
Write-Host "  3. Run the application: dotnet run --project HotelCommerce" -ForegroundColor White
Write-Host ""
Write-Host "Backup location: $backupPath" -ForegroundColor Gray
Write-Host ""

# Offer to build
$build = Read-Host "Would you like to build the solution now? (Y/N)"
if ($build -eq "Y" -or $build -eq "y") {
    Write-Host ""
    Write-Host "Building solution..." -ForegroundColor Green
    dotnet build HotelCommerce.sln
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "✓ Build successful!" -ForegroundColor Green
        Write-Host ""
    } else {
        Write-Host ""
        Write-Host "✗ Build failed. Please check the errors above." -ForegroundColor Red
        Write-Host ""
    }
}

Write-Host "Done! 🎉" -ForegroundColor Cyan
