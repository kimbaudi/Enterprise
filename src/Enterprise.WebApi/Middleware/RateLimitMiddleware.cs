using System.Net;

namespace Enterprise.WebApi.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;

    public RateLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.StatusCode == (int)HttpStatusCode.TooManyRequests)
        {
            context.Response.ContentType = "application/json";
            
            var retryAfter = context.Response.Headers["Retry-After"].ToString();
            var response = new
            {
                StatusCode = 429,
                Message = "Rate limit exceeded. Please try again later.",
                RetryAfter = retryAfter
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}