namespace InvoiceManagement.Application.Common;

public sealed class ApplicationException(string errorCode, string message, int statusCode) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;

    public int StatusCode { get; } = statusCode;

    public static ApplicationException NotFound() => new("invoice.not_found", "Invoice was not found.", 404);
    public static ApplicationException Validation(string code, string message) => new(code, message, 422);
    public static ApplicationException Conflict(string code, string message) => new(code, message, 409);
    public static ApplicationException Precondition(string message) => new("invoice.precondition_required", message, 428);
}
