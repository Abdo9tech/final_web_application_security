# ? Monster ASP.NET Deployment Checklist

## ?? Pre-Deployment (1-2 Hours Before)

### Account & Hosting Setup
- [ ] Monster ASP.NET account active and paid
- [ ] Hosting plan supports .NET 9.0
- [ ] Domain registered and pointing to Monster servers
- [ ] DNS propagated (may take 24-48 hours)
- [ ] FTP credentials obtained
- [ ] SSH access available (optional)
- [ ] Git repository URL obtained (if using Git deployment)

### Database Setup
- [ ] SQL Server database created in Monster Control Panel
- [ ] Database name: `BookifyHotelDB`
- [ ] Connection string obtained
- [ ] Database user created with full permissions
- [ ] Connection string tested and verified

### Application Configuration
- [ ] `appsettings.Production.json` updated with:
  - [ ] Correct connection string
  - [ ] Correct domain in AllowedHosts
  - [ ] Stripe live API keys
  - [ ] Admin email and password
- [ ] `web.config` configured (if needed)
- [ ] No hardcoded credentials in code
- [ ] ASPNETCORE_ENVIRONMENT ready to set to "Production"

### Security Setup
- [ ] SSL/TLS certificate ordered/obtained
- [ ] Stripe live keys obtained from dashboard
- [ ] Backup of configuration values
- [ ] Admin password meets requirements (8+ chars, complexity)

---

## ?? Build & Publish (30 Minutes)

### Clean Build
- [ ] Unnecessary files removed
- [ ] Previous publish folders deleted
- [ ] `dotnet clean` executed successfully
- [ ] No build errors or warnings

### Restore & Build
- [ ] `dotnet restore` completed
- [ ] `dotnet build -c Release` successful
- [ ] All projects compiled without errors
- [ ] No NuGet package issues

### Publish
- [ ] `dotnet publish -c Release -o ./publish` executed
- [ ] No publish errors
- [ ] `./publish` folder contains:
  - [ ] HotelEcomm.dll
  - [ ] appsettings.Production.json
  - [ ] Views folder
  - [ ] wwwroot folder (static files)
  - [ ] Dependencies (Microsoft.*, Stripe, etc.)
- [ ] Publish size reasonable (~150-300 MB)
- [ ] All necessary files present

### Verify Publish Contents
- [ ] Key files present:
  ```
  ? HotelEcomm.dll
  ? appsettings.json
  ? appsettings.Production.json
  ? web.config (if using IIS)
  ? Views/
  ? wwwroot/
  ? bin/
  ```

---

## ?? Deployment (Choose One Method)

### Method 1: FTP Deployment

#### Before Upload
- [ ] FTP client installed (FileZilla, WinSCP, or VS Code)
- [ ] FTP credentials verified
- [ ] Connection to Monster FTP tested
- [ ] Backup of existing files (if any)

#### Upload Process
- [ ] Connected to Monster FTP successfully
- [ ] Directory permissions verified
- [ ] Upload all files from `./publish` to appropriate folder:
  - [ ] Typical path: `/httpdocs/` or `/public_html/`
  - [ ] Confirm with Monster documentation
- [ ] Upload status: 100% complete
- [ ] No upload errors

#### Post-Upload
- [ ] All files uploaded (check file count)
- [ ] HotelEcomm.dll present on server
- [ ] appsettings.Production.json present
- [ ] Static files folder (wwwroot) uploaded
- [ ] Permissions set correctly:
  - [ ] App_Data: 777 (read/write)
  - [ ] bin: 755 (execute)
  - [ ] Other folders: 755

### Method 2: Git Deployment

#### Before Push
- [ ] Git repository configured
- [ ] Monster Git remote added:
  ```bash
  git remote add monster https://yourdomain.git.monsterasp.net/repo.git
  ```
- [ ] All changes committed
- [ ] `.gitignore` configured to exclude:
  - [ ] `/bin/` folder
  - [ ] `/obj/` folder
  - [ ] `publish/` folder
  - [ ] Local build artifacts

#### Git Push
- [ ] `git status` shows clean working directory
- [ ] `git commit` with descriptive message
- [ ] `git push monster main` (or correct branch) successful
- [ ] No merge conflicts

#### Post-Push
- [ ] Monster CI/CD pipeline triggered
- [ ] Application building on server
- [ ] Application deployed automatically
- [ ] Monitor deployment logs

---

## ??? Database Configuration (Post-Deployment)

### Verify Database
- [ ] Database accessible from server
- [ ] Connection string correct
- [ ] Database user has permissions
- [ ] No connection errors in logs

### Run Migrations
- [ ] Migrations run automatically on app startup (configured in Program.cs)
  OR
- [ ] Manual migration via SSH:
  ```bash
  dotnet ef database update --project DAL --startup-project Project(DEPI)
  ```
- [ ] All migrations applied successfully
- [ ] No migration errors
- [ ] Database schema created

### Verify Default Users
- [ ] Admin user created
- [ ] Demo user created (if enabled)
- [ ] Users have correct roles assigned
- [ ] Passwords hashed properly

---

## ?? Security Configuration (Post-Deployment)

### SSL/TLS Certificate
- [ ] Certificate installed and active
- [ ] Certificate valid for domain
- [ ] HTTPS works without warnings
- [ ] Certificate auto-renewal enabled (if Let's Encrypt)

### HTTPS Configuration
- [ ] HTTP redirects to HTTPS
- [ ] HSTS headers configured
- [ ] No mixed content warnings
- [ ] Security headers present (check via browser dev tools)

### Environment Variables (if available)
- [ ] ASPNETCORE_ENVIRONMENT = "Production"
- [ ] Stripe keys set as env vars
- [ ] Database connection string set as env var
- [ ] Application restarted after env var changes

### Firewall & Access Control
- [ ] Firewall rules configured
- [ ] Only necessary ports open (80, 443)
- [ ] Admin areas protected
- [ ] Rate limiting active

---

## ? Testing & Verification (1 Hour)

### Application Access
- [ ] Domain loads successfully
- [ ] No 404 errors
- [ ] No 500 errors
- [ ] Home page fully renders
- [ ] All resources load (CSS, JavaScript, images)

### HTTPS & Security
- [ ] HTTPS works at domain
- [ ] Certificate valid (no warnings)
- [ ] Security headers present (check via F12)
- [ ] Mixed content warnings absent

### Functionality Testing
- [ ] Login page accessible
- [ ] Can log in with admin credentials
- [ ] Can access admin dashboard
- [ ] Can view room listings
- [ ] Can navigate pages
- [ ] Can book a room
- [ ] Payment page loads correctly

### Database Connection
- [ ] Application connects to database
- [ ] Rooms display from database
- [ ] User login works (reads from database)
- [ ] No connection errors in logs
- [ ] Data persists across requests

### Stripe Integration
- [ ] Payment page loads
- [ ] Stripe publishable key configured correctly
- [ ] Can process test transaction (if test mode)
- [ ] Webhook endpoints accessible
- [ ] Payment success page displays

### Features Testing
- [ ] User registration works (if enabled)
- [ ] Favorites functionality works
- [ ] Contact form works
- [ ] Admin features work
- [ ] Report generation works (if applicable)

---

## ?? Performance & Monitoring

### Initial Performance
- [ ] Home page loads in < 3 seconds
- [ ] Admin dashboard loads in < 2 seconds
- [ ] Search results appear quickly (< 2 seconds)
- [ ] Payment processing < 5 seconds

### Monitor Logs
- [ ] Check Monster Control Panel logs
- [ ] No error entries for past 30 minutes
- [ ] No database connection errors
- [ ] No missing file errors

### Resource Usage
- [ ] CPU usage normal (< 50%)
- [ ] Memory usage normal (< 70%)
- [ ] Disk space available (> 20% free)
- [ ] Database size reasonable

---

## ?? Post-Deployment (First Week)

### Daily Checks (First 3 Days)
- [ ] Application running normally
- [ ] No error spikes
- [ ] Database responsive
- [ ] Users can log in
- [ ] Payment processing working
- [ ] Monitor logs hourly

### Weekly Checks (Week 1)
- [ ] Review error logs
- [ ] Check performance metrics
- [ ] Verify backups running
- [ ] Monitor SSL certificate expiration
- [ ] Test critical user workflows

### Setup Ongoing Monitoring
- [ ] Configure uptime monitoring
- [ ] Setup error alerting
- [ ] Configure backup schedules
- [ ] Document system setup
- [ ] Create runbook for common issues

---

## ?? Troubleshooting Checklist

### Issue: "Cannot access website"
- [ ] Domain DNS propagated
- [ ] Monster nameservers configured
- [ ] Website started in Monster Control Panel
- [ ] Files uploaded to correct directory

### Issue: "HTTPS not working"
- [ ] SSL certificate installed
- [ ] Certificate valid for domain
- [ ] Monster Control Panel shows certificate active
- [ ] Force HTTPS enabled

### Issue: "500 Error on home page"
- [ ] Check Monster logs
- [ ] Verify database connection string
- [ ] Verify database exists
- [ ] Verify .NET 9.0 runtime installed
- [ ] Check appsettings.Production.json syntax

### Issue: "Cannot connect to database"
- [ ] Verify connection string format
- [ ] Test connection from SSH (if available)
- [ ] Verify database user permissions
- [ ] Check firewall rules
- [ ] Verify database created

### Issue: "Login not working"
- [ ] Check database migrations ran
- [ ] Verify default users created
- [ ] Check password requirements met
- [ ] Review authentication logs

### Issue: "Stripe payment fails"
- [ ] Verify Stripe live keys (not test keys)
- [ ] Check Stripe account active
- [ ] Verify webhook URL configured in Stripe
- [ ] Check Stripe logs for errors

### Issue: "Static files not loading (CSS/JS)"
- [ ] Verify wwwroot folder uploaded
- [ ] Check folder permissions (755)
- [ ] Verify paths correct in HTML
- [ ] Clear browser cache

---

## ?? Support & Documentation

### Monster ASP.NET Support
- [ ] Support contact information available
- [ ] Support ticket created if issues
- [ ] Response time noted
- [ ] Issue escalated if needed

### Documentation
- [ ] Application logs documented
- [ ] Configuration documented
- [ ] Database schema documented
- [ ] Deployment process documented
- [ ] Backup/restore procedures documented

---

## ?? Launch Ready!

When all items above are checked:
- [ ] Application is ready for production use
- [ ] Users can be directed to site
- [ ] Announce launch
- [ ] Monitor for first 24 hours continuously

---

## ?? Weekly Maintenance Checklist

Repeat weekly for ongoing operation:
- [ ] Check application logs
- [ ] Verify backups completed
- [ ] Monitor performance metrics
- [ ] Update NuGet packages (monthly)
- [ ] Review security alerts
- [ ] Test critical workflows
- [ ] Check SSL certificate expiration

---

## ?? Sign-Off

**Deployed By**: _________________________ **Date**: __________

**Verified By**: _________________________ **Date**: __________

**Ready for Users**: ? YES / ? NO

---

**Monster ASP.NET Deployment Checklist - Version 1.0**
