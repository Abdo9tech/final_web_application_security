using Microsoft.AspNetCore.Mvc;
using Project_DEPI.Services;
[ApiController]
[Route("api/[controller]")]
public class AgentReportController : ControllerBase
{
    private readonly IAgentReportService _svc;
    public AgentReportController(IAgentReportService svc) => _svc = svc;
    [HttpGet("report")]
    public async Task<IActionResult> Get([FromQuery]int count = 50) => Ok(await _svc.GetRecentAsync(count));
}
