using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BookifyHotel.Data;
using BookifyHotel.Model;

namespace Project_DEPI.Controllers
{
    // SECURITY [Sensitive Endpoint Protection / Role-Based Authorization]:
    // The entire FavoriteController requires authentication — anonymous users
    // cannot add or view favorites. This also prevents IDOR (Insecure Direct Object Reference)
    // since favorites are always scoped to the authenticated user's IdentityUserId.
    [Authorize]
    public class FavoriteController : Controller
    {
        private readonly BookifyHotelDbContext _context;

        public FavoriteController(BookifyHotelDbContext context)
        {
            _context = context;
        }

        // GET: Favorite
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(u => u.IdentityUserId == userId);

            if (userProfile == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var favorites = await _context.FavoriteRooms
                .Include(f => f.Room)
                .ThenInclude(r => r.RoomType)
                .Where(f => f.UserProfileId == userProfile.Id)
                .Select(f => f.Room)
                .ToListAsync();

            return View(favorites);
        }

        // SECURITY [CSRF Protection]: Anti-forgery token is globally enforced by Program.cs.
        // SECURITY [Overposting Protection]: Only roomId (an int) is accepted — no other
        // fields can be injected or modified through this endpoint.
        [HttpPost]
        public async Task<IActionResult> Toggle([FromBody] int roomId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(u => u.IdentityUserId == userId);

                if (userProfile == null)
                {
                    return Json(new { success = false, message = "User profile not found." });
                }

                // Validate that the room exists
                var roomExists = await _context.Rooms.AnyAsync(r => r.RoomId == roomId);
                if (!roomExists)
                {
                    return Json(new { success = false, message = "Room not found." });
                }

                var favorite = await _context.FavoriteRooms
                    .FirstOrDefaultAsync(f => f.UserProfileId == userProfile.Id && f.RoomId == roomId);

                if (favorite != null)
                {
                    // Remove from favorites
                    _context.FavoriteRooms.Remove(favorite);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, action = "removed" });
                }
                else
                {
                    // Add to favorites
                    var newFavorite = new FavoriteRoom
                    {
                        UserProfileId = userProfile.Id,
                        RoomId = roomId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.FavoriteRooms.Add(newFavorite);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, action = "added" });
                }
            }
            catch (Exception)
            {
                // Log the exception
                return Json(new { success = false, message = "An error occurred while updating favorites." });
            }
        }
    }
}
