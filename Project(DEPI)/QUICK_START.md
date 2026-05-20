# BookifyHotel - Quick Start & Publishing Guide

## 🎯 Quick Summary

Your BookifyHotel application is built with:
- **.NET 9** with ASP.NET Core Razor Pages
- **SQL Server** for database
- **Stripe** for payments
- **Entity Framework Core** for ORM
- **ASP.NET Core Identity** for authentication

## ✅ Current Status

**Build**: ✅ Successful  
**Security**: ✅ Hardened (HTTPS, CSRF protection, rate limiting, strong passwords)  
**Ready to Publish**: ✅ Yes

---

## 🚀 Quick Deploy Options

### **Option A: Docker (Recommended for Most Deployments)**

```bash
# 1. Clone .env from example
cp Project(DEPI)/.env.example .env

# 2. Edit .env with your values
nano .env

# 3. Start application with SQL Server
docker-compose up -d

# Application runs on http://localhost
# Access at: http://localhost
```

### **Option B: Azure App Service (Recommended for Cloud)**

1. **Sign in to Azure Portal** → https://portal.azure.com
2. **Create Resource** → App Services → Web App
3. **Configuration:**
   - Runtime: .NET 9
   - Operating System: Windows or Linux
   - Region: Choose closest to users
4. **Create → Go to Resource**
5. **Download Publish Profile** (under Overview → Get Publish Profile)
6. **In Visual Studio:**
   - Right-click `HotelEcomm` project
   - Click **Publish**
   - Select **Import Profile** → Choose downloaded profile
   - Click **Publish**
7. **Set Environment Variables** in Azure:
   - Go to Configuration → Application Settings
   - Add all values from `.env.example`
   - Click Save

### **Option C: Local IIS (Windows Only)**

1. **Install Prerequisites:**
   ```bash
   # Install .NET 9.0 Runtime
   # Download from: https://dotnet.microsoft.com/download/dotnet/9.0
   ```

2. **Publish Locally:**
   - Right-click project → Publish
   - Choose Folder target
   - Set path: `C:\Deploy\BookifyHotel`

3. **Setup IIS:**
   - Open IIS Manager
   - Create Application Pool: `.NET CLR version: No Managed Code`
   - Create Website pointing to `C:\Deploy\BookifyHotel`
   - Set Application Pool to your created pool

4. **Configure Environment Variables:**
   - Set in Advanced Settings of Application Pool

---

## 📋 Required Configuration (Before Deploy)

### **1. Database Connection String**
```
Server=your-sql-server;Database=BookifyHotelDB;User Id=sa;Password=YourPassword;Encrypt=True;
```

### **2. Stripe Keys**
- Get from: https://dashboard.stripe.com/apikeys
- Live Keys for Production:
  - `Stripe__SecretKey=sk_live_...`
  - `Stripe__PublishableKey=pk_live_...`
  - `Stripe__WebhookSecret=whsec_...`

### **3. Admin Account**
- `DefaultUsers__Admin__Email=admin@yourdomain.com`
- `DefaultUsers__Admin__Password=StrongPassword123!@#`
- `DefaultUsers__Admin__PhoneNumber=+1234567890`

### **4. Domain (Production)**
In `appsettings.Production.json`:
```json
"AllowedHosts": "yourdomain.com;www.yourdomain.com"
```

---

## 🔐 Security Checklist Before Publishing

- [ ] Set strong admin password
- [ ] Configure Stripe live keys (not test keys)
- [ ] Update AllowedHosts to your domain
- [ ] Enable HTTPS on hosting provider
- [ ] Set ASPNETCORE_ENVIRONMENT=Production
- [ ] Disable demo user or change credentials
- [ ] Setup database backups
- [ ] Configure SSL/TLS certificate
- [ ] Enable Web Application Firewall (if available)
- [ ] Setup monitoring/logging (Application Insights)

---

## 🧪 Test Before Publishing

### **Local Testing**
```bash
# 1. Build locally
dotnet build

# 2. Run locally with production settings
ASPNETCORE_ENVIRONMENT=Production dotnet run

# 3. Test features:
# - Login page
# - Payment processing (use Stripe test cards)
# - Room booking
# - Admin dashboard
```

### **Stripe Test Cards**
Use these cards when `ASPNETCORE_ENVIRONMENT=Development`:
```
4242 4242 4242 4242  - Success
4000 0000 0000 0002  - Card Declined
```

---

## 📊 Post-Deployment Checklist

After deploying to production:

- [ ] Verify HTTPS works
- [ ] Test login with correct credentials
- [ ] Test login rate limiting (wrong password 6+ times)
- [ ] Verify payment integration with Stripe test payment
- [ ] Check database migrations ran successfully
- [ ] Monitor logs for errors
- [ ] Test user registration flow
- [ ] Verify email notifications (if configured)
- [ ] Test session timeout (60 minutes of inactivity)

---

## 🐛 Troubleshooting

### **"Connection string is empty"**
- Set `ConnectionStrings__DefaultConnection` environment variable
- Or update `appsettings.json` for development

### **"Stripe key not configured"**
- Payment features work in dev with test keys
- Set live keys as environment variables for production

### **"Database migration failed"**
```bash
# Run migrations manually:
dotnet ef database update --project DAL --startup-project Project(DEPI)
```

### **"Port already in use"**
Docker:
```bash
# Use different port
docker-compose -e PORT=8080 up -d
```

---

## 📖 Full Documentation

See `DEPLOYMENT_GUIDE.md` for detailed information about:
- Azure App Service deployment
- Docker deployment with examples
- Environment variables setup
- Monitoring and maintenance
- Security best practices
- Troubleshooting guide

---

## 💡 Pro Tips

1. **Enable Application Insights** for monitoring
2. **Setup continuous deployment** via GitHub Actions
3. **Use secrets manager** for sensitive data (Azure Key Vault)
4. **Configure auto-scaling** if expecting high traffic
5. **Setup CDN** for static files (JavaScript, CSS, images)
6. **Enable SSL/TLS certificate** (free with Let's Encrypt)
7. **Setup log aggregation** (Application Insights, Serilog)

---

## 🆘 Need Help?

1. Check `DEPLOYMENT_GUIDE.md`
2. Review application logs in deployment environment
3. Check SQL Server connectivity
4. Verify environment variables are set
5. Test with `ASPNETCORE_ENVIRONMENT=Development` to see error details

---

**Application**: BookifyHotel v1.0  
**Framework**: ASP.NET Core 9.0  
**Status**: Ready for Production  
**Last Updated**: 2024


