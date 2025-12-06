using Microsoft.Extensions.Caching.Distributed;

namespace AgodaBookingApi.Middleware
{
    public class IdempotencyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDistributedCache _cache;

        public IdempotencyMiddleware(RequestDelegate next, IDistributedCache cache)
        {
            _next = next;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only enforce on POST requests with the specific Header
            if (context.Request.Method != "POST" || !context.Request.Headers.ContainsKey("Idempotency-Key"))
            {
                await _next(context);
                return;
            }

            string key = $"idempotency:{context.Request.Headers["Idempotency-Key"]}";

            // 1. Check if we already processed this ID
            var cachedResponse = await _cache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(cachedResponse))
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(cachedResponse);
                return;
            }

            // 2. Capture the response
            var originalBodyStream = context.Response.Body;
            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            await _next(context);

            // 3. Save response to Redis if successful (200 OK)
            memoryStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
            memoryStream.Seek(0, SeekOrigin.Begin);

            if (context.Response.StatusCode == 200)
            {
                await _cache.SetStringAsync(key, responseBody, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                });
            }

            await memoryStream.CopyToAsync(originalBodyStream);
        }
    }
}