using System.Collections.Generic;
using Project_DEPI.Models;

namespace Project_DEPI.Services;
public class StubRoomService
{
    // Simple stub for searching rooms. In a real implementation this would query a DB.
    public IEnumerable<RoomDto> SearchRooms(string query, object? param1, object? param2, object? param3)
    {
        // Return empty list for now; replace with actual search logic later.
        return new List<RoomDto>();
    }
}
