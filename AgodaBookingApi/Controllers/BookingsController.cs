using AgodaBookingApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgodaBookingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] BookingRequest request)
        {
            var result = await _bookingService.BookRoomAsync(request.RoomId, request.GuestName);
            
            if (!result.Success)
                return Conflict(new { error = result.Message });

            return Ok(result);
        }
    }

    public record BookingRequest(int RoomId, string GuestName);
}