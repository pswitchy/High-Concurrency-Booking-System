using AgodaBookingApi.Data;
using AgodaBookingApi.Entities;
using Microsoft.EntityFrameworkCore;
using RedLockNet; // Requires 'RedLock.net' NuGet package

namespace AgodaBookingApi.Services
{
    public class BookingService : IBookingService
    {
        private readonly AppDbContext _context;
        private readonly IDistributedLockFactory _lockFactory;

        public BookingService(AppDbContext context, IDistributedLockFactory lockFactory)
        {
            _context = context;
            _lockFactory = lockFactory;
        }

        public async Task<BookingResult> BookRoomAsync(int roomId, string guestName)
        {
            // Define Lock Key & Timeouts
            var resourceKey = $"room-lock:{roomId}";
            var expiry = TimeSpan.FromSeconds(30);
            var wait = TimeSpan.FromSeconds(10);
            var retry = TimeSpan.FromSeconds(1);

            // 1. Distributed Lock (Redis) - Prevents multiple servers handling same room
            using (var redLock = await _lockFactory.CreateLockAsync(resourceKey, expiry, wait, retry))
            {
                if (!redLock.IsAcquired)
                {
                    return new BookingResult(false, "System busy (Lock not acquired).", null);
                }

                try
                {
                    // 2. Database Check
                    var room = await _context.Rooms.FindAsync(roomId);

                    if (room == null) return new BookingResult(false, "Room not found.", null);
                    if (room.IsBooked) return new BookingResult(false, "Room is already booked.", null);

                    // 3. Update Status
                    room.IsBooked = true;
                    _context.Bookings.Add(new Booking
                    {
                        RoomId = roomId,
                        GuestName = guestName,
                        BookingDate = DateTime.UtcNow
                    });

                    // 4. Save with Optimistic Concurrency
                    // If RowVersion changed since we read it, this throws DbUpdateConcurrencyException
                    await _context.SaveChangesAsync();

                    return new BookingResult(true, "Booking confirmed!", room.Id);
                }
                catch (DbUpdateConcurrencyException)
                {
                    return new BookingResult(false, "Race Condition! Someone beat you to it.", null);
                }
                catch (Exception ex)
                {
                    return new BookingResult(false, $"Error: {ex.Message}", null);
                }
            }
        }
    }
}