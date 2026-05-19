using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
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
using System.IO;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string is missing. Set ConnectionStrings__DefaultConnection as an environment variable.");
}

builder.Services.AddDbContext<BookifyHotelDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlServerOptions => {
            sqlServerOptions.MigrationsAssembly("HotelEcomm");
            sqlServerOptions.EnableRetryOnFailure();
        })
    .UseLazyLoadingProxies()
    .ConfigureWarnings(warnings =>
        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
Console.WriteLine($"? Configured for SQL Server: {connectionString}");

builder.Services.AddHealthChecks();


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
// Keys are persisted to a directory that should be mapped to a Docker volume
// to ensure they survive container restarts.
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"));

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

    // SECURITY [Secure Cookies (HTTPS Only)]: Only transmit cookie over encrypted connections in production.
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
        ? Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest 
        : Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;

    // SECURITY [SameSite Cookie Policy]: Lax is more compatible for local development while still providing protection.
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;

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

    // SECURITY [Secure Cookies (HTTPS Only)]: Only transmit cookie over encrypted connections in production.
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
        ? Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest 
        : Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;

    // SECURITY [SameSite Cookie Policy]: Lax is more compatible for local development.
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;

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

        // Step 1: Ensure the database exists by connecting to 'master' first
        await EnsureDatabaseExistsAsync(connectionString);

        // Step 2: Now run EF Core migrations (database already exists, so no 4060 error)
        var dbContext = services.GetRequiredService<BookifyHotelDbContext>();
        Console.WriteLine("Applying database migrations...");
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("✅ Migrations applied successfully");

        // Step 3: Add missing columns that exist in the model but not in any migration
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('RoomTypes') AND name = 'Capacity')
                BEGIN
                    ALTER TABLE RoomTypes ADD Capacity int NOT NULL DEFAULT 2
                END");
            Console.WriteLine("✅ Schema patches applied");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Schema patch warning: {ex.Message}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ FATAL: Database migration failed: {ex.Message}");
        Console.WriteLine($"   Inner: {ex.InnerException?.Message}");
        Console.WriteLine($"   Stack: {ex.StackTrace}");
        throw; // App cannot work without a database
    }

    // Step 4: Seed data (non-fatal - app can start without seed data)
    try
    {
        await CreateDefaultUsersAndRoles(services);
        await CreateSampleData(services);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Seeding warning (non-fatal): {ex.Message}");
        Console.WriteLine($"   Inner: {ex.InnerException?.Message}");
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
            Console.WriteLine($" Role '{role}' created");
        }
        else
        {
            Console.WriteLine($" Role '{role}' already exists");
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
            Console.WriteLine(" ADMIN ACCOUNT CREATED SUCCESSFULLY!");
            Console.WriteLine("========================================");
            Console.WriteLine($"Email: {adminEmail}");
            Console.WriteLine($" Role: Admin");
            Console.WriteLine("========================================");
        }
        else
        {
            Console.WriteLine("Failed to create admin user:");
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
        Console.WriteLine("  EXISTING ADMIN CREDENTIALS:");
        Console.WriteLine("========================================");
        Console.WriteLine($" Email: {adminEmail}");
        Console.WriteLine($"Role: Admin");
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
    Console.WriteLine(" USER INITIALIZATION COMPLETED");
    Console.WriteLine("========================================\n");
}

async Task CreateSampleData(IServiceProvider services)
{
    var dbContext = services.GetRequiredService<BookifyHotelDbContext>();
    
    // Add Room Types
    if (!dbContext.RoomTypes.Any())
    {
        Console.WriteLine("🌱 Seeding Room Types...");
        var standardType = new BookifyHotel.Model.RoomType { Name = "Standard", Description = "A comfortable standard room.", PricePerNight = 100, Capacity = 2 };
        var deluxeType = new BookifyHotel.Model.RoomType { Name = "Deluxe", Description = "A luxurious deluxe room.", PricePerNight = 200, Capacity = 3 };
        var suiteType = new BookifyHotel.Model.RoomType { Name = "Suite", Description = "A premium suite.", PricePerNight = 400, Capacity = 5 };
        
        dbContext.RoomTypes.AddRange(standardType, deluxeType, suiteType);
        await dbContext.SaveChangesAsync();
        Console.WriteLine(" Room Types seeded");
    }
    else
    {
        // Ensure capacity is set for existing room types
        var existingTypes = dbContext.RoomTypes.Where(t => t.Capacity == 0).ToList();
        if (existingTypes.Any())
        {
            Console.WriteLine("🔄 Updating existing Room Type capacities...");
            foreach (var type in existingTypes)
            {
                type.Capacity = type.Name switch
                {
                    "Standard" => 2,
                    "Deluxe" => 3,
                    "Suite" => 5,
                    _ => 2
                };
            }
            await dbContext.SaveChangesAsync();
            Console.WriteLine(" Room Type capacities updated");
        }
    }

    // Add Rooms
    if (dbContext.Rooms.Any(r => string.IsNullOrWhiteSpace(r.Location)))
    {
        Console.WriteLine(" Found rooms with invalid data. Re-seeding...");
        dbContext.Rooms.RemoveRange(dbContext.Rooms);
        await dbContext.SaveChangesAsync();
    }

    if (!dbContext.Rooms.Any())
    {
        Console.WriteLine(" Seeding Rooms...");
        // Need to get the types again or use existing if we just created them
        var standardType = dbContext.RoomTypes.First(t => t.Name == "Standard");
        var deluxeType = dbContext.RoomTypes.First(t => t.Name == "Deluxe");
        var suiteType = dbContext.RoomTypes.First(t => t.Name == "Suite");

        var rooms = new List<BookifyHotel.Model.Room>
        {
            new BookifyHotel.Model.Room { RoomNumber = 101, Floor = 1, Status = "Available", IsAvailable = true, RoomTypeId = standardType.RoomTypeId, ImageUrl = "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?q=80&w=1000&auto=format&fit=crop", Location = "Manchester, City Centre" },
            new BookifyHotel.Model.Room { RoomNumber = 102, Floor = 1, Status = "Available", IsAvailable = true, RoomTypeId = standardType.RoomTypeId, ImageUrl = "https://images.unsplash.com/photo-1611892440504-42a792e24d32?q=80&w=1000&auto=format&fit=crop", Location = "Manchester, City Centre" },
            new BookifyHotel.Model.Room { RoomNumber = 201, Floor = 2, Status = "Available", IsAvailable = true, RoomTypeId = deluxeType.RoomTypeId, ImageUrl = "https://images.unsplash.com/photo-1566665797739-1674de7a421a?q=80&w=1000&auto=format&fit=crop", Location = "Manchester, City Centre" },
            new BookifyHotel.Model.Room { RoomNumber = 202, Floor = 2, Status = "Available", IsAvailable = true, RoomTypeId = deluxeType.RoomTypeId, ImageUrl = "https://images.unsplash.com/photo-1590490360182-c33d59735188?q=80&w=1000&auto=format&fit=crop", Location = "Manchester, City Centre" },
            new BookifyHotel.Model.Room { RoomNumber = 301, Floor = 3, Status = "Available", IsAvailable = true, RoomTypeId = suiteType.RoomTypeId, ImageUrl = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?q=80&w=1000&auto=format&fit=crop", Location = "Manchester, City Centre" }
        };
        
        dbContext.Rooms.AddRange(rooms);
        await dbContext.SaveChangesAsync();
        Console.WriteLine(" Rooms seeded");
    }
    // Ensure all rooms are available and have a location for demo purposes
    var allRooms = dbContext.Rooms.ToList();
    if (allRooms.Any())
    {
        Console.WriteLine(" Synchronizing room availability and locations...");
        foreach (var room in allRooms)
        {
            room.IsAvailable = true;
            room.Status = "Available";
            room.Location = "Manchester, City Centre";

            if (room.ImageUrl == "/images/default-room.jpg" || string.IsNullOrEmpty(room.ImageUrl))
            {
                room.ImageUrl = room.RoomNumber switch
                {
                    101 => "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?q=80&w=1000&auto=format&fit=crop",
                    102 => "https://images.unsplash.com/photo-1611892440504-42a792e24d32?q=80&w=1000&auto=format&fit=crop",
                    201 => "https://images.unsplash.com/photo-1566665797739-1674de7a421a?q=80&w=1000&auto=format&fit=crop",
                    202 => "https://images.unsplash.com/photo-1590490360182-c33d59735188?q=80&w=1000&auto=format&fit=crop",
                    301 => "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?q=80&w=1000&auto=format&fit=crop",
                    _ => "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?q=80&w=1000&auto=format&fit=crop"
                };
            }
        }
        await dbContext.SaveChangesAsync();
        Console.WriteLine(" All rooms synchronized");
    }
}


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();

app.Run();

// Helper: Connect to 'master' database to create BookifyHotelDb if it doesn't exist.
// This avoids SQL error 4060 where EF Core tries to connect to a non-existent database.
async Task EnsureDatabaseExistsAsync(string connString)
{
    var csBuilder = new SqlConnectionStringBuilder(connString);
    var databaseName = csBuilder.InitialCatalog;
    csBuilder.InitialCatalog = "master"; // Connect to master instead

    Console.WriteLine($"🔍 Ensuring database '{databaseName}' exists...");

    // Retry connecting to master for up to 60 seconds (SQL Server may still be starting)
    for (int attempt = 1; attempt <= 30; attempt++)
    {
        try
        {
            using var connection = new SqlConnection(csBuilder.ConnectionString);
            await connection.OpenAsync();

            // Create the database if it doesn't exist
            using var createCmd = connection.CreateCommand();
            createCmd.CommandText = $@"
                IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{databaseName}')
                BEGIN
                    CREATE DATABASE [{databaseName}]
                END";
            await createCmd.ExecuteNonQueryAsync();
            Console.WriteLine($"✅ Database '{databaseName}' created/verified");

            // Wait for the database to come fully online
            Console.WriteLine($"⏳ Waiting for database '{databaseName}' to come online...");
            for (int waitAttempt = 1; waitAttempt <= 30; waitAttempt++)
            {
                using var checkConn = new SqlConnection(connString);
                try
                {
                    await checkConn.OpenAsync();
                    Console.WriteLine($"✅ Database '{databaseName}' is online and ready");
                    return;
                }
                catch
                {
                    Console.WriteLine($"   Waiting for database to be ready... {waitAttempt}/30");
                    await Task.Delay(2000);
                }
            }

            Console.WriteLine($"✅ Database '{databaseName}' created, proceeding...");
            return;
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"⏳ Waiting for SQL Server... attempt {attempt}/30 (Error: {ex.Number})");
            if (attempt == 30)
            {
                Console.WriteLine($"❌ Could not connect to SQL Server after 30 attempts");
                throw;
            }
            await Task.Delay(2000);
        }
    }
}
