namespace InvoiceManagement.Domain.Invoices;

public sealed class InvoiceStatusHistory
{
    private InvoiceStatusHistory()
    {
    }

    private InvoiceStatusHistory(
        Guid id,
        Guid tenantId,
        Guid invoiceId,
        InvoiceStatus? fromStatus,
        InvoiceStatus toStatus,
        string? reason,
        DateTime changedUtc,
        int changedBy,
        string correlationId)
    {
        Id = id;
        TenantId = tenantId;
        InvoiceId = invoiceId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Reason = reason;
        ChangedUtc = changedUtc;
        ChangedBy = changedBy;
        CorrelationId = correlationId;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid InvoiceId { get; private set; }

    public InvoiceStatus? FromStatus { get; private set; }

    public InvoiceStatus ToStatus { get; private set; }

    public string? Reason { get; private set; }

    public DateTime ChangedUtc { get; private set; }

    public int ChangedBy { get; private set; } = 1;

    public string CorrelationId { get; private set; } = string.Empty;

    internal static InvoiceStatusHistory Record(
        Guid id,
        Guid tenantId,
        Guid invoiceId,
        InvoiceStatus? fromStatus,
        InvoiceStatus toStatus,
        string? reason,
        DateTime changedUtc,
        int changedBy,
        string correlationId) =>
        new(
            id,
            tenantId,
            invoiceId,
            fromStatus,
            toStatus,
            string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            changedUtc,
            changedBy,
            correlationId);
}
