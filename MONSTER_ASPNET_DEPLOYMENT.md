# ?? BookifyHotel - Monster ASP.NET Deployment Guide

## ?? What is Monster ASP.NET?

Monster ASP.NET is a managed ASP.NET hosting provider that supports:
- ? .NET Framework and .NET Core/.NET 5+
- ? Automated deployments via FTP, SFTP, or Git
- ? SQL Server database hosting
- ? Custom domains and SSL/TLS certificates
- ? Control Panel for management
- ? Email services
- ? Automatic backups

---

## ?? Step 1: Prepare Your Application for Monster ASP.NET

### Build for Production

```bash
# Clean previous builds
dotnet clean

# Restore dependencies
dotnet restore

# Build in Release mode
dotnet build -c Release

# Publish for deployment
dotnet publish -c Release -o ./publish

# This creates a folder containing all files needed for deployment
```

### What Gets Published
The `./publish` folder will contain:
- ? Compiled DLLs and dependencies
- ? Content files (Views, static files, etc.)
- ? Configuration files
- ? Entity Framework migration assemblies
- ? Stripe and other NuGet packages

---

## ?? Step 2: Prepare Monster ASP.NET Account

### Prerequisites
1. **Active Monster ASP.NET account**
   - Sign up: https://www.monsterasp.net/

2. **Hosting plan with:**
   - ? .NET 9.0 Runtime support
   - ? SQL Server database
   - ? FTP/SFTP access
   - ? Git deployment support (optional but recommended)

### Access Control Panel
1. Log in to Monster ASP.NET Control Panel
2. Navigate to your account
3. Note down:
   - **FTP Host**: (provided by Monster)
   - **FTP Username**: (your account)
   - **FTP Password**: (your password)
   - **SQL Server**: (connection details)

---

## ?? Step 3: Create/Configure SQL Server Database

### Via Monster ASP.NET Control Panel

1. **Go to**: Databases ? Create New Database
2. **Configure**:
   - Database Name: `BookifyHotelDB`
   - Collation: `SQL_Latin1_General_CP1_CI_AS`
3. **Note the connection string**: 
   ```
   Server=your-monster-server.com;Database=BookifyHotelDB;User Id=sa;Password=YourPassword;Encrypt=True;
   ```

### Create Database User (if required)
1. Go to: Database Users
2. Create user with full permissions to `BookifyHotelDB`
3. Save credentials

---

## ?? Step 4: Configure Production Settings

### Update appsettings.Production.json

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
    "DefaultConnection": "Server=monster-server.monsterasp.net;Database=BookifyHotelDB;User Id=db_user;Password=db_password;Encrypt=True;TrustServerCertificate=False;"
  },
  "DefaultUsers": {
    "Admin": {
      "Email": "admin@yourdomain.com",
      "Password": "StrongPassword123!@#",
      "PhoneNumber": "+1234567890"
    }
  },
  "Stripe": {
    "PublishableKey": "pk_live_xxxxxxxxxxxxxxxxxxxx",
    "SecretKey": "sk_live_xxxxxxxxxxxxxxxxxxxx",
    "WebhookSecret": "whsec_xxxxxxxxxxxxxxxxxxxx"
  }
}
```

### Create .env.production file (Alternative - if using environment variables)
```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Server=monster-server.monsterasp.net;Database=BookifyHotelDB;...
Stripe__SecretKey=sk_live_...
Stripe__PublishableKey=pk_live_...
Stripe__WebhookSecret=whsec_...
DefaultUsers__Admin__Email=admin@yourdomain.com
DefaultUsers__Admin__Password=YourStrongPassword123!@#
```

---

## ?? Step 5: Deploy via FTP/SFTP (Method 1 - Manual)

### Download FTP Client
- **FileZilla**: https://filezilla-project.org/
- **WinSCP**: https://winscp.net/
- **VS Code FTP Extension**: Built-in or install extension

### Deploy Steps

1. **Connect to Monster ASP.NET FTP**
   - Host: `ftp.yourdomain.com` (or provided by Monster)
   - Username: Your FTP username
   - Password: Your FTP password
   - Port: 21 (or 22 for SFTP)

2. **Upload Published Files**
   - Local: `./publish` folder
   - Remote: `/` (root) or `/public_html` (check with Monster)
   - Upload all files from the publish folder

3. **Set Folder Permissions**
   - Set `App_Data` to `777` (read/write)
   - Set `bin` to `755` (read/execute)

4. **Verify Upload**
   - Check that `HotelEcomm.dll` is present in root
   - Check that `appsettings.Production.json` is present

---

## ?? Step 6: Deploy via Git (Method 2 - Recommended)

Monster ASP.NET supports Git deployment which is faster and more reliable.

### Push to Monster ASP.NET Git Repository

1. **Get your Git URL from Monster Control Panel**
   - Should look like: `https://yourdomain.git.monsterasp.net/repo.git`

2. **Add Monster as remote**
   ```bash
   git remote add monster https://yourdomain.git.monsterasp.net/repo.git
   ```

3. **Push to Monster**
   ```bash
   git push monster main
   # or
   git push monster master
   ```

4. **Monster will automatically:**
   - ? Build your application
   - ? Run migrations
   - ? Deploy to production
   - ? Start the application

---

## ??? Step 7: Run Database Migrations

### Important: Migrations MUST run before first app start

### Option A: Via SSH/Remote Desktop (if available)

```bash
# SSH into Monster server
ssh user@yourdomain.com

# Navigate to application folder
cd /path/to/app

# Run migrations
dotnet ef database update --project DAL --startup-project Project(DEPI)
```

### Option B: Migration Script on Startup

Add this code to `Program.cs` (already partially done):

```csharp
// This code already exists in your Program.cs
// It runs migrations and creates default users on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await CreateDefaultUsersAndRoles(services);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during initialization: {ex.Message}");
    }
}
```

The application will automatically run migrations on first startup!

---

## ?? Step 8: Configure SSL/TLS Certificate

### Monster ASP.NET usually provides:

1. **Go to Control Panel** ? SSL Certificates
2. **Options**:
   - ? Free Let's Encrypt certificate (auto-renews)
   - ? Paid SSL certificate
   - ? Bring your own certificate

3. **Configuration**:
   ```bash
   # Force HTTPS (already in Program.cs)
   app.UseHttpsRedirection();
   
   # HSTS configured (already in Program.cs)
   app.UseHsts();
   ```

### Update AllowedHosts
```json
"AllowedHosts": "yourdomain.com;www.yourdomain.com"
```

---

## ? Step 9: Verify Deployment

### Check Application Running

1. **Visit your domain**: `https://yourdomain.com`
2. **Verify**:
   - ? Home page loads
   - ? HTTPS works (certificate valid)
   - ? No 500 errors

### Check Application Status

1. **Via Monster Control Panel**:
   - Application Status should show: "Running"
   - No errors in recent logs

2. **View Application Logs**:
   - Monster Control Panel ? Logs
   - Check for startup errors

### Test Features

```
? Login page loads
? Can log in (use admin credentials)
? Can access admin dashboard
? Can book a room
? Can process payment
? Database connection working
? No error pages
```

---

## ?? Step 10: Configure Environment Variables (if Monster supports)

If Monster ASP.NET Control Panel supports custom environment variables:

1. Go to: **Application Settings** or **Environment Variables**
2. Add:
   ```
   ASPNETCORE_ENVIRONMENT=Production
   ConnectionStrings__DefaultConnection=...
   Stripe__SecretKey=sk_live_...
   Stripe__PublishableKey=pk_live_...
   DefaultUsers__Admin__Email=admin@yourdomain.com
   DefaultUsers__Admin__Password=YourPassword123!@#
   ```

3. **Restart application** for changes to take effect

---

## ?? Troubleshooting Common Issues

### Issue: "404 Not Found"
**Solution**:
- Verify all files uploaded to correct directory
- Check that `HotelEcomm.dll` exists
- Restart application in Monster Control Panel

### Issue: "500 Internal Server Error"
**Solution**:
- Check application logs in Monster Control Panel
- Verify database connection string is correct
- Ensure database exists and is accessible
- Check that .NET 9.0 runtime is installed on server

### Issue: "Connection string is empty"
**Solution**:
- Set `ConnectionStrings__DefaultConnection` in app settings
- Or update `appsettings.Production.json` before uploading
- Restart application

### Issue: "Could not connect to database"
**Solution**:
```bash
# Test connection from Monster SSH
sqlcmd -S monster-server.monsterasp.net -U user -P password -d BookifyHotelDB -Q "SELECT @@VERSION;"
```
- Verify server address is correct
- Verify username/password
- Verify database exists
- Check firewall rules

### Issue: "Stripe payment not working"
**Solution**:
- Verify Stripe live keys (not test keys)
- Check that keys match in `appsettings.Production.json`
- Verify Stripe account is active
- Check webhook configuration in Stripe dashboard

### Issue: "Static files not loading (CSS/JavaScript)"
**Solution**:
- Ensure static files folder exists on server
- Check folder permissions (should be readable)
- Verify `app.UseStaticFiles()` in Program.cs

### Issue: "Application won't start"
**Solution**:
- Check application logs in Monster Control Panel
- Verify all NuGet packages are compatible with .NET 9.0
- Ensure no missing dependencies
- Try rebuilding and republishing

---

## ?? Performance Optimization for Monster ASP.NET

### 1. Enable Compression
Already in `Program.cs`:
```csharp
app.UseResponseCompression();
```

### 2. Enable Response Caching
```csharp
app.UseResponseCaching();
```

### 3. Database Connection Pooling
Already configured in Entity Framework Core:
```csharp
options.UseSqlServer(connectionString)
```

### 4. Static File Caching Headers
```csharp
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000");
    }
});
```

---

## ?? Monitoring & Maintenance

### Daily Checks
- [ ] Application running (check Monster dashboard)
- [ ] No error rate spikes
- [ ] Database accessible
- [ ] Stripe integration working

### Weekly Checks
- [ ] Check application logs
- [ ] Monitor resource usage (CPU, memory, disk)
- [ ] Verify backups running
- [ ] Check for security updates

### Monthly Checks
- [ ] Review error logs
- [ ] Check NuGet package updates
- [ ] Verify SSL certificate validity
- [ ] Update security patches
- [ ] Test backup restoration

---

## ?? Security Checklist for Monster ASP.NET

- [ ] HTTPS enabled with valid certificate
- [ ] Strong admin password (8+ chars, complexity required)
- [ ] Stripe live keys configured (not test keys)
- [ ] Database backups enabled
- [ ] Admin account password changed from default
- [ ] HSTS headers enabled (already in code)
- [ ] Rate limiting enabled (already in code)
- [ ] Account lockout configured (already in code)
- [ ] No hardcoded credentials in code
- [ ] Environment variables set for secrets
- [ ] Application logs monitored
- [ ] firewall rules configured

---

## ?? Quick Deployment Command Summary

### One-Command Deployment (Git Method)

```bash
# 1. Build and publish
dotnet publish -c Release -o ./publish

# 2. Commit changes
git add -A
git commit -m "Prepare for Monster ASP.NET deployment"

# 3. Push to Monster
git push monster main
```

Monster will automatically deploy and start your application!

### Manual FTP Deployment

```bash
# 1. Publish
dotnet publish -c Release -o ./publish

# 2. Upload ./publish folder contents to Monster ASP.NET FTP root
# 3. Set folder permissions
# 4. Monster will detect new files and restart application
```

---

## ?? Monster ASP.NET Support

**Official Website**: https://www.monsterasp.net/

**Contact Support**:
- Email: support@monsterasp.net
- Phone: Check website
- Knowledge Base: https://www.monsterasp.net/support

**Common Questions for Monster Support**:
1. "Does your hosting support .NET 9.0?"
2. "How do I deploy via Git?"
3. "How do I access application logs?"
4. "What is my SQL Server connection string?"
5. "How do I set environment variables?"

---

## ? Deployment Checklist

Before deployment:
- [ ] Application builds successfully (`dotnet build`)
- [ ] Publish folder created (`dotnet publish -c Release`)
- [ ] Monster ASP.NET account active
- [ ] SQL Server database created
- [ ] Database connection string obtained
- [ ] Stripe live keys ready
- [ ] Domain configured at Monster
- [ ] appsettings.Production.json configured

During deployment:
- [ ] Files uploaded via FTP or Git pushed
- [ ] Permissions set correctly
- [ ] Application starts successfully

After deployment:
- [ ] Application loads at domain
- [ ] HTTPS works
- [ ] Login page accessible
- [ ] Admin login works
- [ ] Database connection verified
- [ ] Stripe integration tested
- [ ] All features working

---

## ?? Next Steps

1. **Prepare application**: Run `dotnet publish -c Release -o ./publish`
2. **Configure database**: Create in Monster Control Panel
3. **Update settings**: Update `appsettings.Production.json`
4. **Deploy**: Upload via FTP or push via Git
5. **Verify**: Test all features
6. **Monitor**: Watch logs and performance

---

**Ready to launch!** ??

For detailed questions about Monster ASP.NET features, contact their support team directly.
