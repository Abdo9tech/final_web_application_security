using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BookifyHotel.Data;
using Project_DEPI.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Project_DEPI.Services;

public class SmartSearchService : ISmartSearchService
{
    private readonly PLL.Services.RoomService _roomService;
    private readonly IServiceProvider _serviceProvider;

    public SmartSearchService(PLL.Services.RoomService roomService, IServiceProvider serviceProvider)
    {
        _roomService = roomService;
        _serviceProvider = serviceProvider;
    }

    public async Task<IEnumerable<RoomDto>> SearchAsync(string query, Guid userId)
    {
        // 1. Search rooms
        var rooms = await Task.FromResult(_roomService.SearchRooms(query, null, null, null));
        var roomDtos = rooms.Select(r => new RoomDto {
            Id = r.RoomId,
            RoomNumber = r.RoomNumber.ToString(),
            Type = r.RoomType?.Name ?? "Standard",
            Price = r.RoomType?.PricePerNight ?? 0,
            IsAvailable = r.IsAvailable
        });
        
        // 2. Update user history (last 3)
        await UpdateUserHistoryAsync(userId, query);
        
        // 3. Increment global counter
        await IncrementGlobalCounterAsync(query);
        
        return roomDtos;
    }

    private async Task UpdateUserHistoryAsync(Guid userId, string query)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookifyHotelDbContext>();

        // Prepend new query
        var history = new BookifyHotel.Model.SearchHistory
        {
            UserId = userId,
            Query = query,
            SearchDate = DateTime.UtcNow
        };
        
        dbContext.SearchHistories.Add(history);
        await dbContext.SaveChangesAsync();

        // Keep only top 3
        var userHistory = await dbContext.SearchHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.SearchDate)
            .ToListAsync();

        if (userHistory.Count > 3)
        {
            var toDelete = userHistory.Skip(3);
            dbContext.SearchHistories.RemoveRange(toDelete);
            await dbContext.SaveChangesAsync();
        }
    }

    private async Task IncrementGlobalCounterAsync(string query)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookifyHotelDbContext>();

        var normalizedQuery = query.Trim().ToLower();
        var counter = await dbContext.SearchCounters.FirstOrDefaultAsync(c => c.Query == normalizedQuery);

        if (counter == null)
        {
            counter = new BookifyHotel.Model.SearchCounter { Query = normalizedQuery, Count = 1 };
            dbContext.SearchCounters.Add(counter);
        }
        else
        {
            counter.Count++;
            dbContext.SearchCounters.Update(counter);
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<string>> GetUserHistoryAsync(Guid userId)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookifyHotelDbContext>();

        var history = await dbContext.SearchHistories
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.SearchDate)
            .Take(3)
            .Select(h => h.Query)
            .ToListAsync();

        return history;
    }

    public async Task<IReadOnlyList<string>> GetGlobalTopAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BookifyHotelDbContext>();

        var top = await dbContext.SearchCounters
            .OrderByDescending(c => c.Count)
            .Take(10)
            .Select(c => $"{c.Query} ({c.Count} searches)")
            .ToListAsync();

        return top;
    }
}
