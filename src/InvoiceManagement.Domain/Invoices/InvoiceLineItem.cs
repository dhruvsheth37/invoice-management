using InvoiceManagement.Domain.Common;

namespace InvoiceManagement.Domain.Invoices;

public sealed class InvoiceLineItem : SoftDeletableTenantEntity
{
    private const int MoneyScale = 4;

    private InvoiceLineItem()
    {
    }

    private InvoiceLineItem(
        Guid id,
        Guid tenantId,
        Guid invoiceId,
        short lineNumber,
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal taxRate,
        DateTime createdUtc,
        string createdBy)
        : base(id, tenantId, createdUtc, createdBy)
    {
        InvoiceId = invoiceId;
        LineNumber = lineNumber;
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TaxRate = taxRate;
        Recalculate();
    }

    public Guid InvoiceId { get; private set; }

    public short LineNumber { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal TaxRate { get; private set; }

    public decimal NetAmount { get; private set; }

    public decimal TaxAmount { get; private set; }

    public decimal TotalAmount { get; private set; }

    internal static InvoiceLineItem Create(
        Guid id,
        Guid tenantId,
        Guid invoiceId,
        short lineNumber,
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal taxRate,
        DateTime createdUtc,
        string createdBy)
    {
        if (lineNumber <= 0 || quantity <= 0 || unitPrice < 0 || taxRate is < 0 or > 1)
        {
            throw new DomainException("invoice.line_invalid", "Invoice line values are invalid.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("invoice.line_description_required", "Invoice line description is required.");
        }

        return new InvoiceLineItem(
            id,
            tenantId,
            invoiceId,
            lineNumber,
            description.Trim(),
            quantity,
            unitPrice,
            taxRate,
            createdUtc,
            createdBy);
    }

    internal void SoftDelete(DateTime deletedUtc, string deletedBy) => MarkDeleted(deletedUtc, deletedBy);

    private void Recalculate()
    {
        NetAmount = Round(Quantity * UnitPrice);
        TaxAmount = Round(NetAmount * TaxRate);
        TotalAmount = NetAmount + TaxAmount;
    }

    private static decimal Round(decimal value) =>
        decimal.Round(value, MoneyScale, MidpointRounding.AwayFromZero);
}
