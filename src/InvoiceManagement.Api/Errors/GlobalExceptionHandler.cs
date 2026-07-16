using System.Diagnostics;
using AppException = InvoiceManagement.Application.Common.ApplicationException;
using InvoiceManagement.Api.Observability;
using InvoiceManagement.Api.Tenancy;
using InvoiceManagement.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceManagement.Api.Errors;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private static readonly Action<ILogger, object?, string?, Exception?> UnexpectedError =
        LoggerMessage.Define<object?, string?>(LogLevel.Error, new EventId(3001, nameof(UnexpectedError)),
            "Unhandled request exception for tenant {TenantId} and user {UserId}");
    private static readonly Action<ILogger, string, object?, string?, Exception?> RequestRejected =
        LoggerMessage.Define<string, object?, string?>(LogLevel.Warning, new EventId(3002, nameof(RequestRejected)),
            "Request rejected with {ErrorCode} for tenant {TenantId} and user {UserId}");

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, code, title, detail) = exception switch
        {
            AppException error => (error.StatusCode, error.ErrorCode, error.Message, error.Message),
            DomainException error => (StatusCodes.Status409Conflict, error.Code, "Business rule rejected the operation.", error.Message),
            InvalidCorrelationException error => (StatusCodes.Status400BadRequest, "correlation.invalid", "Invalid correlation identifier.", error.Message),
            TenantAccessException error => (StatusCodes.Status403Forbidden, "tenant.invalid", "Tenant access is not permitted.", error.Message),
            BadHttpRequestException error => (StatusCodes.Status400BadRequest, "request.invalid", "The request is invalid.", error.Message),
            _ => (StatusCodes.Status500InternalServerError, "server.unexpected", "An unexpected error occurred.", null),
        };

        if (status >= 500)
            UnexpectedError(logger, httpContext.Items["TenantId"], httpContext.User.FindFirst("user_id")?.Value, exception);
        else
            RequestRejected(logger, code, httpContext.Items["TenantId"], httpContext.User.FindFirst("user_id")?.Value, exception);

        httpContext.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["errorCode"] = code,
                ["traceId"] = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier,
                ["correlationId"] = httpContext.Items["CorrelationId"]?.ToString(),
            },
        };

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
