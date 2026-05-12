using BookifyHotel.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

public class DbCheck
{
    public static async Task Run(BookifyHotelDbContext context)
    {
        Console.WriteLine("--- DB CHECK ---");
        var rooms = await context.Rooms.Include(r => r.RoomType).ToListAsync();
        Console.WriteLine($"Total Rooms: {rooms.Count}");
        foreach (var r in rooms)
        {
            Console.WriteLine($"Room {r.RoomNumber}: Available={r.IsAvailable}, Status={r.Status}, Location='{r.Location}', Capacity={r.RoomType?.Capacity}");
        }
        Console.WriteLine("----------------");
    }
}
