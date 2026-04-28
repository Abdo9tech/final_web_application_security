# ?? Command Reference Guide

Quick reference for common deployment and development commands.

## ??? Build & Development

### Build Application
```bash
# Restore dependencies and build
dotnet build

# Build in Release configuration
dotnet build -c Release

# Build with verbose output
dotnet build -v d
```

### Run Application
```bash
# Run in debug mode (default)
dotnet run

# Run in specific project
cd Project(DEPI)
dotnet run

# Run with custom port
dotnet run --urls "http://localhost:5001"

# Run with production settings
ASPNETCORE_ENVIRONMENT=Production dotnet run
```

### Test Application
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test --project Tests/

# Run with verbose output
dotnet test -v d

# Run with coverage
dotnet test /p:CollectCoverage=true
```

---

## ??? Database Management

### Entity Framework Commands
```bash
# Add new migration
dotnet ef migrations add MigrationName --project DAL --startup-project Project(DEPI)

# Remove last migration
dotnet ef migrations remove --project DAL --startup-project Project(DEPI)

# List all migrations
dotnet ef migrations list --project DAL --startup-project Project(DEPI)

# Update database to latest migration
dotnet ef database update --project DAL --startup-project Project(DEPI)

# Update to specific migration
dotnet ef database update MigrationName --project DAL --startup-project Project(DEPI)

# Generate migration script (don't apply)
dotnet ef migrations script --project DAL --startup-project Project(DEPI) -o migration.sql

# Generate script between migrations
dotnet ef migrations script FromMigration ToMigration --project DAL --startup-project Project(DEPI)

# Drop database
dotnet ef database drop --project DAL --startup-project Project(DEPI)
```

---

## ?? Docker Commands

### Build Docker Image
```bash
# Build image with tag
docker build -t bookify-hotel:latest .

# Build with build args
docker build -t bookify-hotel:v1.0 \
  --build-arg ASPNETCORE_ENVIRONMENT=Production .

# Build from specific Dockerfile
docker build -f Project(DEPI)/Dockerfile -t bookify-hotel:latest .
```

### Run Docker Container
```bash
# Run container interactively
docker run -it bookify-hotel:latest

# Run with port mapping
docker run -d -p 80:80 -p 443:443 bookify-hotel:latest

# Run with environment variables
docker run -d -p 80:80 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection=... \
  bookify-hotel:latest

# Run with volume mount
docker run -d -p 80:80 \
  -v C:\data:/app/data \
  bookify-hotel:latest

# View container logs
docker logs container_name

# Tail logs (follow)
docker logs -f container_name

# Stop container
docker stop container_name

# Remove container
docker rm container_name
```

### Docker Compose Commands
```bash
# Start services
docker-compose up

# Start in background
docker-compose up -d

# Start specific service
docker-compose up app

# Build and start
docker-compose up --build

# View logs
docker-compose logs

# Follow logs
docker-compose logs -f

# View specific service logs
docker-compose logs app

# Stop services
docker-compose down

# Stop and remove volumes
docker-compose down -v

# Restart services
docker-compose restart

# Execute command in container
docker-compose exec app bash

# Scale service
docker-compose up -d --scale app=3
```

---

## ?? Azure CLI Commands

### Login & Setup
```bash
# Login to Azure
az login

# Set subscription
az account set --subscription "subscription-id"

# Create resource group
az group create --name BookifyRG --location eastus

# List resource groups
az group list --output table
```

### App Service Management
```bash
# Create App Service Plan
az appservice plan create --name BookifyPlan \
  --resource-group BookifyRG \
  --sku B2 \
  --is-linux

# Create Web App
az webapp create --resource-group BookifyRG \
  --plan BookifyPlan \
  --name bookify-hotel-app \
  --runtime "DOTNET|9.0"

# List web apps
az webapp list --resource-group BookifyRG

# Stop app
az webapp stop --resource-group BookifyRG --name bookify-hotel-app

# Start app
az webapp start --resource-group BookifyRG --name bookify-hotel-app

# Restart app
az webapp restart --resource-group BookifyRG --name bookify-hotel-app

# Delete app
az webapp delete --resource-group BookifyRG --name bookify-hotel-app
```

### Configuration Management
```bash
# Set application settings
az webapp config appsettings set --resource-group BookifyRG \
  --name bookify-hotel-app \
  --settings ASPNETCORE_ENVIRONMENT=Production \
              Stripe__SecretKey=sk_live_... \
              DefaultUsers__Admin__Email=admin@bookify.com

# Show application settings
az webapp config appsettings list --resource-group BookifyRG \
  --name bookify-hotel-app

# Set connection strings
az webapp config connection-string set --resource-group BookifyRG \
  --name bookify-hotel-app \
  --settings DefaultConnection="Server=..." \
  --connection-string-type SQLServer

# Show connection strings
az webapp config connection-string list --resource-group BookifyRG \
  --name bookify-hotel-app
```

### SQL Server Management
```bash
# Create SQL Server
az sql server create --resource-group BookifyRG \
  --name bookifydbserver \
  --admin-user sqladmin \
  --admin-password "P@ssw0rd123!"

# Create database
az sql db create --resource-group BookifyRG \
  --server bookifydbserver \
  --name BookifyHotelDB \
  --edition Standard

# Configure firewall
az sql server firewall-rule create --resource-group BookifyRG \
  --server bookifydbserver \
  --name AllowAzureIps \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Show database status
az sql db show --resource-group BookifyRG \
  --server bookifydbserver \
  --name BookifyHotelDB
```

---

## ?? SSL/Certificate Management

### Generate Self-Signed Certificate (Local Only)
```bash
# Linux/Mac
openssl req -new -x509 -newkey rsa:2048 -keyout key.pem -out cert.pem -days 365

# Windows (PowerShell)
New-SelfSignedCertificate -CertStoreLocation cert:\LocalMachine\My -DnsName "localhost"
```

### Trust Certificate (Development Only)
```bash
# .NET development certificate
dotnet dev-certs https --trust

# Check if trusted
dotnet dev-certs https --check --trust
```

---

## ?? Secret Management

### Using .NET User Secrets (Development)
```bash
# Initialize user secrets
dotnet user-secrets init

# Set secret
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."

# Set from file
dotnet user-secrets set --input secrets.json

# List secrets
dotnet user-secrets list

# Clear all secrets
dotnet user-secrets clear

# Remove specific secret
dotnet user-secrets remove "SecretName"
```

### Using Environment Variables
```bash
# Windows (PowerShell)
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:Stripe__SecretKey = "sk_live_..."

# Linux/Mac
export ASPNETCORE_ENVIRONMENT=Production
export Stripe__SecretKey=sk_live_...

# View environment variable
echo $Stripe__SecretKey
```

---

## ?? Publishing & Deployment

### Publish to Folder
```bash
# Publish in Release configuration
dotnet publish -c Release -o .\publish

# Publish for specific runtime
dotnet publish -c Release -r win-x64 -o .\publish

# Publish as self-contained
dotnet publish -c Release -r win-x64 --self-contained
```

### Deploy to Azure (Visual Studio)
```bash
# Right-click project ? Publish
# Select Azure App Service
# Select subscription and app
# Click Publish

# From command line:
az webapp up --resource-group BookifyRG --name bookify-hotel-app
```

---

## ?? Monitoring & Debugging

### View Application Logs
```bash
# Real-time logs (Azure)
az webapp log tail --resource-group BookifyRG --name bookify-hotel-app

# Download logs (Azure)
az webapp log download --resource-group BookifyRG --name bookify-hotel-app

# Docker container logs
docker logs -f container_id

# Show last 100 lines
docker logs --tail 100 container_id

# Show logs from last 10 minutes
docker logs --since 10m container_id
```

### SQL Server Connection Test
```bash
# sqlcmd (Windows)
sqlcmd -S your-server -U sa -P your-password

# mssql-cli (cross-platform)
mssql-cli -S your-server -U sa -P your-password

# Test query
SELECT @@version;
```

### Port Diagnostics
```bash
# Check if port is in use (Windows)
netstat -ano | findstr :80

# Check if port is in use (Linux/Mac)
lsof -i :80

# Kill process using port (Windows)
taskkill /PID process_id /F

# Kill process using port (Linux/Mac)
kill -9 process_id
```

---

## ?? Cleanup Commands

### Remove Build Artifacts
```bash
# Clean build output
dotnet clean

# Remove all bin/obj directories
Get-ChildItem -Include bin,obj -Recurse | Remove-Item -Recurse

# Linux/Mac
find . -type d -name bin -o -name obj | xargs rm -rf
```

### Remove Docker Resources
```bash
# Remove unused images
docker image prune

# Remove unused containers
docker container prune

# Remove unused networks
docker network prune

# Remove everything unused
docker system prune

# Force remove (no confirmation)
docker system prune -f
```

---

## ?? Troubleshooting Commands

### Check .NET Installation
```bash
# List installed SDK versions
dotnet --list-sdks

# List installed runtime versions
dotnet --list-runtimes

# Show .NET info
dotnet --info
```

### Verify Application Settings
```bash
# Show appsettings
cat appsettings.json

# Show production appsettings
cat appsettings.Production.json

# Show environment variables (Windows PowerShell)
Get-Item env:

# Show specific env var
echo $env:Stripe__SecretKey
```

### Network Diagnostics
```bash
# Test SQL Server connectivity
tcping your-sql-server 1433

# Test HTTPS endpoint
curl -I https://yourdomain.com

# Test with verbose output
curl -v https://yourdomain.com

# Check DNS resolution
nslookup yourdomain.com
```

---

## ?? Performance & Monitoring

### Database Queries
```bash
# Check entity tracking
context.ChangeTracker.DebugView

# Count tracked entities
context.ChangeTracker.Entries().Count()

# Show SQL query (EF Core)
var query = context.Users.ToQueryString();
```

### Memory Usage
```bash
# Windows (PowerShell)
Get-Process dotnet | Select-Object Name,@{Name="Memory(MB)";Expression={[math]::Round($_.WorkingSet/1MB)}}

# Linux
ps aux | grep dotnet
```

---

## ?? Common Workflows

### Complete Local Development Setup
```bash
git clone https://github.com/Abdo9tech/final_web_application_security.git
cd myFinalPro
dotnet restore
cd Project(DEPI)
dotnet user-secrets init
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet run
# Open https://localhost:5001
```

### Deploy to Docker
```bash
docker build -t bookify-hotel:latest .
docker run -d -p 80:80 -e ASPNETCORE_ENVIRONMENT=Production bookify-hotel:latest
# Access at http://localhost
```

### Deploy to Azure
```bash
az login
az group create --name BookifyRG --location eastus
az appservice plan create --name BookifyPlan --resource-group BookifyRG --sku B2
az webapp create --resource-group BookifyRG --plan BookifyPlan --name bookify-hotel-app
# Configure settings in portal
# Publish from Visual Studio
```

---

## ?? Additional Resources

- [.NET CLI Reference](https://learn.microsoft.com/en-us/dotnet/core/tools/)
- [Azure CLI Reference](https://learn.microsoft.com/en-us/cli/azure/reference-index)
- [Docker CLI Reference](https://docs.docker.com/engine/reference/commandline/cli/)
- [Entity Framework Core CLI](https://learn.microsoft.com/en-us/ef/core/cli/)

---

**Quick Reference Card Created**: 2024  
**Framework**: .NET 9 with ASP.NET Core  
**Print This for Easy Access**: Yes ?
