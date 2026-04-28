using BookifyHotel.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Project_DEPI.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthCheckController : ControllerBase
    {
        private readonly BookifyHotelDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public HealthCheckController(BookifyHotelDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        [HttpGet("live")]
        public IActionResult Live()
        {
            return Ok(new
            {
                status = "alive",
                service = "bookifyhotel",
                utc = DateTime.UtcNow
            });
        }

        [HttpGet("ready")]
        public async Task<IActionResult> Ready(CancellationToken cancellationToken)
        {
            var dbHealthy = await _dbContext.Database.CanConnectAsync(cancellationToken);
            var redisHealthy = await IsRedisReachableAsync(cancellationToken);

            if (!dbHealthy || !redisHealthy)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "unhealthy",
                    dependencies = new
                    {
                        sqlserver = dbHealthy ? "ok" : "unreachable",
                        redis = redisHealthy ? "ok" : "unreachable"
                    },
                    utc = DateTime.UtcNow
                });
            }

            return Ok(new
            {
                status = "ready",
                dependencies = new
                {
                    sqlserver = "ok",
                    redis = "ok"
                },
                utc = DateTime.UtcNow
            });
        }

        private async Task<bool> IsRedisReachableAsync(CancellationToken cancellationToken)
        {
            var redisConnection =
                _configuration["Redis:ConnectionString"] ??
                _configuration["ConnectionStrings:Redis"] ??
                "redis:6379";

            var hostAndPort = redisConnection.Split(',')[0];
            var parts = hostAndPort.Split(':', StringSplitOptions.RemoveEmptyEntries);
            var host = parts[0];
            var port = parts.Length > 1 && int.TryParse(parts[1], out var parsedPort) ? parsedPort : 6379;

            try
            {
                using var tcpClient = new System.Net.Sockets.TcpClient();
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));
                await tcpClient.ConnectAsync(host, port, timeoutCts.Token);
                return tcpClient.Connected;
            }
            catch
            {
                return false;
            }
        }
    }
}
