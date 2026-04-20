using DAL.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PLL.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Project_DEPI_.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ProfileService _profileService;
        private readonly UserManager<AppUser> _userManager;

        public ProfileController(ProfileService profileService, UserManager<AppUser> userManager)
        {
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var model = await _profileService.GetUserProfileAsync(userId);
                return View(model);
            }
            catch (Exception)
            {
                // Log the exception
                ModelState.AddModelError("", "An error occurred while loading your profile.");
                return View(new ProfileViewModel());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var success = await _profileService.UpdateProfileAsync(userId, model);
                if (!success)
                {
                    ModelState.AddModelError("", "Failed to update profile. Please try again.");
                    return View("Index", model);
                }

                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                // Log the exception
                ModelState.AddModelError("", "An error occurred while updating your profile.");
                return View("Index", model);
            }
        }
    }
}
