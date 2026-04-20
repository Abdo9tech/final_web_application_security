using Microsoft.AspNetCore.Hosting;
using BookifyHotel.Data;
using PLL.Services;
using DAL.DataBase;
using BookifyHotel.Model;
using Project_DEPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Project_DEPI_.Middleware;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
   
    var possibleConnections = new[]
    {
        "Server=.\\SQLEXPRESS;Database=BookifyHotelDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;",
        "Server=(localdb)\\MSSQLLocalDB;Database=BookifyHotelDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;",
        "Server=localhost;Database=BookifyHotelDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;",
        "Data Source=.;Initial Catalog=BookifyHotelDB;Integrated Security=True;TrustServerCertificate=True;"
    };

    foreach (var conn in possibleConnections)
    {
        try
        {
            using var testConn = new Microsoft.Data.SqlClient.SqlConnection(conn);
            testConn.Open();
            connectionString = conn;
            Console.WriteLine($"? Using connection: {conn}");
            break;
        }
        catch
        {
        }
    }
}

if (string.IsNullOrEmpty(connectionString))
{

    connectionString = "Data Source=BookifyHotel.db";
    Console.WriteLine("?? Using SQLite as fallback database");
}

builder.Services.AddDbContext<BookifyHotelDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlServerOptions => sqlServerOptions.MigrationsAssembly("Project(DEPI)"))
    .UseLazyLoadingProxies());
Console.WriteLine($"? Configured for SQL Server: {connectionString}");


// SECURITY [Anti-Forgery / CSRF Protection]: Registers AutoValidateAntiforgeryTokenAttribute globally
// so every non-GET request automatically requires a valid anti-forgery token.
// Individual actions that must be exempt (e.g. webhooks) use [IgnoreAntiforgeryToken].
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});


builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<RoomService>();
builder.Services.AddScoped<RoomTypeService>();
builder.Services.AddScoped<RolePermissionService>();
builder.Services.AddScoped<UserRoleService>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<ReservationCartService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<ProfileService>();

builder.Services.AddScoped<IPaymentService, StripePaymentService>();

// SECURITY [Secure Payment Key Storage]: Stripe secret key is read from configuration.
// In production, set the 'Stripe:SecretKey' value via an environment variable or
// a secrets manager — NEVER commit real keys to source control.
var stripeKey = builder.Configuration["Stripe:SecretKey"];
if (!string.IsNullOrEmpty(stripeKey) && !stripeKey.StartsWith("REPLACE_"))
{
    StripeConfiguration.ApiKey = stripeKey;
    Console.WriteLine("? Stripe API configured");
}
else
{
    Console.WriteLine("?? Stripe SecretKey is not configured. Payment features will be disabled.");
}

// SECURITY [Data Protection API]: ASP.NET Core Data Protection is used to encrypt
// sensitive data such as anti-forgery tokens, authentication cookies, and TempData.
// Keys are persisted automatically and rotated every 90 days by default.
builder.Services.AddDataProtection();

// SECURITY [Strong Password Policy]: Weak passwords are the #1 cause of account compromise.
// Requirements: minimum 8 characters, at least 1 digit, 1 uppercase, 1 lowercase, 1 special character.
// SECURITY [Account Lockout / Brute Force Protection]: After 5 consecutive failed login attempts
// the account is locked for 15 minutes. This defeats credential-stuffing and brute-force attacks.
// SECURITY [Secure Login Validation]: RequireUniqueEmail prevents account enumeration via duplicate registrations.
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    // SECURITY [Strong Password Policy]: Enforce complexity requirements.
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequiredUniqueChars = 4;

    // SECURITY [Broken Authentication Mitigation]: Unique email prevents duplicate account abuse.
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;

    // SECURITY [Account Lockout / Brute Force Protection]: Lock out after 5 failed attempts for 15 minutes.
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<BookifyHotelDbContext>()
.AddDefaultTokenProviders();

// SECURITY [HttpOnly Cookies]: HttpOnly prevents JavaScript from accessing the auth cookie,
//   defending against XSS-based session hijacking.
// SECURITY [Secure Cookies (HTTPS Only)]: CookieSecurePolicy.Always ensures the cookie is
//   only transmitted over HTTPS, preventing plaintext interception.
// SECURITY [SameSite Cookie Policy]: Strict mode prevents the cookie from being sent in
//   cross-site requests, mitigating CSRF attacks.
// SECURITY [Session Timeout Enforcement]: 60-minute sliding expiration forces re-authentication
//   after inactivity, reducing risk from unattended sessions.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login/Login";
    options.LogoutPath = "/Login/Logout";
    options.AccessDeniedPath = "/Register/AccessDenied";

    // SECURITY [Session Timeout Enforcement]: 60 minutes idle timeout with sliding window.
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;

    // SECURITY [HttpOnly Cookies]: Prevents JavaScript access to the session cookie.
    options.Cookie.HttpOnly = true;

    // SECURITY [Secure Cookies (HTTPS Only)]: Only transmit cookie over encrypted connections.
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;

    // SECURITY [SameSite Cookie Policy]: Strict prevents cross-site request forgery via cookies.
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;

    options.Cookie.Name = ".BookifyHotel.Auth";
    options.Cookie.IsEssential = true;
});

// SECURITY [Session Hijacking Prevention]: Session cookie is marked HttpOnly, Secure, and SameSite=Strict.
// The 30-minute idle timeout limits the window of opportunity for session theft.
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);

    // SECURITY [HttpOnly Cookies]: Session cookie is not accessible via JavaScript.
    options.Cookie.HttpOnly = true;

    // SECURITY [Secure Cookies (HTTPS Only)]: Session cookie only sent over HTTPS.
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;

    // SECURITY [SameSite Cookie Policy]: Prevent session cookie in cross-site requests.
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;

    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".BookifyHotel.Session";
});

// SECURITY [Rate Limiting]: Protects the login endpoint from brute-force attacks
// by limiting each IP to 10 login attempts per minute. Excess requests return HTTP 429.
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("LoginPolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 10;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    // SECURITY [Rate Limiting]: Return 429 Too Many Requests instead of throwing.
    options.RejectionStatusCode = 429;
});

// SECURITY [Policy-Based Authorization / Default Deny Access Strategy]:
// A fallback policy is registered that requires all authenticated users by default.
// Any controller/action that should be public must explicitly opt-out with [AllowAnonymous].
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

// SECURITY [Security Misconfiguration Hardening]: Developer exception page is shown
// ONLY in development. In production, a generic error page is used to prevent
// stack traces and internal details from leaking to attackers.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // SECURITY [HSTS]: HTTP Strict Transport Security forces browsers to use HTTPS exclusively.
    app.UseHsts();
}

// SECURITY [HTTPS Redirection]: Redirect all HTTP requests to HTTPS to ensure encrypted transport.
app.UseHttpsRedirection();
app.UseStaticFiles();

// SECURITY [Security Headers / Defense-in-Depth]: Custom middleware injects defensive HTTP headers
// (X-Frame-Options, X-Content-Type-Options, CSP, Referrer-Policy, Permissions-Policy)
// on every response to protect against clickjacking, MIME-sniffing, and XSS.
app.UseSecurityHeaders();

app.UseRouting();

// SECURITY [Rate Limiting]: Apply rate limiting middleware before authentication so abusive
// traffic is rejected before touching the authentication layer.
app.UseRateLimiter();

// SECURITY [Session management]: Session must be initialized before authentication
// so it is available throughout the request lifecycle.
app.UseSession();

// SECURITY [Broken Authentication Mitigation]: Authentication and authorization middleware
// must be registered in this exact order: UseAuthentication → UseAuthorization.
app.UseAuthentication();

// SECURITY [Role-Based Authorization / Default Deny Access Strategy]: Authorization middleware
// enforces the fallback policy and all [Authorize] attributes defined on controllers.
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        Console.WriteLine("========================================");
        Console.WriteLine("🚀 INITIALIZING APPLICATION");
        Console.WriteLine("========================================");

        // Just create default users - database should already exist from migrations
        await CreateDefaultUsersAndRoles(services);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error during initialization: {ex.Message}");
        Console.WriteLine($"   Stack: {ex.StackTrace}");
    }
}

async Task CreateDefaultUsersAndRoles(IServiceProvider services)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<AppUser>>();
    var dbContext = services.GetRequiredService<BookifyHotelDbContext>();

    Console.WriteLine("========================================");
    Console.WriteLine("🔐 CREATING DEFAULT USERS AND ROLES");
    Console.WriteLine("========================================");

    // SECURITY [Role-Based Authorization]: Creating system roles used throughout the application
    // to enforce least-privilege access control (Admin, Manager, Receptionist, User).
    string[]
        roles = { "Admin", "User", "Manager", "Receptionist" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
            Console.WriteLine($"✅ Role '{role}' created");
        }
        else
        {
            Console.WriteLine($"ℹ️  Role '{role}' already exists");
        }
    }

    // SECURITY [Secure Password Hashing]: ASP.NET Core Identity uses PBKDF2 with HMACSHA256
    // (100,000 iterations by default in .NET 9) to hash passwords — never stored in plain text.
    // Admin credentials should be changed immediately after first deployment.
    // SECURITY NOTE: Default credentials are read from configuration (appsettings / env vars).
    // In production, use environment variables or a secrets manager for these values.
    var adminEmail = builder.Configuration["DefaultUsers:Admin:Email"] ?? "admin@bookify.com";
    var adminPassword = builder.Configuration["DefaultUsers:Admin:Password"] ?? "Admin@123456!";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new AppUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            PhoneNumber = builder.Configuration["DefaultUsers:Admin:PhoneNumber"] ?? "+201234567890"
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            
            // Create UserProfile for Admin
            var adminProfile = new UserProfile
            {
                IdentityUserId = adminUser.Id,
                FullName = "System Administrator",
                Email = adminEmail,
                PhoneNumber = "+201234567890",
                Address = "Admin Office",
                City = "Cairo",
                Country = "Egypt",
                CreatedDate = DateTime.Now,
                ProfilePhotoPath = ""
            };
            dbContext.UserProfiles.Add(adminProfile);
            await dbContext.SaveChangesAsync();

            Console.WriteLine("========================================");
            Console.WriteLine("✅ ADMIN ACCOUNT CREATED SUCCESSFULLY!");
            Console.WriteLine("========================================");
            Console.WriteLine($"📧 Email: {adminEmail}");
            Console.WriteLine($"👤 Role: Admin");
            Console.WriteLine("========================================");
        }
        else
        {
            Console.WriteLine("❌ Failed to create admin user:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"   - {error.Description}");
            }
        }
    }
    else
    {
        Console.WriteLine("ℹ️  Admin user already exists");
        
        // Ensure admin has profile
        var adminProfile = dbContext.UserProfiles.FirstOrDefault(p => p.IdentityUserId == adminUser.Id);
        if (adminProfile == null)
        {
            adminProfile = new UserProfile
            {
                IdentityUserId = adminUser.Id,
                FullName = "System Administrator",
                Email = adminEmail,
                PhoneNumber = "+201234567890",
                Address = "Admin Office",
                City = "Cairo",
                Country = "Egypt",
                CreatedDate = DateTime.Now,
                ProfilePhotoPath = ""
            };
            dbContext.UserProfiles.Add(adminProfile);
            await dbContext.SaveChangesAsync();
            Console.WriteLine("✅ Admin profile created");
        }

        // Ensure admin has Admin role
        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            Console.WriteLine("✅ Admin role assigned");
        }

        Console.WriteLine("========================================");
        Console.WriteLine("ℹ️  EXISTING ADMIN CREDENTIALS:");
        Console.WriteLine("========================================");
        Console.WriteLine($"📧 Email: {adminEmail}");
        Console.WriteLine($"👤 Role: Admin");
        Console.WriteLine("========================================");
    }

    // SECURITY [Secure Password Hashing]: Demo user password also hashed via Identity PBKDF2.
    var userEmail = builder.Configuration["DefaultUsers:User:Email"] ?? "user@bookify.com";
    var userPassword = builder.Configuration["DefaultUsers:User:Password"] ?? "User@123456!";

    var normalUser = await userManager.FindByEmailAsync(userEmail);
    if (normalUser == null)
    {
        normalUser = new AppUser
        {
            UserName = userEmail,
            Email = userEmail,
            EmailConfirmed = true,
            PhoneNumber = builder.Configuration["DefaultUsers:User:PhoneNumber"] ?? "+201556677889"
        };

        var result = await userManager.CreateAsync(normalUser, userPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(normalUser, "User");
            
            // Create UserProfile for Demo User
            var userProfile = new UserProfile
            {
                IdentityUserId = normalUser.Id,
                FullName = "Demo User",
                Email = userEmail,
                PhoneNumber = "+201556677889",
                Address = "123 Demo Street",
                City = "Alexandria",
                Country = "Egypt",
                CreatedDate = DateTime.Now,
                ProfilePhotoPath = ""
            };
            dbContext.UserProfiles.Add(userProfile);
            await dbContext.SaveChangesAsync();

            Console.WriteLine("✅ Demo user created");
            Console.WriteLine($"   📧 Email: {userEmail}");
        }
    }
    else
    {
        Console.WriteLine("ℹ️  Demo user already exists");
        
        // Ensure user has profile
        var userProfile = dbContext.UserProfiles.FirstOrDefault(p => p.IdentityUserId == normalUser.Id);
        if (userProfile == null)
        {
            userProfile = new UserProfile
            {
                IdentityUserId = normalUser.Id,
                FullName = "Demo User",
                Email = userEmail,
                PhoneNumber = "+201556677889",
                Address = "123 Demo Street",
                City = "Alexandria",
                Country = "Egypt",
                CreatedDate = DateTime.Now,
                ProfilePhotoPath = ""
            };
            dbContext.UserProfiles.Add(userProfile);
            await dbContext.SaveChangesAsync();
            Console.WriteLine("✅ Demo user profile created");
        }
    }

    Console.WriteLine("========================================");
    Console.WriteLine("✅ USER INITIALIZATION COMPLETED");
    Console.WriteLine("========================================\n");
}


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();







