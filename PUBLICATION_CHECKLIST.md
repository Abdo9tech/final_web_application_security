# ?? Pre-Publication Checklist

Complete this checklist before deploying to production.

## ? Code Quality

- [ ] Build completes without errors: `dotnet build`
- [ ] No compiler warnings (check build output)
- [ ] All tests pass: `dotnet test`
- [ ] No hardcoded credentials in code
- [ ] No debug logging enabled
- [ ] No database connection strings in appsettings.json

## ?? Security Verification

### Authentication & Passwords
- [ ] Admin password changed from default
- [ ] Strong password policy enabled (8+ chars, complexity required)
- [ ] Account lockout configured (5 attempts, 15 min timeout)
- [ ] Session timeout set (60 minutes)
- [ ] Demo user disabled or password changed

### Data Protection
- [ ] HTTPS enabled
- [ ] HSTS headers configured
- [ ] Anti-forgery tokens enabled
- [ ] HttpOnly cookies configured
- [ ] SameSite cookie policy set to Strict
- [ ] SQL injection prevention via EF Core

### API & Integrations
- [ ] Stripe live keys configured (not test keys)
- [ ] Stripe webhook secret configured
- [ ] Rate limiting enabled on login (10/min)
- [ ] Security headers middleware enabled
- [ ] CORS configured properly

## ??? Database

- [ ] Connection string set correctly
- [ ] Database server firewall configured
- [ ] All migrations applied: `dotnet ef database update`
- [ ] Database backups configured
- [ ] Backup tested and verified
- [ ] Database user has minimal required permissions
- [ ] SQL Server version compatible with .NET 9

## ?? Deployment Environment

### Azure App Service (if using)
- [ ] Resource group created
- [ ] App Service Plan sized appropriately
- [ ] Application Insights configured
- [ ] Application Settings all configured
- [ ] Connection strings set as secrets
- [ ] Environment variables set as secrets
- [ ] HTTPS/SSL certificate installed
- [ ] Custom domain configured (if applicable)
- [ ] Monitoring alerts setup

### Docker (if using)
- [ ] Docker image builds successfully: `docker build -t bookify .`
- [ ] .env file created from .env.example
- [ ] All sensitive values in .env (not hardcoded)
- [ ] docker-compose.yml tested locally
- [ ] Container Registry (if used) configured
- [ ] Container runs successfully: `docker-compose up`

### IIS (if using)
- [ ] .NET 9.0 Runtime installed on server
- [ ] IIS Application Pool configured (.NET CLR = No Managed Code)
- [ ] Website binding configured for HTTPS
- [ ] SSL certificate installed
- [ ] Environment variables set in Application Pool
- [ ] Folder permissions configured
- [ ] Application pool identity has database access

## ?? Configuration

### appsettings.Production.json
- [ ] Logging level set to Warning or higher
- [ ] AllowedHosts configured for production domain
- [ ] ASPNETCORE_ENVIRONMENT = Production
- [ ] ConnectionString uses secure connection
- [ ] Stripe keys use live keys (via env vars)
- [ ] No test/debug values present

### Environment Variables (All Required)
- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] `ConnectionStrings__DefaultConnection=...`
- [ ] `Stripe__SecretKey=sk_live_...`
- [ ] `Stripe__PublishableKey=pk_live_...`
- [ ] `Stripe__WebhookSecret=whsec_...`
- [ ] `DefaultUsers__Admin__Email=...`
- [ ] `DefaultUsers__Admin__Password=...`
- [ ] `DefaultUsers__Admin__PhoneNumber=...`

## ?? Domain & SSL

- [ ] Domain name registered and verified
- [ ] DNS records updated (A or CNAME pointing to server)
- [ ] SSL/TLS certificate installed
- [ ] Certificate is valid and not self-signed
- [ ] HTTPS redirect configured
- [ ] Mixed content warnings addressed
- [ ] CDN configured (if applicable)

## ?? Communication & Notifications

- [ ] Email service configured (if used)
- [ ] Notification templates reviewed
- [ ] Admin notification email configured
- [ ] Error notification email configured
- [ ] Support contact email updated

## ?? Testing (Pre-Deployment)

### Functionality Testing
- [ ] Login with correct credentials works
- [ ] Login with wrong credentials fails
- [ ] Rate limiting works (6 wrong attempts = locked)
- [ ] User registration works
- [ ] Room booking flow complete
- [ ] Payment processing works with test card
- [ ] Payment success/failure pages show
- [ ] Admin dashboard accessible
- [ ] User profile editable
- [ ] Favorites functionality works
- [ ] Contact form submits
- [ ] Session timeout works (60 min)

### Security Testing
- [ ] Cannot access admin pages without login
- [ ] Cannot access protected endpoints without authorization
- [ ] Cannot bypass CSRF protection
- [ ] Cannot inject SQL via input fields
- [ ] XSS attempts are blocked
- [ ] HTTP requests redirect to HTTPS
- [ ] Stripe test cards in production show test warning
- [ ] Invalid Stripe card gets declined

### Performance Testing
- [ ] Home page loads in < 2 seconds
- [ ] Search results return quickly
- [ ] Payment processing completes < 10 seconds
- [ ] Database queries are optimized
- [ ] No N+1 query problems
- [ ] Static files compressed (CSS, JS)
- [ ] Images optimized for web

## ?? Support & Monitoring

- [ ] Application Insights/Monitoring configured
- [ ] Error logging configured
- [ ] Uptime monitoring configured
- [ ] Database backup schedule verified
- [ ] Support contact information current
- [ ] Documentation updated and accessible
- [ ] Runbooks created for common issues
- [ ] On-call schedule established (if needed)

## ?? Final Checks (Day Before Launch)

- [ ] All checklist items completed ?
- [ ] Production database backed up ?
- [ ] Deployment tested on staging environment ?
- [ ] Team notified of launch time ?
- [ ] Rollback plan documented ?
- [ ] Database migration script tested ?
- [ ] Load testing completed (if expected high traffic) ?
- [ ] Performance baseline established ?

## ?? Launch Day

- [ ] Backup production database
- [ ] Deploy application
- [ ] Run post-deployment tests
- [ ] Verify all features working
- [ ] Monitor application logs
- [ ] Check error rates
- [ ] Monitor database performance
- [ ] Monitor user feedback
- [ ] Be ready to rollback if critical issue found

## ?? Post-Launch

- [ ] Monitor application 24/7 first day
- [ ] Check error logs hourly first day
- [ ] Verify database backups ran
- [ ] Update status page
- [ ] Announce to users (if applicable)
- [ ] Plan retrospective meeting
- [ ] Document any issues encountered
- [ ] Update runbooks with new learnings

---

## ?? Emergency Contacts

**In Case of Critical Issues:**
- Team Lead: [Contact Info]
- DevOps: [Contact Info]
- Database Admin: [Contact Info]
- On-Call Rotation: [Link]

---

## ?? Sign-Off

**Prepared By**: __________________ **Date**: __________

**Reviewed By**: __________________ **Date**: __________

**Approved By**: __________________ **Date**: __________

**Deployed By**: __________________ **Date**: __________

---

## ?? Related Documents

- [QUICK_START.md](./Project(DEPI)/QUICK_START.md)
- [DEPLOYMENT_GUIDE.md](./Project(DEPI)/DEPLOYMENT_GUIDE.md)
- [PROJECT_README.md](./PROJECT_README.md)

---

**Remember**: A successful deployment is not just about going live—it's about monitoring and maintaining the application afterward. ?
