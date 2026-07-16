using System.Diagnostics;

namespace InvoiceManagement.Api.Observability;

public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    private static readonly Action<ILogger, string, string?, int, double, Exception?> RequestCompleted =
        LoggerMessage.Define<string, string?, int, double>(
            LogLevel.Information,
            new EventId(2001, nameof(RequestCompleted)),
            "HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMilliseconds:F2} ms");

    public async Task InvokeAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            RequestCompleted(
                logger,
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                null);
        }
    }
}
