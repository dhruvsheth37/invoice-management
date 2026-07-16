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
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
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
            logger.LogError(exception, "Unhandled request exception for tenant {TenantId} and user {UserId}",
                context.Items["TenantId"], context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
        else
            logger.LogWarning(exception, "Request rejected with {ErrorCode} for tenant {TenantId} and user {UserId}",
                code, context.Items["TenantId"], context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

        context.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Extensions =
            {
                ["errorCode"] = code,
                ["traceId"] = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier,
                ["correlationId"] = context.Items["CorrelationId"]?.ToString(),
            },
        };

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
