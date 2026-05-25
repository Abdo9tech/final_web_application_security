using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Project_DEPI.Services;
public interface IAgentReportService
{
    Task LogAsync(string action, Guid userId, object payload);
    Task<IReadOnlyList<AgentReport>> GetRecentAsync(int count = 50);
}
public record AgentReport(Guid Id, DateTime Timestamp, string Action, Guid UserId, object Payload);
