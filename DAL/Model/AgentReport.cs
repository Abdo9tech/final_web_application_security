using System;

namespace BookifyHotel.Model
{
    public class AgentReport
    {
        public int Id { get; set; }
        public Guid ReportId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Action { get; set; }
        public Guid UserId { get; set; }
        public string PayloadJson { get; set; }
    }
}
