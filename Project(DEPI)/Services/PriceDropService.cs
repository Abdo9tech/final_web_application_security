using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BookifyHotel.Data;
using BookifyHotel.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Project_DEPI.Services
{
    public class PriceDropService : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Random _random = new Random();
        private readonly object _lock = new object();
        private const int IntervalMs = 60_000; // 60 seconds

        public PriceDropService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DoWork, null, IntervalMs, IntervalMs);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            lock (_lock)
            {
                // We use IServiceScopeFactory because PriceDropService is a Singleton HostedService, 
                // but BookifyHotelDbContext and IEmailSender are Scoped.
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<BookifyHotelDbContext>();
                
                var watchedHotels = dbContext.WatchedHotels.ToList();
                if (watchedHotels.Count == 0) return;

                var idx = _random.Next(watchedHotels.Count);
                var hotel = watchedHotels[idx];
                
                // Simulate 10-15% drop from original price
                var dropPct = _random.Next(10, 16);
                var newPrice = Math.Round(hotel.OriginalPrice * (100 - dropPct) / 100m, 2);
                
                if (newPrice < hotel.CurrentPrice)
                {
                    hotel.CurrentPrice = newPrice;
                    dbContext.SaveChanges(); // Update DB

                    var subject = $"Price drop alert: {hotel.Name}";
                    var body = $"Price dropped for {hotel.Name} from ${hotel.OriginalPrice} to ${hotel.CurrentPrice}! Book now.";

                    var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                    _ = emailSender.SendEmailAsync(hotel.UserEmail, subject, body);
                }
            }
        }

        public void AddWatch(int hotelId, string name, decimal price, string userEmail)
        {
            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<BookifyHotelDbContext>();

                if (dbContext.WatchedHotels.Any(w => w.HotelId == hotelId && w.UserEmail == userEmail))
                {
                    return; // already watching
                }

                dbContext.WatchedHotels.Add(new WatchedHotel
                {
                    HotelId = hotelId,
                    Name = name,
                    OriginalPrice = price,
                    CurrentPrice = price,
                    UserEmail = userEmail
                });
                dbContext.SaveChanges();
            }
        }

        public List<WatchedHotel> GetWatches(string email)
        {
            lock (_lock)
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<BookifyHotelDbContext>();

                return dbContext.WatchedHotels
                    .Where(w => w.UserEmail == email)
                    .ToList();
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
