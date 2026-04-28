# BookifyHotel - Publishing & Deployment Guide

## ?? Pre-Publication Checklist

### 1. **Configuration & Secrets**
- [ ] Set up Azure Key Vault or similar secret manager
- [ ] Configure production connection string (use environment variable)
- [ ] Set Stripe live API keys (use environment variable)
- [ ] Change default admin password to secure value
- [ ] Update admin email for production environment
- [ ] Set `AllowedHosts` to your production domain

### 2. **Database**
- [ ] Run Entity Framework migrations on production database
- [ ] Verify database backups are configured
- [ ] Test connection string before deployment

### 3. **Security**
- [ ] Enable HTTPS only (already configured in Program.cs)
- [ ] Verify HSTS is enabled (already configured in Program.cs)
- [ ] Review CORS settings if calling from different domains
- [ ] Set strong password policy (already configured in Program.cs)
- [ ] Enable rate limiting on login (already configured in Program.cs)

### 4. **Application Settings**
- [ ] Set logging level to Warning in production
- [ ] Disable developer exception page in production (already configured in Program.cs)
- [ ] Verify anti-forgery token settings
- [ ] Test session timeout (30 minutes configured)

---

## ?? Environment Variables Setup

Set these environment variables in your production environment:

```bash
# Database Connection
ConnectionStrings__DefaultConnection=Server=your-server;Database=BookifyHotelDB;User Id=sa;Password=YourStrongPassword;Encrypt=True;

# Stripe Keys (Production Live Keys)
Stripe__SecretKey=sk_live_xxxxxxxxxxxxxxxxxxxx
Stripe__PublishableKey=pk_live_xxxxxxxxxxxxxxxxxxxx
Stripe__WebhookSecret=whsec_xxxxxxxxxxxxxxxxxxxx

# Default Admin Credentials
DefaultUsers__Admin__Email=admin@yourdomain.com
DefaultUsers__Admin__Password=YourStrongAdminPassword123!
DefaultUsers__Admin__PhoneNumber=+1234567890

# Optional Demo User (disable in production by removing)
DefaultUsers__User__Email=demo@yourdomain.com
DefaultUsers__User__Password=DemoUserPassword123!
DefaultUsers__User__PhoneNumber=+9876543210

# ASPNETCORE Environment
ASPNETCORE_ENVIRONMENT=Production
```

---

## ?? Security Best Practices Already Implemented

? **Strong Password Policy**
- Minimum 8 characters
- Requires: 1 digit, 1 uppercase, 1 lowercase, 1 special character
- 4 unique characters required

? **Account Security**
- Account lockout after 5 failed attempts (15 minutes)
- Unique email requirement to prevent account enumeration
- Session timeout: 60 minutes with sliding expiration

? **Cookie Security**
- HttpOnly cookies (prevent JavaScript access)
- Secure flag enabled (HTTPS only)
- SameSite=Strict (prevent CSRF)

? **Request Protection**
- Rate limiting on login endpoint (10 attempts/minute)
- Global anti-forgery token validation
- CORS and security headers configured

? **Data Protection**
- ASP.NET Core Data Protection API enabled
- Password hashing via PBKDF2-HMACSHA256
- Sensitive data encrypted in transit (HTTPS)

---

## ?? Publishing Steps

### Option 1: Azure App Service Deployment

```bash
# 1. Install Azure CLI
# https://docs.microsoft.com/en-us/cli/azure/install-azure-cli

# 2. Login to Azure
az login

# 3. Create Resource Group
az group create --name BookifyHotelRG --location eastus

# 4. Create App Service Plan
az appservice plan create --name BookifyHotelPlan --resource-group BookifyHotelRG --sku B2

# 5. Create Web App
az webapp create --resource-group BookifyHotelRG --plan BookifyHotelPlan --name bookify-hotel-app

# 6. Publish from Visual Studio
# Right-click project ? Publish ? Azure ? Select created app

# 7. Set environment variables in Azure Portal
# Configuration ? Application settings
```

### Option 2: Docker Container Deployment

**Create Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src
COPY ["Project(DEPI)/HotelEcomm.csproj", "Project(DEPI)/"]
COPY ["DAL/DAL.csproj", "DAL/"]
COPY ["PLL/PLL.csproj", "PLL/"]
RUN dotnet restore "Project(DEPI)/HotelEcomm.csproj"

COPY . .
WORKDIR "/src/Project(DEPI)"
RUN dotnet build "HotelEcomm.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HotelEcomm.csproj" -c Release -o /app/publish

FROM runtime AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HotelEcomm.dll"]
```

**Build and run:**
```bash
docker build -t bookify-hotel:latest .
docker run -d -p 80:80 -e ASPNETCORE_ENVIRONMENT=Production bookify-hotel:latest
```

### Option 3: Direct IIS Deployment

1. **Publish from Visual Studio:**
   - Right-click project ? Publish
   - Choose "Folder" target
   - Create folder: `C:\Deploy\BookifyHotel`

2. **On IIS Server:**
   - Install .NET 9.0 Runtime
   - Create IIS Application Pool (.NET CLR version: No Managed Code)
   - Create Website pointing to publish folder
   - Set environment variables in Application Pool identity settings

---

## ?? Post-Deployment Verification

```bash
# 1. Test HTTPS
curl -I https://yourdomain.com

# 2. Verify HSTS Header
curl -I https://yourdomain.com | grep Strict-Transport-Security

# 3. Test Login with Rate Limiting
# Try logging in with wrong password 6 times - should get 429 on 6th

# 4. Verify Database Connection
# Check application logs in Azure/IIS

# 5. Test Payment Integration
# Use Stripe test cards (requires Stripe test keys during testing)

# 6. Verify Email Configuration
# If sending emails, test user notifications
```

---

## ?? Troubleshooting

### Issue: "Database connection failed"
**Solution:**
```bash
# Verify connection string is set correctly
# Check SQL Server firewall rules
# Verify application has database permissions
# Review logs: ~/LogFiles/Application/
```

### Issue: "Stripe API key not configured"
**Solution:**
- Add Stripe test keys to appsettings.json for development
- Add live keys as environment variables for production

### Issue: "HTTP 500 Error in Production"
**Solution:**
- Set `ASPNETCORE_ENVIRONMENT=Development` temporarily to see details
- Check application logs in Azure App Service
- Verify all environment variables are set
- Run migrations against production database

### Issue: "Session expires too quickly"
**Solution:**
- Current: 60-minute idle timeout (configured in Program.cs)
- To change: Modify `options.ExpireTimeSpan = TimeSpan.FromMinutes(60);`

---

## ?? Monitoring & Maintenance

### Enable Application Insights (Azure)
```bash
# Add to Program.cs after builder creation:
builder.Services.AddApplicationInsightsTelemetry();
```

### Monitor Key Metrics
- Application uptime
- Response times
- Failed requests
- Database connection pool
- Memory usage

### Regular Tasks
- [ ] Monitor logs daily
- [ ] Update NuGet packages monthly
- [ ] Review security advisories
- [ ] Backup database daily
- [ ] Test disaster recovery monthly

---

## ?? Support & References

- **Microsoft Docs**: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/
- **Stripe Documentation**: https://stripe.com/docs
- **Entity Framework**: https://learn.microsoft.com/en-us/ef/core/
- **ASP.NET Core Security**: https://learn.microsoft.com/en-us/aspnet/core/security/

---

**Last Updated**: 2024
**Application**: BookifyHotel
**Framework**: ASP.NET Core 9.0 (.NET 9)
**Database**: SQL Server
**Payment Provider**: Stripe
