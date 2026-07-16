using System.Diagnostics;

namespace InvoiceManagement.Api.Observability;

public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            logger.LogInformation(
                "HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMilliseconds:F2} ms",
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }
}
