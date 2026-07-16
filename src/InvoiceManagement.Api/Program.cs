using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using InvoiceManagement.Api.Authentication;
using InvoiceManagement.Api.Health;
using InvoiceManagement.Api.Errors;
using InvoiceManagement.Api.Observability;
using InvoiceManagement.Api.Tenancy;
using InvoiceManagement.Application.Abstractions.Tenancy;
using InvoiceManagement.Infrastructure;
using InvoiceManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
});

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    context.ProblemDetails.Extensions.TryAdd("traceId", System.Diagnostics.Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier);
    context.ProblemDetails.Extensions.TryAdd("correlationId", context.HttpContext.Items["CorrelationId"]?.ToString());
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthentication(DevelopmentAuthenticationHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
            DevelopmentAuthenticationHandler.SchemeName,
            _ => { });
}
else
{
    var authority = builder.Configuration["Authentication:Authority"];
    var audience = builder.Configuration["Authentication:Audience"];
    if (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(audience))
        throw new InvalidOperationException("Authentication:Authority and Authentication:Audience are required outside Development.");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = audience;
            options.RequireHttpsMetadata = true;
            options.MapInboundClaims = false;
        });
}

builder.Services.AddAuthorization();
builder.Services.AddScoped<RequestTenantContext>();
builder.Services.AddScoped<ITenantContext>(provider => provider.GetRequiredService<RequestTenantContext>());
builder.Services.AddScoped<IMutableTenantContext>(provider => provider.GetRequiredService<RequestTenantContext>());
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var requestTimeoutSeconds = builder.Configuration.GetValue("Api:RequestTimeoutSeconds", 30);
builder.Services.AddRequestTimeouts(options => options.AddPolicy("api", TimeSpan.FromSeconds(requestTimeoutSeconds)));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("api", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst("tenant_id")?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = builder.Configuration.GetValue("Api:RateLimit:PermitLimit", 100),
            Window = TimeSpan.FromSeconds(builder.Configuration.GetValue("Api:RateLimit:WindowSeconds", 60)),
            QueueLimit = 0,
            AutoReplenishment = true,
        }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        var problemService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await problemService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "The tenant request limit has been exceeded.",
                Extensions =
                {
                    ["errorCode"] = "rate_limit.exceeded",
                    ["correlationId"] = context.HttpContext.Items["CorrelationId"]?.ToString(),
                },
            },
        });
    };
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<CorrelationMiddleware>();
app.UseStatusCodePages(async statusCodeContext =>
{
    var response = statusCodeContext.HttpContext.Response;
    if (response.HasStarted || response.ContentLength is > 0 || string.IsNullOrEmpty(response.ContentType) is false)
        return;

    var problemService = statusCodeContext.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
    await problemService.TryWriteAsync(new ProblemDetailsContext
    {
        HttpContext = statusCodeContext.HttpContext,
        ProblemDetails = new ProblemDetails
        {
            Status = response.StatusCode,
            Title = response.StatusCode switch
            {
                StatusCodes.Status401Unauthorized => "Authentication is required.",
                StatusCodes.Status403Forbidden => "Access is forbidden.",
                StatusCodes.Status404NotFound => "The resource was not found.",
                _ => "The request could not be completed.",
            },
        },
    });
});
app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseRateLimiter();
app.UseRequestTimeouts();
app.UseAuthorization();

app.MapControllers()
    .RequireAuthorization()
    .RequireRateLimiting("api")
    .WithRequestTimeout("api");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponse,
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponse,
});

app.Run();

static Task WriteHealthResponse(HttpContext context, Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
{
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsync(JsonSerializer.Serialize(new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString(),
            durationMs = entry.Value.Duration.TotalMilliseconds,
        }),
    }));
}

public partial class Program;
