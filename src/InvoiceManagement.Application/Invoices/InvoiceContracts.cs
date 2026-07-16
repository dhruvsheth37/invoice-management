using InvoiceManagement.Domain.Invoices;

namespace InvoiceManagement.Application.Invoices;

public sealed record CreateInvoiceRequest(
    Guid CustomerId,
    Guid CustomerLocationId,
    string CurrencyCode,
    DateOnly? DueDate,
    string? Notes,
    IReadOnlyList<CreateInvoiceLineRequest> LineItems);

public sealed record CreateInvoiceLineRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate);

public sealed record IssueInvoiceRequest(DateOnly? IssueDate, DateOnly? DueDate);

public sealed record MarkInvoicePaidRequest(DateOnly PaidDate, string Reference);

public sealed record VoidInvoiceRequest(string Reason);

public sealed record InvoiceOperationContext(
    int UserId,
    string CorrelationId,
    string IdempotencyKey,
    string? IfMatch);

public sealed record InvoiceListQuery(
    int Page = 1,
    int PageSize = 25,
    InvoiceStatus? Status = null,
    Guid? CustomerId = null,
    DateOnly? From = null,
    DateOnly? To = null,
    DateOnly? DueFrom = null,
    DateOnly? DueTo = null,
    string? InvoiceNumber = null,
    string? Sort = null);

public sealed record InvoiceLineDto(
    Guid Id,
    short LineNumber,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal NetAmount,
    decimal TaxAmount,
    decimal TotalAmount);

public sealed record InvoiceStatusHistoryDto(
    InvoiceStatus? FromStatus,
    InvoiceStatus ToStatus,
    string? Reason,
    DateTime ChangedUtc,
    int ChangedBy,
    string CorrelationId);

public sealed record InvoiceDto(
    Guid Id,
    Guid CustomerId,
    Guid CustomerLocationId,
    string? InvoiceNumber,
    InvoiceStatus Status,
    string CurrencyCode,
    DateOnly? IssueDate,
    DateOnly? DueDate,
    DateOnly? PaidDate,
    string? PaymentReference,
    decimal Subtotal,
    decimal TaxTotal,
    decimal Total,
    string? Notes,
    string? VoidReason,
    string? BillToCustomerCode,
    string? BillToLegalName,
    string? BillToTaxNumber,
    string? BillToAddressLine1,
    string? BillToAddressLine2,
    string? BillToCity,
    string? BillToStateProvince,
    string? BillToPostalCode,
    string? BillToCountryCode,
    DateTime CreatedUtc,
    DateTime? ModifiedUtc,
    string ETag,
    IReadOnlyList<InvoiceLineDto> LineItems,
    IReadOnlyList<InvoiceStatusHistoryDto> StatusHistory);

public sealed record InvoiceListItemDto(
    Guid Id,
    string? InvoiceNumber,
    InvoiceStatus Status,
    Guid CustomerId,
    string CurrencyCode,
    decimal Total,
    DateOnly? IssueDate,
    DateOnly? DueDate,
    DateTime CreatedUtc);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record InvoiceAmountSummary(int Count, decimal Amount);

public sealed record InvoiceCurrencySummary(
    string CurrencyCode,
    InvoiceAmountSummary Draft,
    InvoiceAmountSummary Issued,
    InvoiceAmountSummary Paid,
    InvoiceAmountSummary Void,
    InvoiceAmountSummary Overdue);

public sealed record InvoiceDashboardDto(DateOnly AsOf, IReadOnlyList<InvoiceCurrencySummary> CurrencyBreakdown);
