using BookifyHotel.Configuration;
using BookifyHotel.Model;
using BookifyHotel.ModelConfiguration;
using DAL.DataBase;
using LuxuryHaven.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BookifyHotel.Data
{
    public class BookifyHotelDbContext : IdentityDbContext<AppUser> 
    {
        public BookifyHotelDbContext()
        {
        }

        public BookifyHotelDbContext(DbContextOptions<BookifyHotelDbContext> option)
            : base(option)
        {
        }

        public DbSet<UserProfile> UserProfiles { get; set; }    // جدولك المستقل
        public DbSet<WatchedHotel> WatchedHotels { get; set; }
        //public DbSet<Role> AppRoles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<User_Role> User_Roles { get; set; }
        public DbSet<RolePermission> Role_Permissions { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<RoomType> RoomTypes { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<ReservationCart> ReservationCarts { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<FavoriteRoom> FavoriteRooms { get; set; }
        public DbSet<AgentReport> AgentReports { get; set; }
        public DbSet<SearchHistory> SearchHistories { get; set; }
        public DbSet<SearchCounter> SearchCounters { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // مهم: يهيئ جداول AspNetUsers وغيرها

            //modelBuilder.ApplyConfiguration(new ReservationCartConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentConfiguration());
            modelBuilder.ApplyConfiguration(new BookingConfiguration());
            modelBuilder.ApplyConfiguration(new RoomTypeConfiguration());
            modelBuilder.ApplyConfiguration(new RoomConfiguration());

            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            modelBuilder.Entity<User_Role>()
                .HasKey(ur => new { ur.UserProfileId, ur.RoleId });

            //modelBuilder.ApplyConfiguration(new PermissionConfiguration());
            //modelBuilder.ApplyConfiguration(new RoleConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());




          
            modelBuilder.Entity<User_Role>()
                .HasKey(ur => new { ur.UserProfileId, ur.RoleId });

            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            // إعداد العلاقة بين UserProfile و AppUser
            modelBuilder.Entity<UserProfile>()
                .HasOne(up => up.User)
                .WithOne()
                .HasForeignKey<UserProfile>(up => up.IdentityUserId)
                .IsRequired();


            modelBuilder.Entity<IdentityRole>().ToTable("AspNetRoles") ;
       

            // ربط UserProfile بـ IdentityUser
            //modelBuilder.Entity<UserProfile>()
            //    .HasOne(up => up.User)
            //    .WithOne()
            //    .HasForeignKey<UserProfile>(up => up.IdentityUserId)
            //    .IsRequired();




            modelBuilder.Entity<Booking>()
        .Property(b => b.TotalPrice)
        .HasPrecision(18, 2);

            modelBuilder.Entity<ReservationCart>()
                .Property(r => r.PricePreview)
                .HasPrecision(18, 2);







            modelBuilder.Entity<ReservationCart>()
                .HasOne(rc => rc.Room)
                .WithMany() // إذا كان Room ليس له ICollection<ReservationCart>
                .HasForeignKey(rc => rc.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReservationCart>()
                .HasOne(rc => rc.UserProfile)
                .WithMany(up => up.ReservationCarts)
                .HasForeignKey(rc => rc.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);







            modelBuilder.Entity<Contact>(entity =>
            {
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.CreatedAt);

                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.PhoneNumber)
                    .HasMaxLength(20);

                entity.Property(e => e.Message)
                    .IsRequired()
                    .HasMaxLength(1000);
            });











            modelBuilder.Entity<FavoriteRoom>(entity =>
            {
                entity.HasKey(f => f.Id);
                entity.HasOne(f => f.UserProfile)
                    .WithMany()
                    .HasForeignKey(f => f.UserProfileId)
                    .OnDelete(DeleteBehavior.NoAction); // Use NoAction to avoid multiple cascade paths

                entity.HasOne(f => f.Room)
                    .WithMany()
                    .HasForeignKey(f => f.RoomId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}


