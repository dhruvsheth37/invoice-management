namespace InvoiceManagement.Domain.Invoices;

public enum InvoiceStatus : byte
{
    Draft = 1,
    Issued = 2,
    Paid = 3,
    Void = 4,
}
