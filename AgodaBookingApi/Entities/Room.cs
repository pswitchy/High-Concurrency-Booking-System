using System.ComponentModel.DataAnnotations;

namespace AgodaBookingApi.Entities
{
    public class Room
    {
        public int Id { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public bool IsBooked { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; } // <--- Added '?' here
    }
}