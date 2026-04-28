# ?? BookifyHotel - Hotel Management System

A modern, secure ASP.NET Core 9 hotel reservation and management system built with Razor Pages, Entity Framework Core, and Stripe payment integration.

## ? Features

- ?? **Secure Authentication**: ASP.NET Core Identity with strong password policies
- ?? **Hotel Management**: Room management, booking system, reservation tracking
- ?? **Payment Integration**: Stripe payment processing with webhook support
- ?? **Role-Based Access**: Admin, Manager, Receptionist, User roles
- ?? **Responsive Design**: Mobile-friendly UI
- ?? **Security Hardened**: HTTPS, CSRF protection, rate limiting, SQL injection prevention
- ?? **Dashboard**: Admin dashboard with statistics and analytics
- ? **Favorites**: Users can mark favorite rooms
- ?? **Contact**: Guest contact form
- ?? **Rate Limiting**: Protection against brute force attacks

## ??? Technology Stack

| Layer | Technology |
|-------|-----------|
| **Framework** | ASP.NET Core 9.0 (C# 13) |
| **UI** | Razor Pages, HTML5, CSS3, JavaScript |
| **Database** | SQL Server 2022 / SQLite (fallback) |
| **ORM** | Entity Framework Core 9.0 |
| **Authentication** | ASP.NET Core Identity |
| **Payment** | Stripe API |
| **Deployment** | Docker, Azure App Service, IIS |

## ?? Prerequisites

### For Local Development
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server 2022 or Express (optional - falls back to SQLite)
- Visual Studio 2022 or VS Code
- Git

### For Production Deployment
- Docker (for containerized deployment)
- Azure subscription (for cloud deployment)
- Stripe account (for payments)
- SQL Server instance

## ?? Quick Start

### 1. Clone the Repository
```bash
git clone https://github.com/Abdo9tech/final_web_application_security.git
cd myFinalPro
```

### 2. Setup Environment (Development)
```bash
# Copy environment template
cp Project(DEPI)/.env.example .env

# Edit with your settings
# For local development, Stripe test keys work fine
```

### 3. Build and Run Locally
```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
cd Project(DEPI)
dotnet run

# Application opens at: http://localhost:5000
```

### 4. Default Credentials (Development)
```
?? Admin
?? Email: admin@bookify.com
?? Password: Admin@123456!

?? Demo User
?? Email: user@bookify.com
?? Password: User@123456!
```

## ?? Docker Deployment

### Quick Docker Start
```bash
# Copy environment file
cp Project(DEPI)/.env.example .env

# Edit .env with your production values
nano .env

# Start with Docker Compose (includes SQL Server)
docker-compose up -d

# Application runs on http://localhost
```

### Environment Variables (in .env)
```env
# SQL Server
SQL_SA_PASSWORD=YourComplexPassword123!@#

# Stripe (use live keys for production)
STRIPE_SECRET_KEY=sk_live_xxxxxxxxxxxxxxxxxxxx
STRIPE_PUBLISHABLE_KEY=pk_live_xxxxxxxxxxxxxxxxxxxx
STRIPE_WEBHOOK_SECRET=whsec_xxxxxxxxxxxxxxxxxxxx

# Admin Account
ADMIN_EMAIL=admin@yourdomain.com
ADMIN_PASSWORD=YourStrongPassword123!@#
ADMIN_PHONE=+201234567890
```

## ?? Azure Deployment

### Step 1: Create Azure Resources
```bash
az login
az group create --name BookifyRG --location eastus
az appservice plan create --name BookifyPlan --resource-group BookifyRG --sku B2
az webapp create --resource-group BookifyRG --plan BookifyPlan --name bookify-hotel-app
```

### Step 2: Configure Application Settings
```bash
az webapp config appsettings set --resource-group BookifyRG --name bookify-hotel-app --settings \
  ASPNETCORE_ENVIRONMENT=Production \
  ConnectionStrings__DefaultConnection="Server=your-sql-server;..." \
  Stripe__SecretKey="sk_live_..." \
  Stripe__PublishableKey="pk_live_..." \
  DefaultUsers__Admin__Email="admin@yourdomain.com" \
  DefaultUsers__Admin__Password="StrongPassword123!@#"
```

### Step 3: Deploy from Visual Studio
```bash
# Right-click HotelEcomm project ? Publish
# Select Azure target
# Follow the wizard
```

## ?? Security Features

? **Authentication & Authorization**
- ASP.NET Core Identity with email uniqueness
- Strong password requirements (8+ chars, 1 digit, 1 uppercase, 1 lowercase, 1 special)
- Account lockout after 5 failed attempts (15 minutes)
- 60-minute session timeout with sliding expiration

? **Data Protection**
- HTTPS enforced on all connections
- HttpOnly cookies (prevent JavaScript access)
- SameSite=Strict cookie policy (prevent CSRF)
- Anti-forgery token validation on all non-GET requests
- PBKDF2-HMACSHA256 password hashing (100,000+ iterations)

? **Request Protection**
- Rate limiting on login endpoint (10 attempts/minute)
- Security headers (X-Frame-Options, X-Content-Type-Options, CSP)
- HSTS headers (force HTTPS for 31,536,000 seconds)
- SQL injection prevention (parameterized queries via EF Core)

? **Infrastructure**
- Environment variable secrets management
- No hardcoded credentials in code
- Secure Stripe key handling
- Development exception page disabled in production

## ?? Database

### Schema
- **Users** (AspNetUsers): Identity users
- **Rooms**: Hotel rooms
- **RoomTypes**: Room categories
- **Bookings**: Guest reservations
- **Payments**: Payment records
- **UserProfiles**: Extended user information
- **Contacts**: Guest inquiries
- **Favorites**: Favorite rooms

### Migrations
```bash
# View pending migrations
dotnet ef migrations list --project DAL

# Apply migrations
dotnet ef database update --project DAL --startup-project Project(DEPI)

# Create new migration
dotnet ef migrations add MigrationName --project DAL --startup-project Project(DEPI)
```

## ?? Stripe Integration

### Setup
1. Create [Stripe account](https://stripe.com)
2. Get API keys from Dashboard ? Developers ? API keys
3. Configure test keys in `appsettings.json` (development)
4. Configure live keys as environment variables (production)

### Test Cards
```
4242 4242 4242 4242  - Success
4000 0000 0000 0002  - Card Declined
3782 822463 10005    - American Express
```

## ?? Testing

### Manual Testing Checklist
- [ ] User registration and login
- [ ] Payment processing (Stripe test cards)
- [ ] Room booking workflow
- [ ] Admin dashboard
- [ ] Rate limiting (wrong password 6+ times)
- [ ] Session timeout
- [ ] Favorite rooms functionality
- [ ] Contact form submission

### Run Unit Tests
```bash
dotnet test
```

## ?? Monitoring & Logs

### Enable Application Insights
```bash
# Add to Program.cs
builder.Services.AddApplicationInsightsTelemetry();
```

### View Logs
- **Azure**: Azure Portal ? App Service ? App Service Logs
- **Docker**: `docker-compose logs -f app`
- **Local**: `~/.config/dotnet-user-secrets/project-name/secrets.json`

## ?? Troubleshooting

### Issue: Database connection failed
```bash
# Check connection string
# Verify SQL Server is running
# Test connection: sqlcmd -S server-name -U sa -P password
```

### Issue: Stripe payment fails
```
# Use test keys for development
# Use live keys (via env vars) for production
# Check webhook configuration in Stripe dashboard
```

### Issue: Port already in use
```bash
# Change port in launchSettings.json or use:
dotnet run --urls "http://localhost:5001"
```

### Issue: HTTPS certificate error (local)
```bash
# Trust development certificate
dotnet dev-certs https --trust
```

## ?? Documentation

- [QUICK_START.md](./Project(DEPI)/QUICK_START.md) - Quick deployment guide
- [DEPLOYMENT_GUIDE.md](./Project(DEPI)/DEPLOYMENT_GUIDE.md) - Detailed deployment instructions
- [Microsoft ASP.NET Core Docs](https://learn.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core Docs](https://learn.microsoft.com/en-us/ef/core/)
- [Stripe API Reference](https://stripe.com/docs/api)

## ?? CI/CD Pipeline

GitHub Actions workflow automatically:
- Builds the application
- Runs tests
- Builds Docker image
- Pushes to container registry
- Deploys to Azure

See `.github/workflows/build-and-deploy.yml`

## ?? Configuration

### appsettings.json (Development)
```json
{
  "Logging": { "LogLevel": { "Default": "Information" } },
  "ConnectionStrings": { "DefaultConnection": "" },
  "AllowedHosts": "*",
  "Stripe": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_..."
  }
}
```

### appsettings.Production.json
All sensitive values should come from environment variables, not hardcoded.

## ?? Contributing

1. Fork the repository
2. Create feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Open Pull Request

## ?? License

This project is licensed under the MIT License - see LICENSE file for details.

## ??ž?? Author

**Abdo Tech**
- GitHub: [@Abdo9tech](https://github.com/Abdo9tech)
- Project: [final_web_application_security](https://github.com/Abdo9tech/final_web_application_security)

## ?? Support

For issues and questions:
1. Check existing [GitHub Issues](https://github.com/Abdo9tech/final_web_application_security/issues)
2. Create new issue with detailed description
3. Include error logs and steps to reproduce

## ?? Release History

- **v1.0.0** - Initial release with core features
  - Hotel room management
  - Booking system
  - Stripe payment integration
  - Admin dashboard
  - Security hardening

---

**Status**: ? Production Ready  
**Framework**: ASP.NET Core 9.0  
**Last Updated**: 2024  
**Maintained**: Active
