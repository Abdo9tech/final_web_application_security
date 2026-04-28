@echo off
REM BookifyHotel - Monster ASP.NET Deployment Script for Windows
REM This script automates the publishing process for Monster ASP.NET deployment

echo.
echo ===============================================
echo  BookifyHotel - Monster ASP.NET Publishing
echo ===============================================
echo.

REM Step 1: Clean previous build
echo [1/4] Cleaning previous build...
dotnet clean
if %errorlevel% neq 0 (
    echo ERROR: Failed to clean project
    pause
    exit /b %errorlevel%
)
echo [?] Clean successful

REM Step 2: Restore dependencies
echo.
echo [2/4] Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore packages
    pause
    exit /b %errorlevel%
)
echo [?] Restore successful

REM Step 3: Build in Release mode
echo.
echo [3/4] Building application in Release mode...
dotnet build -c Release
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    pause
    exit /b %errorlevel%
)
echo [?] Build successful

REM Step 4: Publish
echo.
echo [4/4] Publishing to ./publish folder...
dotnet publish -c Release -o ./publish
if %errorlevel% neq 0 (
    echo ERROR: Publish failed
    pause
    exit /b %errorlevel%
)
echo [?] Publish successful

REM Summary
echo.
echo ===============================================
echo  Deployment Complete!
echo ===============================================
echo.
echo Published files are in: ./publish
echo.
echo Next steps:
echo 1. Upload ./publish folder contents to Monster ASP.NET FTP
echo    OR push Git changes: git push monster main
echo.
echo 2. Verify deployment at: https://yourdomain.com
echo.
echo 3. Check Monster Control Panel for:
echo    - Application Status
echo    - Recent Logs
echo    - Database Connection
echo.
echo ===============================================
echo.

REM Optional: Open Explorer to show publish folder
explorer "./publish"

pause
