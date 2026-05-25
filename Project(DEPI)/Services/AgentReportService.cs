using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookifyHotel.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Project_DEPI.Services;

public class AgentReportService : IAgentReportService
{
    private readonly IServiceProvider _serviceProvider;

    public AgentReportService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task LogAsync(string action, Guid userId, object payload)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookifyHotelDbContext>();
        
        var report = new BookifyHotel.Model.AgentReport
        {
            ReportId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Action = action,
            UserId = userId,
            PayloadJson = JsonSerializer.Serialize(payload)
        };

        dbContext.AgentReports.Add(report);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AgentReport>> GetRecentAsync(int count = 50)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookifyHotelDbContext>();
        
        var list = await dbContext.AgentReports
            .OrderByDescending(r => r.Timestamp)
            .Take(count)
            .ToListAsync();

        return list.Select(r => new AgentReport(r.ReportId, r.Timestamp, r.Action, r.UserId, r.PayloadJson)).ToList();
    }
}
