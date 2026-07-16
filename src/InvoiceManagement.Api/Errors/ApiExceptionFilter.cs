using AppException = InvoiceManagement.Application.Common.ApplicationException;
using InvoiceManagement.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InvoiceManagement.Api.Errors;

public sealed class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        (int status, string code) = context.Exception switch
        {
            AppException exception => (exception.StatusCode, exception.ErrorCode),
            DomainException exception => (StatusCodes.Status409Conflict, exception.Code),
            _ => (StatusCodes.Status500InternalServerError, "server.unexpected"),
        };

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = status,
            Title = status == 500 ? "An unexpected error occurred." : context.Exception.Message,
            Detail = status == 500 ? null : context.Exception.Message,
            Extensions = { ["errorCode"] = code },
        }) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
