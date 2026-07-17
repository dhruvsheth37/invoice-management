using InvoiceManagement.Domain.Common;

namespace InvoiceManagement.Domain.Invoices;

public sealed class InvoiceNumberSequence : ITenantScoped
{
    private InvoiceNumberSequence()
    {
    }

    public Guid TenantId { get; private set; }

    public short FiscalYear { get; private set; }

    public long CurrentValue { get; private set; }

    public DateTime ModifiedUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static InvoiceNumberSequence Create(Guid tenantId, short fiscalYear, DateTime now) => new()
    {
        TenantId = tenantId,
        FiscalYear = fiscalYear,
        CurrentValue = 0,
        ModifiedUtc = now,
    };

    public long Allocate(DateTime now)
    {
        CurrentValue++;
        ModifiedUtc = now;
        return CurrentValue;
    }
}
