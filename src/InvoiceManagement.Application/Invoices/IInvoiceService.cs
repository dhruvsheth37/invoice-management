namespace InvoiceManagement.Application.Invoices;

public interface IInvoiceService
{
    Task<InvoiceDto> CreateAsync(CreateInvoiceRequest request, InvoiceOperationContext context, CancellationToken cancellationToken);
    Task<CursorResult<InvoiceListItemDto>> ListAsync(InvoiceListQuery query, CancellationToken cancellationToken);
    Task<InvoiceDto?> GetAsync(Guid invoiceId, CancellationToken cancellationToken);
    Task<InvoiceDto> IssueAsync(Guid invoiceId, IssueInvoiceRequest request, InvoiceOperationContext context, CancellationToken cancellationToken);
    Task<InvoiceDto> MarkPaidAsync(Guid invoiceId, MarkInvoicePaidRequest request, InvoiceOperationContext context, CancellationToken cancellationToken);
    Task<InvoiceDto> VoidAsync(Guid invoiceId, VoidInvoiceRequest request, InvoiceOperationContext context, CancellationToken cancellationToken);
    Task<InvoiceDashboardDto> GetDashboardAsync(DateOnly asOf, CancellationToken cancellationToken);
}
