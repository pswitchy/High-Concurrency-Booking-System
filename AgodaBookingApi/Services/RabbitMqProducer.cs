using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace AgodaBookingApi.Services
{
    // Define the interface here (or in a separate file)
    public interface IEmailProducer
    {
        void SendBookingConfirmation(int bookingId, string guestName);
    }

    public class RabbitMqProducer : IEmailProducer
    {
        private readonly ConnectionFactory _factory;

        public RabbitMqProducer(IConfiguration config)
        {
            _factory = new ConnectionFactory()
            {
                // If running via "dotnet run", use "localhost"
                // If running inside Docker container, this should be "rabbitmq"
                HostName = config["RabbitMqHost"] ?? "localhost", 
                UserName = "guest",
                Password = "guest"
            };
        }

        public void SendBookingConfirmation(int bookingId, string guestName)
        {
            try
            {
                using var connection = _factory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.QueueDeclare(queue: "booking_confirmations",
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var message = new { BookingId = bookingId, Guest = guestName, Timestamp = DateTime.UtcNow };
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                channel.BasicPublish(exchange: "",
                                     routingKey: "booking_confirmations",
                                     basicProperties: null,
                                     body: body);
                                     
                Console.WriteLine($"[RabbitMQ Producer] Queued email for Booking #{bookingId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RabbitMQ Error] {ex.Message}");
            }
        }
    }
}