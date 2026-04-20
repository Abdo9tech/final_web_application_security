using BookifyHotel.Data;
using BookifyHotel.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BookifyHotel.Controllers
{
    // SECURITY [Admin Panel Protection / Role-Based Authorization]: The entire AdminController
    // is restricted to the 'Admin' role. Any unauthenticated or lower-privileged
    // user attempting access is redirected to the AccessDenied page.
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly BookifyHotelDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        // SECURITY [Security Event Logging]: Logger records sensitive admin actions
        // (role changes, user management) for audit and incident response.
        private readonly ILogger<AdminController> _logger;

        public AdminController(BookifyHotelDbContext context, UserManager<AppUser> userManager, ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // لوحة تحكم المدير
        public async Task<IActionResult> Dashboard()
        {
            var stats = new
            {
                TotalUsers = await _context.UserProfiles.CountAsync(),
                TotalBookings = await _context.Bookings.CountAsync(),
                //TodayBookings = await _context.Bookings
                //    .Where(b => b.CreatedDate.Date == DateTime.Today)
                //    .CountAsync(),
                //ActiveUsers = await _context.Users.CountAsync()
            };

            ViewBag.Stats = stats;
            return View();
        }

        // إدارة المستخدمين
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _context.UserProfiles
                .Include(u => u.User)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.PhoneNumber,
                    u.IdentityUserId,
                  //  RegistrationDate = u.CreatedDate
                })
                .ToListAsync();

            var userList = new List<dynamic>();

            foreach (var user in users)
            {
                var appUser = await _userManager.FindByIdAsync(user.IdentityUserId);
                var roles = appUser != null ? await _userManager.GetRolesAsync(appUser) : new List<string>();

                userList.Add(new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.PhoneNumber,
                    user.IdentityUserId,
                    Role = roles.FirstOrDefault() ?? "User",
                    //user.RegistrationDate
                });
            }

            return View(userList);
        }

        // SECURITY [CSRF Protection]: [ValidateAntiForgeryToken] is present here (and globally)
        // to prevent cross-site request forgery attacks on this sensitive role-change operation.
        // SECURITY [Security Event Logging]: Role changes are logged with who performed the action,
        // who was affected, and what the new role is — critical for audit trails.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // SECURITY [Role-Based Authorization]: Only valid, known roles can be assigned.
            // This prevents privilege escalation via unexpected role name injection.
            var allowedRoles = new[] { "Admin", "Manager", "Receptionist", "User" };
            if (!allowedRoles.Contains(role))
            {
                _logger.LogWarning("[SECURITY] Admin attempted to assign invalid role '{Role}' to user {UserId}.", role, userId);
                TempData["ErrorMessage"] = "Invalid role specified.";
                return RedirectToAction(nameof(ManageUsers));
            }

            // إزالة جميع الأدوار الحالية
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // إضافة الدور الجديد
            await _userManager.AddToRoleAsync(user, role);

            // SECURITY [Security Event Logging]: Log the role change for audit purposes.
            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
            _logger.LogInformation("[SECURITY] Admin {AdminId} changed role of user {TargetUserId} from [{OldRoles}] to '{NewRole}'.",
                performedBy, userId, string.Join(", ", currentRoles), role);

            TempData["SuccessMessage"] = $"User role updated to {role} successfully.";
            return RedirectToAction(nameof(ManageUsers));
        }

        // SECURITY [CSRF Protection]: [ValidateAntiForgeryToken] protects password change from CSRF attacks.
        // SECURITY [Password Security]: Passwords are hashed by ASP.NET Core Identity (never stored in plain text).
        // SECURITY [Security Event Logging]: Password changes are logged for audit trail.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserPassword(string userId, string newPassword, string confirmPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(ManageUsers));
            }

            // Validate passwords match
            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match.";
                return RedirectToAction(nameof(ManageUsers));
            }

            // Validate password is not empty
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["ErrorMessage"] = "Password cannot be empty.";
                return RedirectToAction(nameof(ManageUsers));
            }

            // Remove current password and set new one
            var removePasswordResult = await _userManager.RemovePasswordAsync(user);
            if (!removePasswordResult.Succeeded)
            {
                TempData["ErrorMessage"] = "Failed to remove current password.";
                _logger.LogError("[SECURITY] Failed to remove password for user {UserId}: {Errors}", 
                    userId, string.Join(", ", removePasswordResult.Errors.Select(e => e.Description)));
                return RedirectToAction(nameof(ManageUsers));
            }

            var addPasswordResult = await _userManager.AddPasswordAsync(user, newPassword);
            if (!addPasswordResult.Succeeded)
            {
                var errors = string.Join(", ", addPasswordResult.Errors.Select(e => e.Description));
                TempData["ErrorMessage"] = $"Password change failed: {errors}";
                _logger.LogWarning("[SECURITY] Password change failed for user {UserId}: {Errors}", userId, errors);
                return RedirectToAction(nameof(ManageUsers));
            }

            // SECURITY [Security Event Logging]: Log the password change for audit purposes.
            var performedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
            _logger.LogInformation("[SECURITY] Admin {AdminId} changed password for user {TargetUserId}.",
                performedBy, userId);

            TempData["SuccessMessage"] = $"Password for user has been successfully updated.";
            return RedirectToAction(nameof(ManageUsers));
        }
    }
}