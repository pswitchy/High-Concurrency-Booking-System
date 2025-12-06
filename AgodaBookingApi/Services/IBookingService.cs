namespace AgodaBookingApi.Services
{
    public interface IBookingService
    {
        Task<BookingResult> BookRoomAsync(int roomId, string guestName);
    }

    public record BookingResult(bool Success, string Message, int? BookingId);
}