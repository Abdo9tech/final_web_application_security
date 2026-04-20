using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Project_DEPI_.Models;

namespace Project_DEPI_.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly PLL.Services.RoomService _roomService;

        public HomeController(ILogger<HomeController> logger, PLL.Services.RoomService roomService)
        {
            _logger = logger;
            _roomService = roomService;
        }

        // SECURITY [Default Deny Access Strategy]: [AllowAnonymous] is required here because the global
        // FallbackPolicy in Program.cs requires authentication for all endpoints by default.
        // The home page is intentionally public.
        [AllowAnonymous]
        public IActionResult Index()
        {
            try
            {
                // Get only 6 rooms for better performance
                var rooms = _roomService.GetAvailableRooms()?.Take(6) ?? Enumerable.Empty<BookifyHotel.Model.Room>();
                var viewModel = new HomeViewModel
                {
                    FeaturedRooms = rooms
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page");
                // Return view with empty rooms list
                return View(new HomeViewModel { FeaturedRooms = Enumerable.Empty<BookifyHotel.Model.Room>() });
            }
        }

        // SECURITY [Default Deny Access Strategy]: Privacy page is intentionally public.
        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        // SECURITY [Default Deny Access Strategy]: Error page must be public so unauthenticated
        // users also see a proper error page rather than being redirect-looped.
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
