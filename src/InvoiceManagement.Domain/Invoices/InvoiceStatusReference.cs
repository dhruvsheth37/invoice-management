namespace InvoiceManagement.Domain.Invoices;

public sealed class InvoiceStatusReference
{
    private InvoiceStatusReference()
    {
    }

    public InvoiceStatus Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public byte SortOrder { get; private set; }
}
