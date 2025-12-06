using System.Net.Http.Json;
using System.Diagnostics;

// SETTINGS
const int USER_COUNT = 15; // 15 people fighting for 1 room
const string BASE_URL = "http://localhost:5217/api/Bookings"; 
// ^ Ensure this port matches what your API runs on (Properties/launchSettings.json)

Console.WriteLine($"--- STARTING AGODA FLASH SALE SIMULATION ---");
Console.WriteLine($"Users: {USER_COUNT} | Room ID: 1\n");

var client = new HttpClient();
var tasks = new List<Task<HttpResponseMessage>>();
var stopwatch = Stopwatch.StartNew();

// 1. Prepare requests (don't await them yet)
for (int i = 0; i < USER_COUNT; i++)
{
    var body = new { RoomId = 1, GuestName = $"User_{i}" };
    
    // All requests share SAME Idempotency Key? No, unique per user.
    // Same user double clicking? Then same Key.
    // Here we simulate unique users.
    var requestMessage = new HttpRequestMessage(HttpMethod.Post, BASE_URL);
    requestMessage.Content = JsonContent.Create(body);
    requestMessage.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());

    tasks.Add(client.SendAsync(requestMessage));
}

// 2. Fire!
await Task.WhenAll(tasks);
stopwatch.Stop();

// 3. Tally results
int success = 0;
int failed = 0;

foreach (var t in tasks)
{
    var response = await t;
    string msg = await response.Content.ReadAsStringAsync();
    
    if (response.IsSuccessStatusCode)
    {
        success++;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[SUCCESS] {msg}");
    }
    else
    {
        failed++;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[FAILED]  {msg}");
    }
}
Console.ResetColor();

Console.WriteLine($"\n--- RESULT ---");
Console.WriteLine($"Sold: {success} (Should be 1)");
Console.WriteLine($"Rejected: {failed}");