using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BookifyHotel.Data;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Project_DEPI.Controllers
{
    [Route("aiAssistant")]
    [AllowAnonymous]
    public class AIAssistantController : Controller
    {
        private readonly BookifyHotelDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIAssistantController> _logger;
        private readonly PLL.Services.RoomService _roomService;

        public AIAssistantController(
            BookifyHotelDbContext context,
            IConfiguration configuration,
            ILogger<AIAssistantController> logger,
            PLL.Services.RoomService roomService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _roomService = roomService;
        }

        // AI Assistant Razor view for normal users
        [HttpGet]
        public IActionResult Index()
        {
            return View("~/Views/AIAssistant/AIAssistant.cshtml");
        }

        public class ChatRequest
        {
            public string Message { get; set; }
        }

        // Backend AI Chatbot endpoint powered by real Gemini API with a smart fallback
        [HttpPost("ai-chat")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AIChat([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message is required" });
            }

            try
            {
                // 1. Get available rooms to feed as contextual grounding
                var rooms = _roomService.GetAvailableRooms();
                var roomsContext = rooms.Select(r => new {
                    id = r.RoomId,
                    name = $"{r.RoomType?.Name ?? "Room"} #{r.RoomNumber}",
                    price = r.RoomType?.PricePerNight ?? 0,
                    location = r.Location,
                    description = r.RoomType?.Description ?? "WiFi, TV, AC",
                    capacity = r.RoomType?.Capacity ?? 2
                }).ToList();

                var roomsJson = System.Text.Json.JsonSerializer.Serialize(roomsContext);

                // 2. Read Gemini configuration
                var apiKey = _configuration["Gemini:ApiKey"];
                bool useGemini = !string.IsNullOrEmpty(apiKey) && !apiKey.StartsWith("YOUR_");

                if (useGemini)
                {
                    try
                    {
                        using var httpClient = new HttpClient();
                        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";

                        var systemInstruction = "You are the Bookify Hotel Assistant. Help the user find and book the best rooms in our hotel. Be professional, friendly, and helpful. Always use the provided room listings to answer the user's questions about rooms. Do not make up rooms that are not in the list. Provide the response as a JSON object with two fields: 'response' (your markdown-formatted answer to the user) and 'recommendedRoomIds' (an array of integers representing the IDs of the rooms from the list that you recommend, or an empty array if none apply). Do not wrap the JSON in ```json markdown formatting, just return raw JSON.";

                        var requestBody = new
                        {
                            contents = new[]
                            {
                                new {
                                    role = "user",
                                    parts = new[] {
                                        new { text = $"System Instructions: {systemInstruction}\n\nAvailable Rooms: {roomsJson}\n\nUser Question: {request.Message}" }
                                    }
                                }
                            },
                            generationConfig = new
                            {
                                responseMimeType = "application/json"
                            }
                        };

                        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");
                        var response = await httpClient.PostAsync(url, content);

                        if (response.IsSuccessStatusCode)
                        {
                            var responseString = await response.Content.ReadAsStringAsync();
                            using var doc = System.Text.Json.JsonDocument.Parse(responseString);
                            var textResponse = doc.RootElement
                                .GetProperty("candidates")[0]
                                .GetProperty("content")
                                .GetProperty("parts")[0]
                                .GetProperty("text")
                                .GetString();

                            // Clean and sanitize textResponse in case the model returned markdown wrapped JSON
                            var cleanedText = (textResponse ?? string.Empty).Trim();
                            if (cleanedText.StartsWith("```"))
                            {
                                var firstLineEnd = cleanedText.IndexOf('\n');
                                if (firstLineEnd != -1)
                                {
                                    cleanedText = cleanedText.Substring(firstLineEnd + 1);
                                }
                                var lastBackticks = cleanedText.LastIndexOf("```");
                                if (lastBackticks != -1)
                                {
                                    cleanedText = cleanedText.Substring(0, lastBackticks);
                                }
                                cleanedText = cleanedText.Trim();
                            }

                            // Parse the model's inner JSON
                            using var innerDoc = System.Text.Json.JsonDocument.Parse(cleanedText);
                            var modelReply = innerDoc.RootElement.GetProperty("response").GetString();
                            
                            var recIdsElement = innerDoc.RootElement.GetProperty("recommendedRoomIds");
                            var recommendedRoomIds = new List<int>();
                            if (recIdsElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                foreach (var item in recIdsElement.EnumerateArray())
                                {
                                    if (item.ValueKind == System.Text.Json.JsonValueKind.Number && item.TryGetInt32(out var id))
                                    {
                                        recommendedRoomIds.Add(id);
                                    }
                                }
                            }

                            var recommendedRooms = rooms
                                .Where(r => recommendedRoomIds.Contains(r.RoomId))
                                .Select(r => new {
                                    id = r.RoomId,
                                    name = $"{r.RoomType?.Name ?? "Room"} #{r.RoomNumber}",
                                    price = r.RoomType?.PricePerNight ?? 0,
                                    img = string.IsNullOrEmpty(r.ImageUrl) 
                                        ? "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?q=80&w=1000&auto=format&fit=crop" 
                                        : r.ImageUrl,
                                    location = r.Location
                                }).ToList();

                            return Json(new {
                                reply = modelReply,
                                recommendedRooms = recommendedRooms
                            });
                        }
                        else
                        {
                            var errStr = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"[Gemini API HTTP Error]: Status {response.StatusCode} | Details: {errStr}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Gemini Error] Fallback to local matcher: {ex.Message}\nStack: {ex.StackTrace}");
                    }
                }

                // Fallback: Smart local rule-based matching if Gemini is not configured or fails
                string userMsgLower = request.Message.ToLower();
                string replyText = "";
                List<BookifyHotel.Model.Room> matchedRooms = new List<BookifyHotel.Model.Room>();

                if (userMsgLower.Contains("deluxe"))
                {
                    matchedRooms = rooms.Where(r => (r.RoomType?.Name ?? "").ToLower().Contains("deluxe")).ToList();
                    replyText = matchedRooms.Any() 
                        ? $"Here are the Deluxe rooms currently available at Bookify Hotel:" 
                        : "I'm sorry, we do not have any Deluxe rooms available right now.";
                }
                else if (userMsgLower.Contains("suite"))
                {
                    matchedRooms = rooms.Where(r => (r.RoomType?.Name ?? "").ToLower().Contains("suite")).ToList();
                    replyText = matchedRooms.Any() 
                        ? "Here are the Suite rooms we currently offer:" 
                        : "We don't have any suites available at the moment.";
                }
                else if (userMsgLower.Contains("standard"))
                {
                    matchedRooms = rooms.Where(r => (r.RoomType?.Name ?? "").ToLower().Contains("standard")).ToList();
                    replyText = matchedRooms.Any() 
                        ? "These are our available Standard rooms:" 
                        : "There are no Standard rooms listed right now.";
                }
                else if (userMsgLower.Contains("manchester"))
                {
                    matchedRooms = rooms.Where(r => r.Location.ToLower().Contains("manchester")).ToList();
                    replyText = matchedRooms.Any() 
                        ? $"I found {matchedRooms.Count} room(s) located in Manchester, City Centre:" 
                        : "We do not have any rooms in Manchester at the moment.";
                }
                else if (userMsgLower.Contains("under") || userMsgLower.Contains("cheap") || userMsgLower.Contains("less than"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(userMsgLower, @"\d+");
                    int limit = match.Success ? int.Parse(match.Value) : 150;
                    matchedRooms = rooms.Where(r => (r.RoomType?.PricePerNight ?? 0) <= limit).ToList();
                    replyText = matchedRooms.Any() 
                        ? $"Here are the comfortable rooms under ${limit} per night:" 
                        : $"We do not have any rooms under ${limit} per night.";
                }
                else
                {
                    replyText = "Hello! I am your AI assistant. You can ask me to search for Deluxe rooms, Suites, rooms under a certain budget (e.g., 'under $250'), or in specific locations (like Manchester). How can I help you today?";
                }

                var finalMatched = matchedRooms.Select(r => new {
                    id = r.RoomId,
                    name = $"{r.RoomType?.Name ?? "Room"} #{r.RoomNumber}",
                    price = r.RoomType?.PricePerNight ?? 0,
                    img = string.IsNullOrEmpty(r.ImageUrl) 
                        ? "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?q=80&w=1000&auto=format&fit=crop" 
                        : r.ImageUrl,
                    location = r.Location
                }).ToList();

                return Json(new {
                    reply = replyText,
                    recommendedRooms = finalMatched
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Dashboard Controller Outer Error]: {ex}");
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
