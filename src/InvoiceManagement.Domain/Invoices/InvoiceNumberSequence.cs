namespace InvoiceManagement.Domain.Invoices;

public sealed class InvoiceNumberSequence
{
    private InvoiceNumberSequence()
    {
    }

    public Guid TenantId { get; private set; }

    public short FiscalYear { get; private set; }

    public long CurrentValue { get; private set; }

    public DateTime ModifiedUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];
}
