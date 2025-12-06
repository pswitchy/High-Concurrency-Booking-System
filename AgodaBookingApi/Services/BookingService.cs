using AgodaBookingApi.Data;
using AgodaBookingApi.Entities;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using RedLockNet;

namespace AgodaBookingApi.Services
{
    public class BookingService : IBookingService
    {
        private readonly AppDbContext _context;
        private readonly IDistributedLockFactory _lockFactory;
        private readonly IEmailProducer _emailProducer; // <--- 1. Inject the Producer
        private readonly AsyncRetryPolicy _dbRetryPolicy; // <--- 2. Define Policy

        public BookingService(AppDbContext context, 
                              IDistributedLockFactory lockFactory, 
                              IEmailProducer emailProducer)
        {
            _context = context;
            _lockFactory = lockFactory;
            _emailProducer = emailProducer;

            // 3. Define the Retry Logic (Resilience)
            // If DB fails, wait 1s, then 2s, then 4s (Exponential Backoff)
            _dbRetryPolicy = Policy
                .Handle<DbUpdateException>() 
                .Or<TimeoutException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, context) =>
                {
                    Console.WriteLine($"[Polly] DB Glitch! Retrying in {timeSpan.TotalSeconds}s...");
                });
        }

        public async Task<BookingResult> BookRoomAsync(int roomId, string guestName)
        {
            var resourceKey = $"room-lock:{roomId}";
            var expiry = TimeSpan.FromSeconds(30);
            var wait = TimeSpan.FromSeconds(10);
            var retry = TimeSpan.FromSeconds(1);

            using (var redLock = await _lockFactory.CreateLockAsync(resourceKey, expiry, wait, retry))
            {
                if (!redLock.IsAcquired) return new BookingResult(false, "System busy.", null);

                try
                {
                    var room = await _context.Rooms.FindAsync(roomId);

                    if (room == null) return new BookingResult(false, "Room not found.", null);
                    if (room.IsBooked) return new BookingResult(false, "Room is already booked.", null);

                    room.IsBooked = true;
                    var newBooking = new Booking
                    {
                        RoomId = roomId,
                        GuestName = guestName,
                        BookingDate = DateTime.UtcNow
                    };
                    _context.Bookings.Add(newBooking);

                    // 4. Execute DB Save inside Polly Policy
                    await _dbRetryPolicy.ExecuteAsync(async () =>
                    {
                        await _context.SaveChangesAsync();
                    });

                    // 5. Fire and Forget Email (RabbitMQ)
                    // We do this AFTER DB save is successful
                    _emailProducer.SendBookingConfirmation(newBooking.Id, guestName);

                    return new BookingResult(true, "Booking confirmed!", room.Id);
                }
                catch (DbUpdateConcurrencyException)
                {
                    return new BookingResult(false, "Race Condition! Room taken.", null);
                }
                catch (Exception ex)
                {
                    return new BookingResult(false, $"Error: {ex.Message}", null);
                }
            }
        }
    }
}