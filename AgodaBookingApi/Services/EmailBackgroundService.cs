using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace AgodaBookingApi.Services
{
    public class EmailBackgroundService : BackgroundService
    {
        private readonly IConfiguration _config;
        private IConnection _connection;
        private IModel _channel;

        public EmailBackgroundService(IConfiguration config)
        {
            _config = config;
            InitializeRabbitMq();
        }

        private void InitializeRabbitMq()
        {
            var factory = new ConnectionFactory()
            {
                HostName = _config["RabbitMqHost"] ?? "localhost",
                UserName = "guest",
                Password = "guest",
                DispatchConsumersAsync = true
            };

            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
                _channel.QueueDeclare(queue: "booking_confirmations", durable: true, exclusive: false, autoDelete: false, arguments: null);
            }
            catch (Exception ex)
            {
                // It might fail if RabbitMQ container isn't ready yet
                Console.WriteLine($"[WmailWorker] Waiting for RabbitMQ... ({ex.Message})");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_channel == null || _channel.IsClosed)
                {
                    InitializeRabbitMq();
                    await Task.Delay(3000, stoppingToken);
                    continue;
                }

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    
                    // Simulate sending email
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"[WmailWorker] 📧 Sending Email -> {message}");
                    Console.ResetColor();

                    _channel.BasicAck(ea.DeliveryTag, false);
                    await Task.Yield();
                };

                _channel.BasicConsume(queue: "booking_confirmations", autoAck: false, consumer: consumer);
                
                // Block this thread so the service keeps running
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
        }
        
        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}