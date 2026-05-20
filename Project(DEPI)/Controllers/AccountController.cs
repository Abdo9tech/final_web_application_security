using BookifyHotel.Data;
using BookifyHotel.Model;
using BookifyHotel.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Project_DEPI.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BookifyHotel.Controllers
{
    public class RegisterController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly BookifyHotelDbContext _context;
        private readonly IEmailSender _emailSender;

        public RegisterController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            BookifyHotelDbContext context,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailSender = emailSender;
        }

        // SECURITY [Default Deny Access Strategy]: AccessDenied must be publicly accessible
        // so users who are blocked see the denial page instead of an infinite auth redirect.
        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // SECURITY [Default Deny Access Strategy]: Registration page must be public
        // so new users can create accounts without being logged in.
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // SECURITY [CSRF Protection]: [ValidateAntiForgeryToken] is applied here (and globally)
        // to ensure registration form submissions come from our own site only.
        // SECURITY [Secure Password Hashing]: CreateAsync hashes the password using PBKDF2/HMACSHA256.
        // SECURITY [Default Deny Access Strategy]: Registration must be public.
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel newUser)
        {
            if (!ModelState.IsValid)
                return View(newUser);

            // التحقق إذا كان الإيميل موجود بالفعل
            var existingUser = await _userManager.FindByEmailAsync(newUser.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email is already registered.");
                return View(newUser);
            }

            // إنشاء مستخدم Identity جديد
            var appUser = new AppUser
            {
                UserName = newUser.Email,
                Email = newUser.Email,
                PhoneNumber = newUser.PhoneNumber,
                EmailConfirmed = false // SECURITY: Require email confirmation via SMTP token
            };

            var result = await _userManager.CreateAsync(appUser, newUser.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(newUser);
            }

            try
            {
                // ⭐⭐ **تخزين في جدول UserProfile** ⭐⭐
                var profile = new UserProfile
                {
                    IdentityUserId = appUser.Id,
                    FullName = newUser.FullName,
                    Email = newUser.Email,
                    PhoneNumber = newUser.PhoneNumber,
                    Address = "",
                    City = "",
                    Country = "",
                    CreatedDate = DateTime.Now,
                    ProfilePhotoPath = ""
                };

                _context.UserProfiles.Add(profile);
                await _context.SaveChangesAsync();

                // ⭐⭐ **إضافة Identity Role "User" تلقائياً** ⭐⭐
                await _userManager.AddToRoleAsync(appUser, "User");

                // Get role ID from database and map it in custom User_Roles table
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

                // ⭐⭐ **توليد رمز تأكيد الإيميل وإرساله** ⭐⭐
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(appUser);
                var confirmationLink = Url.Action("ConfirmEmail", "Register", 
                    new { userId = appUser.Id, token = token }, Request.Scheme);

                var emailBody = $@"
                    <div style='font-family: Arial, sans-serif; padding: 25px; border: 1px solid #e0e0e0; border-radius: 8px; max-width: 600px; margin: 20px auto; background-color: #ffffff; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
                        <div style='text-align: center; border-bottom: 2px solid #f2f2f2; padding-bottom: 20px; margin-bottom: 20px;'>
                            <h2 style='color: #003580; margin: 0; font-size: 24px; font-weight: 800;'>Bookify Hotel</h2>
                        </div>
                        <p style='color: #333333; font-size: 16px; line-height: 1.5;'>Hello <strong>{newUser.FullName}</strong>,</p>
                        <p style='color: #333333; font-size: 16px; line-height: 1.5;'>Thank you for registering an account with Bookify Hotel. To activate your account, please confirm your email address by clicking the button below:</p>
                        <div style='text-align: center; margin: 35px 0;'>
                            <a href='{confirmationLink}' style='background-color: #003580; color: #ffffff; padding: 14px 28px; text-decoration: none; border-radius: 4px; font-weight: bold; font-size: 16px; display: inline-block; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>Confirm Email Address</a>
                        </div>
                        <p style='color: #666666; font-size: 12px; line-height: 1.4; border-top: 1px solid #f2f2f2; padding-top: 15px;'>If you did not request this registration, please ignore this email.</p>
                        <p style='color: #999999; font-size: 11px; line-height: 1.4; margin-top: 5px;'>If the button doesn't work, copy and paste the link below into your browser:<br/><a href='{confirmationLink}' style='color: #003580; word-break: break-all;'>{confirmationLink}</a></p>
                    </div>";

                await _emailSender.SendEmailAsync(appUser.Email, "Confirm your email - Bookify Hotel", emailBody);

                TempData["SuccessMessage"] = "Account registered successfully! Please check your email inbox to confirm your email and activate your account.";
                return RedirectToAction("Login", "Login");
            }
            catch (Exception ex)
            {
                // حذف مستخدم Identity لو حصل خطأ
                await _userManager.DeleteAsync(appUser);

                Console.WriteLine($"Error during registration: {ex.Message}");

                ModelState.AddModelError("", "An error occurred during account creation. Please try again.");
                return View(newUser);
            }
        }

        // GET: Register/ConfirmEmail
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Invalid email confirmation parameters.";
                return RedirectToAction("Login", "Login");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User account not found.";
                return RedirectToAction("Login", "Login");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Email confirmed successfully! You can now log in to your account.";
                return RedirectToAction("Login", "Login");
            }

            TempData["ErrorMessage"] = "Email confirmation failed. The link may have expired or is invalid.";
            return RedirectToAction("Login", "Login");
        }
    }
}