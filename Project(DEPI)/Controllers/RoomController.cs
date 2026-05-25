using BookifyHotel.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PLL.Services;
using RoomService = PLL.Services.RoomService;
using System;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Project_DEPI_.Controllers
{
    public class RoomController : Controller
    {
        private readonly RoomService _roomService;
        private readonly RoomTypeService _roomTypeService;
        private readonly BookifyHotel.Data.BookifyHotelDbContext _context;

        public RoomController(RoomService roomService, RoomTypeService roomTypeService, BookifyHotel.Data.BookifyHotelDbContext context)
        {
            _roomService = roomService ?? throw new ArgumentNullException(nameof(roomService));
            _roomTypeService = roomTypeService ?? throw new ArgumentNullException(nameof(roomTypeService));
            _context = context;
        }

        #region User Side Operations
        // SECURITY [Default Deny Access Strategy]: Public room listing is intentionally
        // accessible to anonymous visitors. [AllowAnonymous] overrides the global
        // FallbackPolicy that requires authentication for all other routes.
        [AllowAnonymous]
        public async Task<IActionResult> Index(
            string? location, 
            DateTime? checkIn, 
            DateTime? checkOut, 
            int? guests,
            int? roomTypeId,
            int? floor,
            decimal? minPrice,
            decimal? maxPrice,
            bool? isAvailable,
            string? status,
            string[]? budget,
            string[]? amenity,
            int[]? stars)
        {
            try
            {
                ViewBag.RoomTypes = _roomTypeService.GetAll() ?? new List<RoomType>();
                IEnumerable<Room> rooms;

                // Start with base search
                if (!string.IsNullOrEmpty(location) || (checkIn.HasValue && checkOut.HasValue) || (guests.HasValue && guests.Value > 0))
                {
                    rooms = _roomService.SearchRooms(location ?? string.Empty, checkIn, checkOut, guests);
                }
                else
                {
                    // Eager load RoomType for GetAll to avoid null references in filters
                    rooms = _context.Rooms.Include(r => r.RoomType).ToList();
                }

                // Apply additional filters
                if (roomTypeId.HasValue && roomTypeId.Value > 0)
                {
                    rooms = rooms.Where(r => r.RoomTypeId == roomTypeId.Value);
                }

                if (floor.HasValue && floor.Value > 0)
                {
                    rooms = rooms.Where(r => r.Floor == floor.Value);
                }

                if (minPrice.HasValue && minPrice.Value > 0)
                {
                    rooms = rooms.Where(r => r.RoomType != null && r.RoomType.PricePerNight >= minPrice.Value);
                }

                if (maxPrice.HasValue && maxPrice.Value > 0)
                {
                    rooms = rooms.Where(r => r.RoomType != null && r.RoomType.PricePerNight <= maxPrice.Value);
                }

                if (isAvailable.HasValue)
                {
                    rooms = rooms.Where(r => r.IsAvailable == isAvailable.Value);
                }

                if (!string.IsNullOrEmpty(status))
                {
                    rooms = rooms.Where(r => r.Status == status);
                }

                // Budget Filter (Handle ranges like "0-50", "50-100")
                if (budget != null && budget.Any())
                {
                    rooms = rooms.Where(r => {
                        var price = r.RoomType?.PricePerNight ?? 0;
                        return budget.Any(b => {
                            var parts = b.Split('-');
                            if (parts.Length == 2 && decimal.TryParse(parts[0], out var min) && decimal.TryParse(parts[1], out var max))
                            {
                                return price >= min && price <= max;
                            }
                            return false;
                        });
                    });
                }

                // Amenity Filter (Simplified: check if room type name contains or if it's a specific logic)
                if (amenity != null && amenity.Any())
                {
                    rooms = rooms.Where(r => {
                        var desc = (r.RoomType?.Description ?? "").ToLower();
                        return amenity.Any(a => desc.Contains(a.ToLower()));
                    });
                }

                // Stars Filter
                if (stars != null && stars.Any())
                {
                    // Assuming Room has a StarRating or we map it to RoomType
                    // For now, let's filter by RoomType name if it contains "Star" or similar, 
                    // or just leave a placeholder if the model doesn't have Stars yet.
                    // rooms = rooms.Where(r => stars.Contains(r.StarRating));
                }

                return await LoadIndexView(rooms);
            }
            catch (Exception)
            {
                // Log the exception
                ModelState.AddModelError("", "An error occurred while retrieving room data.");
                return View(new List<Room>());
            }
        }

        private async Task<IActionResult> LoadIndexView(IEnumerable<Room> rooms)
        {
            var favoriteRoomIds = new List<int>();
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(u => u.IdentityUserId == userId);
                if (userProfile != null)
                {
                    favoriteRoomIds = await _context.FavoriteRooms
                        .Where(f => f.UserProfileId == userProfile.Id)
                        .Select(f => f.RoomId)
                        .ToListAsync();
                }
            }
            ViewBag.FavoriteRoomIds = favoriteRoomIds;
            return View(rooms.ToList());
        }

        // SECURITY [Default Deny Access Strategy]: Room details page is intentionally public.
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Invalid room ID");
                }

                ViewBag.RoomTypes = _roomTypeService.GetAll() ?? new List<RoomType>();
                var room = _roomService.GetById(id);
                
                if (room == null)
                {
                    return NotFound();
                }

                bool isFavorited = false;
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(u => u.IdentityUserId == userId);
                    if (userProfile != null)
                    {
                        isFavorited = await _context.FavoriteRooms.AnyAsync(f => f.UserProfileId == userProfile.Id && f.RoomId == id);
                    }
                }
                ViewBag.IsFavorited = isFavorited;

                return View(room);
            }
            catch (Exception)
            {
                // Log the exception
                ModelState.AddModelError("", "An error occurred while retrieving room details.");
                return RedirectToAction(nameof(Index));
            }
        }
        #endregion

        #region Admin Dashboard CRUD Operations
        // SECURITY [Role-Based Authorization]: Admin/Manager-only access to room admin details.
        // Only users with Admin or Manager role can view this protected panel.
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Manager")]
        public IActionResult DetailsToAdmin(int id)
        {    
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Invalid room ID");
                }

                var room = _roomService.GetById(id);
                if (room == null)
                {
                    return NotFound();
                }

                ViewBag.RoomTypes = _roomTypeService.GetAll() ?? new List<RoomType>();
                return View(room);
            }
            catch (Exception)
            {
                // Log the exception
                ModelState.AddModelError("", "An error occurred while retrieving room details.");
                return RedirectToAction(nameof(Index2));
            }
        }

        // SECURITY [Role-Based Authorization]: Admin/Manager-only access to room management list.
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Manager")]
        public IActionResult Index2(
            int? roomNumber,
            int? roomTypeId,
            int? floor,
            string? status)    // Admin View with Simple Filters
        {
            try
            {
                ViewBag.RoomTypes = _roomTypeService.GetAll() ?? new List<RoomType>();
                
                // Use _context directly to ensure RoomType is eagerly loaded for the Admin view
                IEnumerable<Room> rooms = _context.Rooms.Include(r => r.RoomType).ToList();

                // Apply filters
                if (roomNumber.HasValue && roomNumber.Value > 0)
                {
                    rooms = rooms.Where(r => r.RoomNumber == roomNumber.Value);
                }

                if (roomTypeId.HasValue && roomTypeId.Value > 0)
                {
                    rooms = rooms.Where(r => r.RoomTypeId == roomTypeId.Value);
                }

                if (floor.HasValue && floor.Value > 0)
                {
                    rooms = rooms.Where(r => r.Floor == floor.Value);
                }

                if (!string.IsNullOrEmpty(status))
                {
                    rooms = rooms.Where(r => r.Status == status);
                }

                return View(rooms.ToList());
            }
            catch (Exception)
            {
                // Log the exception
                ViewBag.RoomTypes = _roomTypeService.GetAll() ?? new List<RoomType>();
                return View(new List<Room>());
            }
        }

        // SECURITY [Role-Based Authorization]: Only Admin/Manager can add new rooms.
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Manager")]
        public IActionResult Add()
        {
            try
            {
                var roomTypes = _roomTypeService.GetAll() ?? new List<RoomType>();
                
                if (!roomTypes.Any())
                {
                    TempData["Warning"] = "⚠️ Please create room types first before adding rooms.";
                    return RedirectToAction("Index", "RoomType");
                }
                
                ViewBag.RoomTypes = roomTypes;
                return View(new Room { IsAvailable = true, Status = "Available" });
            }
            catch (Exception ex)
            {
                // Log the exception
                ModelState.AddModelError("", $"An error occurred while loading the add room form: {ex.Message}");
                ViewBag.RoomTypes = _roomTypeService.GetAll() ?? new List<RoomType>();
                return View(new Room());
            }
        }

        // GET: Redirect to Add page if accessed directly
        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Manager")]
        public IActionResult AddSave()
        {
            TempData["Info"] = "Please use the form to create a new room.";
            return RedirectToAction(nameof(Add));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Manager")]
        public IActionResult AddSave(Room room)
        {
            try
            {
                // Debug: Log received data
                Console.WriteLine($"AddSave called - RoomNumber: {room?.RoomNumber}, Floor: {room?.Floor}, RoomTypeId: {room?.RoomTypeId}");

                // Remove navigation property validation errors (these are populated by EF, not the form)
                ModelState.Remove("RoomType");
                ModelState.Remove("RoomTypes");
                ModelState.Remove("Bookings");
                ModelState.Remove("ReservationCarts");

                // Check if room object is null
                if (room == null)
                {
                    ModelState.AddModelError("", "Room data is missing. Please fill in all fields.");
                    ViewBag.RoomTypes = _roomTypeService.GetAll() ?? new List<RoomType>();
                    return View("Add", new Room { IsAvailable = true, Status = "Available" });
                }

                // Validate required fields
                if (room.RoomNumber <= 0)
                {
                    ModelState.AddModelError("RoomNumber", "Room number must be greater than 0");
                }

                if (room.Floor < 0)
                {
                    ModelState.AddModelError("Floor", "Floor number cannot be negative");
                }

                if (room.RoomTypeId <= 0)
                {
                    ModelState.AddModelError("RoomTypeId", "Please select a valid room type");
                }

                if (string.IsNullOrWhiteSpace(room.Status))
                {
                    ModelState.AddModelError("Status", "Please select a room status");
                }

                // Set default ImageUrl if empty
                if (string.IsNullOrWhiteSpace(room.ImageUrl))
                {
                    room.ImageUrl = "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?q=80&w=1000&auto=format&fit=crop";
                }

                // Force location to Manchester for consistency
                if (string.IsNullOrWhiteSpace(room.Location))
                {
                    room.Location = "Manchester, City Centre";
                }

                // Check ModelState
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    Console.WriteLine($"Validation errors: {string.Join(", ", errors)}");
                    
                    ViewBag.RoomTypes = _roomTypeService.GetAll() ?? new List<RoomType>();
                    TempData["Error"] = "Please fix the validation errors and try again.";
                    return View("Add", room);
                }

                // Check if room number already exists
                var existingRoom = _roomService.GetAll()?.FirstOrDefault(r => r.RoomNumber == room.RoomNumber);
                if (existingRoom != null)
                {
                    ModelState.AddModelError("RoomNumber", $"Room number {room.RoomNumber} already exists");
                    ViewBag.RoomTypes = _roomTypeService.GetAll() ?? new List<RoomType>();
                    TempData["Error"] = $"Room number {room.RoomNumber} already exists!";
                    return View("Add", room);
                }

                // Create the room
                Console.WriteLine($"Creating room {room.RoomNumber}...");
                _roomService.Create(room);
                Console.WriteLine($"Room {room.RoomNumber} created successfully!");
                
                TempData["Success"] = $"✅ Room {room.RoomNumber} created successfully!";
                return RedirectToAction(nameof(Index2));
            }
            catch (Exception ex)
            {
                // Log the exception with details
                Console.WriteLine($"Error creating room: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                ModelState.AddModelError("", $"❌ Error creating room: {ex.Message}");
                if (ex.InnerException != null)
                {
                    ModelState.AddModelError("", $"Details: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                ViewBag.RoomTypes = _roomTypeService.GetAll() ?? new List<RoomType>();
                TempData["Error"] = $"Error creating room: {ex.Message}";
                return View("Add", room ?? new Room { IsAvailable = true, Status = "Available" });
            }
        }

        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Manager")]
        public IActionResult Edit(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["Error"] = "Invalid room ID";
                    return RedirectToAction(nameof(Index2));
                }

                var room = _roomService.GetById(id);
                if (room == null)
                {
                    TempData["Error"] = "Room not found";
                    return RedirectToAction(nameof(Index2));
                }

                ViewBag.RoomTypes = _roomTypeService.GetAll() ?? new List<RoomType>();
                return View(room);
            }
            catch (Exception ex)
            {
                // Log the exception
                TempData["Error"] = $"An error occurred while loading the edit form: {ex.Message}";
                return RedirectToAction(nameof(Index2));
            }
        }

        // GET: Redirect to room list if accessed directly
        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Manager")]
        public IActionResult EditSave()
        {
            TempData["Info"] = "Please select a room to edit.";
            return RedirectToAction(nameof(Index2));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Manager")]
        public IActionResult EditSave(Room room)
        {
            try
            {
                // Remove navigation property validation errors
                ModelState.Remove("RoomType");
                ModelState.Remove("RoomTypes");
                ModelState.Remove("Bookings");
                ModelState.Remove("ReservationCarts");

                // Validate required fields
                if (room.RoomNumber <= 0)
                {
                    ModelState.AddModelError("RoomNumber", "Room number must be greater than 0");
                }

                if (room.Floor < 0)
                {
                    ModelState.AddModelError("Floor", "Floor number cannot be negative");
                }

                if (room.RoomTypeId <= 0)
                {
                    ModelState.AddModelError("RoomTypeId", "Please select a valid room type");
                }

                if (string.IsNullOrWhiteSpace(room.Status))
                {
                    ModelState.AddModelError("Status", "Please select a room status");
                }

                // Set default ImageUrl if empty
                if (string.IsNullOrWhiteSpace(room.ImageUrl) || room.ImageUrl == "/images/default-room.jpg")
                {
                    room.ImageUrl = "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?q=80&w=1000&auto=format&fit=crop";
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.RoomTypes = _roomTypeService.GetAll() ?? new List<RoomType>();
                    return View("Edit", room);
                }

                // Check if room number already exists (excluding current room)
                var existingRoom = _roomService.GetAll()?.FirstOrDefault(r => r.RoomNumber == room.RoomNumber && r.RoomId != room.RoomId);
                if (existingRoom != null)
                {
                    ModelState.AddModelError("RoomNumber", $"Room number {room.RoomNumber} already exists");
                    ViewBag.RoomTypes = _roomTypeService.GetAll() ?? new List<RoomType>();
                    return View("Edit", room);
                }

                _roomService.Update(room);
                TempData["Success"] = $"✅ Room {room.RoomNumber} updated successfully!";
                return RedirectToAction(nameof(Index2));
            }
            catch (Exception ex)
            {
                // Log the exception with details
                ModelState.AddModelError("", $"❌ Error updating room: {ex.Message}");
                if (ex.InnerException != null)
                {
                    ModelState.AddModelError("", $"Details: {ex.InnerException.Message}");
                }
                ViewBag.RoomTypes = _roomTypeService.GetAll() ?? new List<RoomType>();
                return View("Edit", room);
            }
        }

        // SECURITY [Role-Based Authorization]: Only Admin can delete rooms — most destructive operation.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Invalid room ID");
                }

                var room = _roomService.GetById(id);
                if (room == null)
                {
                    return NotFound();
                }

                _roomService.Delete(id);
                return RedirectToAction(nameof(Index2));
            }
            catch (Exception)
            {
                // Log the exception
                ModelState.AddModelError("", "An error occurred while deleting the room.");
                return RedirectToAction(nameof(Index2));
            }
        }

        #endregion

    }
}
