# ?? MONSTER ASP.NET DEPLOYMENT - COMPLETE GUIDE

## ?? Welcome!

You have chosen **Monster ASP.NET** for hosting your **BookifyHotel** application. This guide walks you through the complete deployment process step-by-step.

---

## ?? What is Monster ASP.NET?

**Monster ASP.NET** is a managed hosting provider offering:
- ? Dedicated support for .NET applications
- ? .NET 9.0 runtime support
- ? SQL Server database hosting
- ? Easy-to-use control panel
- ? FTP/SFTP access
- ? Git deployment support
- ? SSL/TLS certificates
- ? Email services
- ? Automatic backups
- ? Professional support

---

## ?? Estimated Timeline

| Step | Time | Difficulty |
|------|------|-----------|
| 1. Prepare application | 10 min | ? Easy |
| 2. Setup Monster account | 15 min | ? Easy |
| 3. Create database | 10 min | ? Easy |
| 4. Configure settings | 10 min | ? Easy |
| 5. Publish/Upload | 15 min | ? Easy |
| 6. Verify deployment | 15 min | ? Easy |
| **TOTAL** | **75 min** | **Easy** |

---

## ?? Step 1: Prepare Your Application (10 minutes)

### 1.1 Ensure Build Success

```bash
# Verify application builds without errors
dotnet build -c Release

# Should complete with: "Build succeeded"
```

? **Success**: No errors or warnings

---

### 1.2 Publish Application

```bash
# Clean previous builds
dotnet clean

# Restore NuGet packages
dotnet restore

# Publish in Release mode
dotnet publish -c Release -o ./publish

# Should complete with: "Publish succeeded"
```

? **Success**: `./publish` folder created with ~200-300 MB of files

---

### 1.3 Verify Published Files

Check that `./publish` contains:
- ? `HotelEcomm.dll` (main application)
- ? `appsettings.json` (default settings)
- ? `appsettings.Production.json` (production settings)
- ? `Views/` folder (Razor pages)
- ? `wwwroot/` folder (CSS, JavaScript, images)
- ? `bin/` folder (dependencies)

? **Success**: All files present

---

## ?? Step 2: Setup Monster ASP.NET Account (15 minutes)

### 2.1 Create/Verify Account

1. Visit: https://www.monsterasp.net/
2. Sign up or log in
3. Choose hosting plan with:
   - ? .NET 9.0 support
   - ? SQL Server database
   - ? FTP/SFTP access
   - ? Sufficient disk space (at least 2 GB)
   - ? Sufficient monthly bandwidth (at least 50 GB)

? **Success**: Hosting plan activated

---

### 2.2 Get Your Credentials

From Monster Control Panel, obtain:

```
FTP Host:       ftp.yourdomain.com (or provided)
FTP Username:   your-ftp-username
FTP Password:   your-ftp-password
FTP Port:       21 (standard) or 22 (SFTP)

Database Host:  db.monsterasp.net (or similar)
Database Name:  BookifyHotelDB (or assigned)
Database User:  (create in next step)
Database Pass:  (create in next step)

Domain:         yourdomain.com
Path:           /httpdocs/ (typical, verify with Monster)
```

? **Success**: Credentials obtained and saved

---

## ??? Step 3: Create Database (10 minutes)

### 3.1 Create SQL Server Database

**Via Monster Control Panel**:

1. Log in to Control Panel
2. Go to: **Databases** ? **Create New Database**
3. Configure:
   ```
   Database Name:  BookifyHotelDB
   Collation:      SQL_Latin1_General_CP1_CI_AS
   ```
4. Click: **Create**

? **Success**: Database created

---

### 3.2 Create Database User

**Via Monster Control Panel**:

1. Go to: **Database Users** ? **Create New User**
2. Configure:
   ```
   Username:       dbuser
   Password:       StrongPassword123!@#
   Database:       BookifyHotelDB
   Permissions:    Full Access
   ```
3. Click: **Create**

? **Success**: Database user created with full permissions

---

### 3.3 Get Connection String

**Connection String Format**:
```
Server=db.monsterasp.net;Database=BookifyHotelDB;User Id=dbuser;Password=StrongPassword123!@#;Encrypt=True;TrustServerCertificate=False;
```

Or check Monster Control Panel for exact connection string.

? **Success**: Connection string obtained

---

## ?? Step 4: Configure Application Settings (10 minutes)

### 4.1 Edit appsettings.Production.json

```bash
# Edit file (or use your text editor)
notepad Project(DEPI)\appsettings.Production.json
```

### 4.2 Update Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Error"
    }
  },
  "AllowedHosts": "yourdomain.com;www.yourdomain.com",
  "ConnectionStrings": {
    "DefaultConnection": "Server=db.monsterasp.net;Database=BookifyHotelDB;User Id=dbuser;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=False;"
  },
  "DefaultUsers": {
    "Admin": {
      "Email": "admin@yourdomain.com",
      "Password": "StrongAdminPassword123!@#",
      "PhoneNumber": "+1234567890"
    },
    "User": {
      "Email": "demo@yourdomain.com",
      "Password": "DemoUserPassword123!@#",
      "PhoneNumber": "+0987654321"
    }
  },
  "Stripe": {
    "PublishableKey": "pk_live_xxxxxxxxxxxxxxxxxxxx",
    "SecretKey": "sk_live_xxxxxxxxxxxxxxxxxxxx",
    "WebhookSecret": "whsec_xxxxxxxxxxxxxxxxxxxx"
  }
}
```

### 4.3 Update Key Settings

| Setting | Value | Source |
|---------|-------|--------|
| **ConnectionString** | Your Monster database connection string | Monster Control Panel |
| **AllowedHosts** | Your domain | Your domain registrar |
| **Admin Email** | Your admin email | Create new |
| **Admin Password** | Strong password (8+ chars, complex) | Create new |
| **Stripe Keys** | Your Stripe live keys | Stripe Dashboard |

? **Success**: appsettings.Production.json configured correctly

---

## ?? Step 5: Deploy to Monster ASP.NET (15 minutes)

### Choose Your Deployment Method

## ?? Method A: FTP Deployment (Easiest)

### 5.1 Install FTP Client

Download one of:
- **FileZilla** (Free): https://filezilla-project.org/
- **WinSCP** (Free): https://winscp.net/
- **VS Code Extension**: Install "FTP Simple" or similar

### 5.2 Connect to Monster FTP

**FileZilla Steps**:

1. **Open FileZilla**
2. **File** ? **Site Manager** ? **New Site**
3. **Configure**:
   ```
   Protocol:       FTP
   Host:           ftp.yourdomain.com
   Port:           21
   Username:       your-ftp-username
   Password:       your-ftp-password
   ```
4. **Connect** ? Should see remote files

### 5.3 Upload Files

1. **Local Panel** (left): Navigate to `./publish` folder
2. **Remote Panel** (right): Navigate to `/httpdocs/` (or Monster specified path)
3. **Select All** files in `./publish` folder
4. **Drag & Drop** to remote panel OR **Right-click** ? **Upload**
5. **Wait** for 100% completion (Green status)

? **Success**: All files uploaded

### 5.4 Verify Upload

Check remote FTP contains:
- ? `HotelEcomm.dll`
- ? `appsettings.Production.json`
- ? `Views/` folder
- ? `wwwroot/` folder
- ? Other dependencies

? **Success**: Files verified on server

---

## ?? Method B: Git Deployment (Recommended)

### 5.1 Add Monster Remote

```bash
# Add Monster as remote
git remote add monster https://yourdomain.git.monsterasp.net/repo.git

# Verify (should show monster remote)
git remote -v
```

### 5.2 Push to Monster

```bash
# Push to Monster
git push monster main

# Or if branch is master:
git push monster master
```

? **Success**: Monster automatically builds and deploys

### 5.3 Monitor Deployment

- Check Monster Control Panel
- Application builds automatically
- Application starts automatically
- Wait 2-3 minutes for deployment

? **Success**: Deployment complete

---

## ? Step 6: Verify Deployment (15 minutes)

### 6.1 Test Website Access

1. **Open browser**
2. **Visit**: https://yourdomain.com
3. **Expected**: Home page loads with HTTPS (green lock)

? **Success**: Website accessible

---

### 6.2 Test Key Features

```
? Home page loads
? Navigation works
? Hotel rooms display
? Search/filter works
? Contact form accessible
? About page accessible
```

? **Success**: Basic navigation works

---

### 6.3 Test Authentication

```
? Login page loads at /Login/Login
? Admin email: admin@yourdomain.com
? Admin password: (your configured password)
? Can log in successfully
? Session created and maintained
? Can access admin features
```

? **Success**: Authentication works

---

### 6.4 Test Core Features

```
? View rooms list
? View room details
? Book a room (if payment not required for test)
? Payment page loads
? Dashboard loads
? User profile works
```

? **Success**: Core features working

---

### 6.5 Test Database Connection

Expected behavior:
- ? Rooms load from database
- ? User login works (authenticates against database)
- ? Bookings save to database
- ? No "database connection" errors

Check logs:
1. Monster Control Panel ? Application Logs
2. Should show no connection errors
3. Should show successful startup messages

? **Success**: Database connected

---

### 6.6 Check Security

Open Browser Developer Tools (F12):

**Console Tab**:
- ? No JavaScript errors

**Network Tab**:
- ? All resources load successfully
- ? No 404 errors
- ? All requests over HTTPS

**Security/Certificate Tab**:
- ? Certificate valid
- ? Issued to your domain
- ? Not expired

? **Success**: Security configured correctly

---

## ?? Success! Application is Live!

When all above steps pass:

? Application deployed successfully  
? Website accessible  
? HTTPS working  
? Features functional  
? Database connected  
? Security in place  

**Your application is now LIVE!**

---

## ?? Post-Deployment Actions

### 1. Announce Launch
- Tell users the site is live
- Share domain name
- Share with team

### 2. Monitor First Week
- Check logs daily
- Verify no errors
- Test critical workflows
- Collect user feedback

### 3. Setup Monitoring
- Enable error alerting
- Setup performance monitoring
- Configure backups
- Plan maintenance window

### 4. Ongoing Maintenance
- Weekly log review
- Monthly security updates
- Database optimization
- SSL certificate monitoring

---

## ?? Troubleshooting

### Website shows "404 Not Found"

**Causes**:
- Files not uploaded to correct directory
- Application not started
- Wrong domain configured

**Solutions**:
1. Verify files in `/httpdocs/` via FTP
2. Check Monster Control Panel ? Application Status
3. Verify domain DNS points to Monster
4. Restart application in Monster Control Panel

---

### Website shows "500 Internal Server Error"

**Causes**:
- Database connection failed
- Missing configuration
- Application not starting

**Solutions**:
1. Check Monster logs
2. Verify connection string correct
3. Verify database exists and is accessible
4. Ensure .NET 9.0 runtime installed
5. Contact Monster support

---

### "Cannot connect to database"

**Causes**:
- Wrong connection string
- Database doesn't exist
- Database user doesn't have permissions
- Firewall blocking connection

**Solutions**:
1. Verify connection string format
2. Test connection (if SSH available):
   ```bash
   sqlcmd -S db.monsterasp.net -U dbuser -P password -d BookifyHotelDB -Q "SELECT @@VERSION;"
   ```
3. Verify database user permissions
4. Contact Monster support

---

### "HTTPS certificate error"

**Causes**:
- SSL certificate not installed
- Certificate for wrong domain
- Certificate expired

**Solutions**:
1. Check Monster Control Panel ? SSL Certificates
2. Verify certificate is for your domain
3. Install/renew certificate
4. Clear browser cache
5. Wait 24 hours for DNS to propagate

---

### "Stripe payment not working"

**Causes**:
- Using test keys instead of live keys
- Stripe account not setup
- Webhook not configured

**Solutions**:
1. Verify using LIVE keys (sk_live_ not sk_test_)
2. Check Stripe account is active
3. Verify webhook endpoint in Stripe dashboard
4. Check Stripe logs for errors

---

### "Application is slow"

**Causes**:
- Database queries not optimized
- Server resources limited
- Shared hosting resources consumed

**Solutions**:
1. Check Monster Control Panel ? Resource Usage
2. Optimize database queries
3. Consider upgrade to higher tier plan
4. Enable caching
5. Use CDN for static files

---

## ?? Getting Help

### From Monster ASP.NET Support

**Contact**:
- Website: https://www.monsterasp.net/
- Email: support@monsterasp.net
- Phone: (check website for number)

**Questions to Ask**:
1. "Does your hosting support .NET 9.0?"
2. "What is my FTP host address?"
3. "How do I view application logs?"
4. "How do I set environment variables?"
5. "How do I configure auto-start for applications?"

### From Your Development Team

- Attach error logs
- Include connection string (redact password)
- Describe what worked/what didn't
- Provide step-by-step reproduction

---

## ?? Additional Resources

| Resource | Link |
|----------|------|
| Monster ASP.NET | https://www.monsterasp.net/ |
| .NET 9 Docs | https://learn.microsoft.com/dotnet/ |
| ASP.NET Core | https://learn.microsoft.com/aspnet/core/ |
| Stripe Docs | https://stripe.com/docs |
| Entity Framework | https://learn.microsoft.com/ef/core/ |

---

## ?? Next Steps from Here

1. **If deployment successful**:
   - ? Announce launch to users
   - ? Monitor for first week
   - ? Collect feedback

2. **If deployment has issues**:
   - ? Check troubleshooting section above
   - ? Review Monster logs
   - ? Contact Monster support
   - ? Include relevant error messages

3. **For ongoing operation**:
   - ? Set up monitoring
   - ? Plan maintenance schedule
   - ? Backup important data
   - ? Stay updated with security patches

---

## ? Summary

**You have successfully deployed BookifyHotel to Monster ASP.NET!**

```
? Application Published
? Uploaded to Monster
? Database Connected
? Website Live
? Features Working
? Security Configured
? Ready for Users
```

---

**Congratulations on going live!** ??

**Need more help?**
- ?? See `MONSTER_ASPNET_QUICK_REFERENCE.md` for quick commands
- ?? See `MONSTER_ASPNET_CHECKLIST.md` for verification checklist
- ?? See `MONSTER_ASPNET_DEPLOYMENT.md` for detailed guide

---

**Monster ASP.NET Deployment - Complete Guide**  
**Version 1.0 | Last Updated: 2024**  
**BookifyHotel v1.0 | .NET 9.0 | Production Ready**
