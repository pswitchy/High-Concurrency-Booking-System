using AgodaBookingApi.Data;
using AgodaBookingApi.Services;
using AgodaBookingApi.Middleware;
using Microsoft.EntityFrameworkCore;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using RedLockNet;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURATION ---
var redisConn = builder.Configuration.GetValue<string>("RedisConnection") ?? "localhost:6379";
var dbConn = builder.Configuration.GetConnectionString("DefaultConnection");

// --- 2. REGISTER SERVICES ---

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dbConn)); 

// Redis Cache (for Idempotency)
builder.Services.AddStackExchangeRedisCache(options => {
    options.Configuration = redisConn;
});

builder.Services.AddSingleton<IEmailProducer, RabbitMqProducer>();

// 2. Register the Consumer (The Background Worker)
// This starts the "WmailBackgroundService" automatically when the app starts
builder.Services.AddHostedService<EmailBackgroundService>();

// RedLock (Distributed Locking)
builder.Services.AddSingleton<IDistributedLockFactory>(sp =>
{
    var multiplexer = ConnectionMultiplexer.Connect(redisConn);
    return RedLockFactory.Create(new List<RedLockMultiplexer> { multiplexer });
});

// Internal Services
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- 3. PIPELINE ---

// Auto-migrate DB (For demo convenience)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Wait a moment for SQL Server container to wake up
    Thread.Sleep(3000); 
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<IdempotencyMiddleware>();

app.UseAuthorization();
app.MapControllers();

app.Run();