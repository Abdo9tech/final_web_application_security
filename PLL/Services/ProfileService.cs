using DAL.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace PLL.Services
{
    public class ProfileService
    {
        private readonly UserManager<AppUser> _userManager;

        public ProfileService(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }
        public override bool Equals(object? obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public async Task<ProfileViewModel> GetUserProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            return new ProfileViewModel
            {
                FullName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                // Add ProfilePhotoPath to AppUser if needed
            };
        }

        public override string? ToString()
        {
            return base.ToString();
        }

        public async Task<bool> UpdateProfileAsync(string userId, ProfileViewModel model)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            // Update user properties
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.UserName = model.FullName;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }
    }
}