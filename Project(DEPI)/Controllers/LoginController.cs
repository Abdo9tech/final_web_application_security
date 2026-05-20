using BookifyHotel.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.Tasks;
using System.Security.Claims;
using BookifyHotel.Data;

namespace Project_DEPI_.Controllers
{
    public class LoginController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly BookifyHotelDbContext _context;

        // SECURITY [Security Event Logging]: ILogger is injected to log all authentication
        // events (success, failure, lockout) for security monitoring and incident response.
        private readonly ILogger<LoginController> _logger;

        public LoginController(
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            BookifyHotelDbContext context,
            ILogger<LoginController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        // SECURITY [Default Deny Access Strategy]: Login page must be public (AllowAnonymous)
        // because the global FallbackPolicy requires authentication — this opts out intentionally.
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl
            };
            return View(model);
        }

        // SECURITY [Brute Force Protection / Rate Limiting]: The "LoginPolicy" rate limiter
        // defined in Program.cs restricts this endpoint to 10 requests per minute per IP.
        // SECURITY [Account Lockout Mechanism]: lockoutOnFailure: true activates the Identity
        // lockout system — after 5 consecutive failures, the account is locked for 15 minutes.
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // SECURITY [Secure Login Validation]: PasswordSignInAsync is used with
            // lockoutOnFailure: true so Identity automatically tracks and enforces
            // account lockout after the configured number of consecutive failures.
            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);  // SECURITY: was false — now enables lockout

            if (result.Succeeded)
            {
                // SECURITY [Security Event Logging]: Log successful login for audit trail.
                // Email is logged in masked form to avoid storing PII in plain text in logs.
                _logger.LogInformation("[SECURITY] Successful login for user: {Email}", MaskEmail(model.Email));

                // Get user data for role-based redirect
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user != null)
                {
                    // SECURITY [Role-Based Authorization]: Users are redirected based on their
                    // Identity role. Admin users go to the dashboard; regular users go to Home.
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToAction("Index", "Dashboard");
                    }
                    else if (await _userManager.IsInRoleAsync(user, "User"))
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }

                // SECURITY [Open Redirect Prevention]: Url.IsLocalUrl() validates the ReturnUrl
                // before redirecting to prevent open-redirect attacks that could be used for phishing.
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            else if (result.IsLockedOut)
            {
                // SECURITY [Account Lockout Mechanism / Failed Login Logging]:
                // Log the lockout event for security monitoring.
                _logger.LogWarning("[SECURITY] Account locked out for user: {Email}", MaskEmail(model.Email));
                ModelState.AddModelError("", "Account is temporarily locked due to multiple failed attempts. Please try again in 15 minutes.");
                return View(model);
            }
            else if (result.IsNotAllowed)
            {
                _logger.LogWarning("[SECURITY] Login not allowed for user: {Email}", MaskEmail(model.Email));
                ModelState.AddModelError("", "Your account is not allowed to sign in. Please confirm your email first.");
                return View(model);
            }
            else
            {
                // SECURITY [Failed Login Logging]: Every failed login attempt is logged with
                // a masked email, enabling detection of credential-stuffing or brute-force attacks.
                _logger.LogWarning("[SECURITY] Failed login attempt for: {Email} from IP: {IP}",
                    MaskEmail(model.Email),
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

                // SECURITY [Broken Authentication Mitigation / Information Leakage Prevention]:
                // A single generic error message is returned regardless of whether the email
                // exists or the password is wrong. This prevents user enumeration attacks
                // where an attacker can determine valid email addresses in the system.
                ModelState.AddModelError("", "Invalid credentials. Please check your email and password.");

                return View(model);
            }
        }

        // SECURITY [Default Deny Access Strategy]: Logout must be accessible to authenticated users.
        // ValidateAntiForgeryToken (applied globally) prevents CSRF-based forced logout.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // SECURITY [Security Event Logging]: Log logout event for audit trail.
            var userName = User.Identity?.Name ?? "unknown";
            _logger.LogInformation("[SECURITY] User logged out: {User}", userName);

            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "BasePage");
        }

        // SECURITY [No Sensitive Data Logging]: Email addresses are masked before logging
        // to prevent PII exposure in log files (e.g. "us***@example.com").
        private static string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
                return "***";

            var parts = email.Split('@');
            var local = parts[0];
            var maskedLocal = local.Length <= 2
                ? new string('*', local.Length)
                : local[0] + new string('*', local.Length - 2) + local[^1];
            return $"{maskedLocal}@{parts[1]}";
        }

        // SECURITY [Google OAuth Challenge]: Initiates the external authentication challenge
        // using Google provider. CSRF verification is performed implicitly on challenge.
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Login", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        // SECURITY [Google OAuth Callback]: Processes the external login callback.
        // If the user already has an account, logs them in. If they don't, registers them.
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError != null)
            {
                TempData["ErrorMessage"] = $"Error from Google: {remoteError}";
                return RedirectToAction("Login");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                TempData["ErrorMessage"] = "Failed to load Google account information.";
                return RedirectToAction("Login");
            }

            // Attempt to sign in the user if they've linked this provider already
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("[SECURITY] User logged in via Google Provider: {Email}", MaskEmail(info.Principal.FindFirstValue(ClaimTypes.Email) ?? "unknown"));
                
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                TempData["ErrorMessage"] = "This account has been locked out.";
                return RedirectToAction("Login");
            }

            // User doesn't have an associated login. We must sign them up or link their existing account.
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var fullName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email;

            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Could not retrieve email from your Google account.";
                return RedirectToAction("Login");
            }

            // Check if user already exists
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Register a new user
                user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true // External authentication verified the email already
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    TempData["ErrorMessage"] = "Error creating account via Google.";
                    return RedirectToAction("Login");
                }

                // Assign default role "User" in Identity
                await _userManager.AddToRoleAsync(user, "User");

                // Create custom UserProfile
                var profile = new UserProfile
                {
                    IdentityUserId = user.Id,
                    FullName = fullName,
                    Email = email,
                    PhoneNumber = "",
                    Address = "",
                    City = "",
                    Country = "",
                    CreatedDate = DateTime.Now,
                    ProfilePhotoPath = ""
                };
                _context.UserProfiles.Add(profile);
                await _context.SaveChangesAsync();

                // Create custom User_Role association
                var role = _context.Roles.FirstOrDefault(r => r.Name == "User");
                if (role != null)
                {
                    var customUserRole = new User_Role
                    {
                        UserProfileId = profile.Id,
                        RoleId = role.Id
                    };
                    _context.User_Roles.Add(customUserRole);
                    await _context.SaveChangesAsync();
                }
            }

            // Link Google external login to user account
            var addLoginResult = await _userManager.AddLoginAsync(user, info);
            if (addLoginResult.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation("[SECURITY] User registered and logged in via Google: {Email}", MaskEmail(email));
                
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }

            TempData["ErrorMessage"] = "Failed to link Google account.";
            return RedirectToAction("Login");
        }
    }
}