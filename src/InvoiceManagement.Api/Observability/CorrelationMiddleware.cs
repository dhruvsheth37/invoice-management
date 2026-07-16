using System.Diagnostics;
using System.Text.RegularExpressions;

namespace InvoiceManagement.Api.Observability;

public sealed partial class CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers[HeaderName].FirstOrDefault();
        var isInvalid = supplied is not null && (supplied.Length is < 1 or > 64 || !AllowedCharacters().IsMatch(supplied));
        var generated = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        var correlationId = isInvalid || supplied is null ? generated : supplied;
        context.Items["CorrelationId"] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });
        Activity.Current?.SetTag("app.correlation_id", correlationId);

        if (isInvalid)
        {
            throw new InvalidCorrelationException("X-Correlation-ID must contain 1-64 letters, digits, dots, underscores, colons, or hyphens.");
        }

        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier,
        }))
        {
            await next(context);
        }
    }

    [GeneratedRegex("^[A-Za-z0-9._:-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex AllowedCharacters();
}

public sealed class InvalidCorrelationException(string message) : Exception(message);
