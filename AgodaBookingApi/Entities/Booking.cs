namespace AgodaBookingApi.Entities
{
    public class Booking
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public string GuestName { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
    }
}