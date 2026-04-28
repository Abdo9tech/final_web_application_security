# ?? Monster ASP.NET - Quick Reference Guide

## ? 5-Minute Quick Start

### Before You Start
- ? Monster ASP.NET account active
- ? SQL Server database created
- ? FTP credentials ready
- ? Domain configured

### 3 Commands to Deploy

```bash
# 1. Publish (5 minutes)
dotnet publish -c Release -o ./publish

# 2. Upload via FTP (5-10 minutes)
# Use FileZilla/WinSCP to upload ./publish contents to Monster

# 3. Verify
# Visit https://yourdomain.com and test features
```

---

## ?? Configuration Checklist

### Required Before Deployment
```
? Database Connection String
  Server=your-monster-server.com;Database=BookifyHotelDB;User Id=user;Password=pass;

? Stripe Live Keys
  Stripe__SecretKey=sk_live_...
  Stripe__PublishableKey=pk_live_...

? Admin Credentials
  DefaultUsers__Admin__Email=admin@yourdomain.com
  DefaultUsers__Admin__Password=StrongPassword123!@#

? Domain
  AllowedHosts=yourdomain.com;www.yourdomain.com
```

---

## ?? Two Deployment Methods

### Method 1: FTP (Easy)
```
1. dotnet publish -c Release -o ./publish
2. Open FileZilla/WinSCP
3. Connect with Monster FTP credentials
4. Upload ./publish folder contents
5. Done!
```

### Method 2: Git (Recommended)
```bash
# 1. Add Monster remote
git remote add monster https://yourdomain.git.monsterasp.net/repo.git

# 2. Push
git push monster main

# Monster automatically builds and deploys!
```

---

## ?? Monster Configuration

### Get Your Details
1. Log in to Monster ASP.NET Control Panel
2. Find:
   - **FTP Host**: `ftp.yourdomain.com` or provided
   - **FTP User**: Your account username
   - **FTP Password**: Your account password
   - **Database Server**: `server.monsterasp.net`
   - **Database Name**: `BookifyHotelDB`
   - **Database User**: Created in Control Panel

### First Time Setup
```
1. Control Panel ? Databases ? Create New
2. Name: BookifyHotelDB
3. Control Panel ? Database Users ? Create
4. Give full permissions to BookifyHotelDB
5. Copy connection string
6. Update appsettings.Production.json
```

---

## ?? What Gets Deployed

From `./publish` folder:
```
publish/
??? HotelEcomm.dll                (application)
??? appsettings.json              (config)
??? appsettings.Production.json   (production config)
??? Views/                         (razor pages)
??? wwwroot/                       (CSS, JS, images)
??? bin/                           (dependencies)
??? [other dependencies]
```

---

## ? Test After Deployment

### Essential Tests
```
? https://yourdomain.com loads
? Login page accessible
? Admin login works
? Can book a room
? Payment page loads
? Dashboard works
? No 500 errors
```

### Browser Dev Tools Check (F12)
```
? HTTPS protocol
? No console errors
? No broken resources (CSS/JS)
? Security headers present
```

---

## ?? Common Issues & Fixes

### "404 Not Found"
```
? Verify files uploaded to correct directory
? Check HotelEcomm.dll exists on server
? Restart application in Monster Control Panel
```

### "500 Error"
```
? Check Monster logs
? Verify database connection string
? Ensure .NET 9.0 runtime installed
? Check appsettings.Production.json format
```

### "Cannot connect to database"
```
? Verify connection string format
? Check database exists
? Verify database user has permissions
? Test with: sqlcmd -S server -U user -P pass -d BookifyHotelDB
```

### "HTTPS not working"
```
? Check SSL certificate in Monster Control Panel
? Verify certificate is installed
? Check domain in certificate matches
```

### "Stripe not working"
```
? Use LIVE keys (not test keys) in Production
? Verify keys in appsettings.Production.json
? Check Stripe account active
```

### "Static files broken (CSS/JS)"
```
? Ensure wwwroot folder uploaded
? Check folder permissions (755)
? Clear browser cache
```

---

## ?? FTP Upload Steps (Detailed)

### Using FileZilla

1. **Open FileZilla**
   - File ? Site Manager

2. **Add New Site**
   - Protocol: FTP
   - Host: `ftp.yourdomain.com`
   - Port: 21
   - Username: Your FTP username
   - Password: Your FTP password

3. **Connect**
   - Click Connect
   - Should see remote files

4. **Navigate**
   - Remote: Go to `/httpdocs/` (or check with Monster)
   - Local: Open `./publish` folder

5. **Upload**
   - Select all files in `./publish`
   - Drag to remote FTP folder
   - Wait for 100% complete

6. **Verify**
   - Check HotelEcomm.dll exists
   - Check Views folder exists
   - Check wwwroot folder exists

---

## ?? FTP Upload Steps (Alternative - WinSCP)

1. **Open WinSCP**
2. **New Session**
   - Host: `ftp.yourdomain.com`
   - Port: 21
   - Username: Your FTP username
   - Password: Your FTP password
3. **Login**
4. **Navigate to `/httpdocs/` (or Monster specified path)**
5. **Drag `./publish` contents to remote panel**
6. **Wait for upload to complete**

---

## ?? Performance Tips

### Enable Compression
Already configured in code - gzip compression reduces traffic

### Use CDN for Static Files
```
1. Upload wwwroot to CDN
2. Update wwwroot paths in layout
3. Reduces server load
```

### Enable Caching
```
In appsettings.Production.json:
"CacheControl": "public,max-age=31536000"
```

### Monitor Database
```
Regular backups enabled in Monster Control Panel
Monitor query performance
Consider indexing common queries
```

---

## ?? Monster ASP.NET Support

**Website**: https://www.monsterasp.net/

**Contact**:
- Support Portal: Check your account
- Email: support@monsterasp.net
- Phone: Listed on website

**Ask Them**:
- "Does your hosting support .NET 9.0?"
- "What is my FTP host address?"
- "How do I access my database?"
- "Where is the application root folder?"
- "How do I view application logs?"
- "How do I set environment variables?"

---

## ?? Daily Monitoring

### First Week
- [ ] Check logs every day
- [ ] Monitor for errors
- [ ] Verify users can access
- [ ] Test key features

### Ongoing
- [ ] Weekly log review
- [ ] Monthly backup test
- [ ] Monitor SSL certificate expiration
- [ ] Check performance metrics

---

## ?? Emergency Checklist

If deployment fails:

1. **Check Monster Control Panel**
   - Application Status ? Should be "Running"
   - Recent Logs ? Check for errors

2. **Verify Files Uploaded**
   - FTP in and check files present
   - HotelEcomm.dll must be there
   - appsettings.Production.json must be there

3. **Check Configuration**
   - Database connection string correct?
   - .NET 9.0 runtime installed?
   - Environment variables set?

4. **Review Logs**
   - Monster Control Panel ? Application Logs
   - Look for specific error messages
   - Database connection errors?
   - Missing file errors?

5. **Contact Monster Support**
   - Provide error message
   - Provide application logs
   - Ask if .NET 9.0 is installed
   - Ask to verify database accessible

---

## ?? Key Files Locations

| File | Purpose | Location |
|------|---------|----------|
| HotelEcomm.dll | Application executable | /httpdocs/ (root) |
| appsettings.Production.json | Production config | /httpdocs/ (root) |
| Views/ | Razor Pages | /httpdocs/Views/ |
| wwwroot/ | Static files | /httpdocs/wwwroot/ |
| Database | SQL Server | Monster-hosted server |

---

## ? Success Indicators

? Website accessible at domain  
? HTTPS works (valid certificate)  
? Login page loads  
? Can log in with admin account  
? Can view rooms  
? Can book a room  
? Payment page accessible  
? Admin dashboard works  
? No errors in logs  
? Database queries working  

**If all ?, you're live!**

---

## ?? Next Steps

1. **Choose deployment method** (FTP or Git)
2. **Publish application** (`dotnet publish -c Release -o ./publish`)
3. **Deploy** (Upload via FTP or push via Git)
4. **Verify** (Test all features)
5. **Monitor** (Watch logs for first week)
6. **Announce** (Tell users to visit site)

---

## ?? Quick Reference Commands

```bash
# Publish
dotnet publish -c Release -o ./publish

# Build only
dotnet build -c Release

# Clean
dotnet clean

# Restore
dotnet restore

# Test locally
dotnet run

# View current config
type appsettings.Production.json

# Run migrations manually (if needed)
dotnet ef database update --project DAL --startup-project Project(DEPI)
```

---

**Monster ASP.NET Deployment Guide - Quick Reference**  
**Version 1.0 | Last Updated: 2024**

Ready to deploy? Follow the **5-Minute Quick Start** above! ??
