using AgodaBookingApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgodaBookingApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Room> Rooms { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Seeding 1 Room for the test
            modelBuilder.Entity<Room>().HasData(
                new Room { Id = 1, RoomName = "Agoda Deluxe Suite", IsBooked = false }
            );
        }
    }
}