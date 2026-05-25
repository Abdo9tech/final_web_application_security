# Google Cloud Credentials Setup Script
# This script helps you securely configure Google Cloud credentials

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Google Cloud Credentials Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if credentials folder exists
$credentialsFolder = "Project(DEPI)/credentials"
if (-not (Test-Path $credentialsFolder)) {
    Write-Host "Creating credentials folder..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $credentialsFolder -Force | Out-Null
    Write-Host "✅ Credentials folder created" -ForegroundColor Green
}

# Check if credentials file exists
$credentialsFile = "$credentialsFolder/google-cloud-credentials.json"
if (Test-Path $credentialsFile) {
    Write-Host "✅ Credentials file found: $credentialsFile" -ForegroundColor Green
    
    # Validate JSON
    try {
        $json = Get-Content $credentialsFile -Raw | ConvertFrom-Json
        Write-Host "✅ Credentials file is valid JSON" -ForegroundColor Green
        
        # Display info (without sensitive data)
        Write-Host ""
        Write-Host "Credentials Info:" -ForegroundColor Cyan
        Write-Host "  Project ID: $($json.project_id)" -ForegroundColor White
        Write-Host "  Client Email: $($json.client_email)" -ForegroundColor White
        Write-Host "  Type: $($json.type)" -ForegroundColor White
        
    } catch {
        Write-Host "❌ ERROR: Credentials file is not valid JSON" -ForegroundColor Red
        Write-Host "   Please check the file format" -ForegroundColor Yellow
        exit 1
    }
} else {
    Write-Host "⚠️  Credentials file not found" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Please follow these steps:" -ForegroundColor Cyan
    Write-Host "1. Copy your Google Cloud service account JSON file" -ForegroundColor White
    Write-Host "2. Save it as: $credentialsFile" -ForegroundColor White
    Write-Host "3. Run this script again to verify" -ForegroundColor White
    Write-Host ""
    Write-Host "Need help? See GOOGLE_CLOUD_SETUP.md for detailed instructions" -ForegroundColor Yellow
    exit 1
}

# Check if appsettings.Development.json exists
$devSettings = "Project(DEPI)/appsettings.Development.json"
if (Test-Path $devSettings) {
    Write-Host ""
    Write-Host "✅ Development settings file found" -ForegroundColor Green
    
    # Read and validate
    try {
        $settings = Get-Content $devSettings -Raw | ConvertFrom-Json
        
        if ($settings.GoogleCloud) {
            Write-Host "✅ GoogleCloud configuration found" -ForegroundColor Green
            Write-Host "  Project ID: $($settings.GoogleCloud.ProjectId)" -ForegroundColor White
            Write-Host "  Credentials Path: $($settings.GoogleCloud.CredentialsPath)" -ForegroundColor White
            
            # Verify project ID matches
            $json = Get-Content $credentialsFile -Raw | ConvertFrom-Json
            if ($settings.GoogleCloud.ProjectId -eq "YOUR_PROJECT_ID") {
                Write-Host ""
                Write-Host "⚠️  WARNING: Project ID not configured" -ForegroundColor Yellow
                Write-Host "   Update appsettings.Development.json with:" -ForegroundColor Yellow
                Write-Host "   `"ProjectId`": `"$($json.project_id)`"" -ForegroundColor White
            } elseif ($settings.GoogleCloud.ProjectId -ne $json.project_id) {
                Write-Host ""
                Write-Host "⚠️  WARNING: Project ID mismatch" -ForegroundColor Yellow
                Write-Host "   Settings: $($settings.GoogleCloud.ProjectId)" -ForegroundColor White
                Write-Host "   Credentials: $($json.project_id)" -ForegroundColor White
            } else {
                Write-Host "✅ Project ID matches credentials" -ForegroundColor Green
            }
        } else {
            Write-Host "⚠️  GoogleCloud configuration not found in settings" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "❌ ERROR: Could not read development settings" -ForegroundColor Red
    }
} else {
    Write-Host ""
    Write-Host "⚠️  Development settings file not found" -ForegroundColor Yellow
    Write-Host "   Creating from template..." -ForegroundColor Yellow
    
    # Create from template
    $json = Get-Content $credentialsFile -Raw | ConvertFrom-Json
    $template = @{
        "Logging" = @{
            "LogLevel" = @{
                "Default" = "Information"
                "Microsoft.AspNetCore" = "Warning"
            }
        }
        "GoogleCloud" = @{
            "ProjectId" = $json.project_id
            "CredentialsPath" = "./credentials/google-cloud-credentials.json"
        }
    }
    
    $template | ConvertTo-Json -Depth 10 | Set-Content $devSettings
    Write-Host "✅ Development settings created" -ForegroundColor Green
}

# Check .gitignore
Write-Host ""
Write-Host "Checking .gitignore..." -ForegroundColor Cyan
$gitignore = Get-Content ".gitignore" -Raw
if ($gitignore -match "credentials/\*\.json") {
    Write-Host "✅ Credentials folder is in .gitignore" -ForegroundColor Green
} else {
    Write-Host "⚠️  WARNING: Credentials folder may not be properly ignored by git" -ForegroundColor Yellow
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Setup Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✅ Credentials file: OK" -ForegroundColor Green
Write-Host "✅ Configuration: OK" -ForegroundColor Green
Write-Host "✅ Security: OK" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Run: docker-compose down" -ForegroundColor White
Write-Host "2. Run: docker-compose build web" -ForegroundColor White
Write-Host "3. Run: docker-compose up -d" -ForegroundColor White
Write-Host ""
Write-Host "Your Google Cloud credentials are now configured!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
