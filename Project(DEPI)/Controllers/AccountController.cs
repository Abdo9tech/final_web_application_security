using BookifyHotel.Data;
using BookifyHotel.Model;
using BookifyHotel.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BookifyHotel.Controllers
{
    public class RegisterController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly BookifyHotelDbContext _context;

        public RegisterController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            BookifyHotelDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
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
                EmailConfirmed = true // يمكنك تغيير هذا إذا أردت تأكيد الإيميل
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

                // ⭐⭐ **تسجيل الدخول تلقائياً** ⭐⭐
                await _signInManager.SignInAsync(appUser, isPersistent: false);

                TempData["SuccessMessage"] = "Account created successfully! Welcome to BookifyHotel.";

                // توجيه المستخدم العادي إلى Home
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                // حذف مستخدم Identity لو حصل خطأ
                await _userManager.DeleteAsync(appUser);

                Console.WriteLine($"Error during registration: {ex.Message}");

                ModelState.AddModelError("", "حدث خطأ أثناء إنشاء الحساب، حاول مرة أخرى.");
                return View(newUser);
            }
        }
    }
}