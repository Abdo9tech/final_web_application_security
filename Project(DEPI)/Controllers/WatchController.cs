using Microsoft.AspNetCore.Mvc;

namespace Project_DEPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WatchController : ControllerBase
    {
        private readonly Services.PriceDropService _service;
        public WatchController(Services.PriceDropService service)
        {
            _service = service;
        }

        public record WatchDto(int HotelId, string HotelName, decimal Price, string Email);

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult Post([FromBody] WatchDto dto)
        {
            _service.AddWatch(dto.HotelId, dto.HotelName, dto.Price, dto.Email);
            return Ok(new { success = true });
        }

        [HttpGet]
        public IActionResult Get([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email is required");
            }
            var watches = _service.GetWatches(email);
            return Ok(watches);
        }
    }
}
