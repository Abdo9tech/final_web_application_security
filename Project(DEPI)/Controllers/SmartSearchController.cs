using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Project_DEPI.Models;
using Project_DEPI.Services;

namespace Project_DEPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SmartSearchController : ControllerBase
    {
        private readonly ISmartSearchService _service;
        public SmartSearchController(ISmartSearchService service) => _service = service;

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] Guid userId)
        {
            var rooms = await _service.SearchAsync(query ?? string.Empty, userId);
            var recent = await _service.GetUserHistoryAsync(userId);
            var top = await _service.GetGlobalTopAsync();
            return Ok(new { rooms, recent, top });
        }
    }
}
