using System.Diagnostics;

namespace InvoiceManagement.Api.Observability;

public sealed partial class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/health") || !logger.IsEnabled(LogLevel.Debug))
        {
            await next(context);
            return;
        }

        var started = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            LogRequestCompleted(
                logger,
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Debug,
        Message = "HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMilliseconds:F2} ms")]
    private static partial void LogRequestCompleted(
        ILogger logger,
        string method,
        string? path,
        int statusCode,
        double elapsedMilliseconds);
}
