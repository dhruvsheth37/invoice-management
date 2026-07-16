using InvoiceManagement.Domain.Common;
using InvoiceManagement.Domain.Invoices;

namespace InvoiceManagement.UnitTests.Invoices;

public sealed class InvoiceFoundationTests
{
    private static readonly DateTime Now = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Draft_calculates_totals_and_can_be_deactivated()
    {
        var invoice = CreateDraft();
        invoice.AddLine(Guid.NewGuid(), 1, "Freight", 2m, 125m, 0.18m, Now, "user-1");

        Assert.Equal(250m, invoice.Subtotal);
        Assert.Equal(45m, invoice.TaxTotal);
        Assert.Equal(295m, invoice.Total);

        invoice.DeactivateDraft(Now.AddMinutes(1), "user-1");

        Assert.False(invoice.IsActive);
        Assert.All(invoice.LineItems, line => Assert.False(line.IsActive));
    }

    [Fact]
    public void Issued_invoice_captures_bill_to_snapshot_and_cannot_be_deactivated()
    {
        var invoice = CreateDraft();
        invoice.AddLine(Guid.NewGuid(), 1, "Freight", 1m, 100m, 0.18m, Now, "user-1");
        var billTo = new BillToSnapshot(
            "ACME",
            "Acme Logistics Ltd",
            "TAX-1",
            "1 Main Street",
            null,
            "Mumbai",
            "Maharashtra",
            "400001",
            "IN");

        invoice.Issue(
            "INV-2026-000001",
            billTo,
            new DateOnly(2026, 7, 16),
            new DateOnly(2026, 8, 15),
            Now,
            "user-1",
            "correlation-1");

        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
        Assert.Equal("Acme Logistics Ltd", invoice.BillToLegalName);
        Assert.Throws<DomainException>(() => invoice.DeactivateDraft(Now.AddMinutes(1), "user-1"));
    }

    private static Invoice CreateDraft() =>
        Invoice.CreateDraft(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "USD",
            new DateOnly(2026, 8, 15),
            null,
            Now,
            "user-1",
            "correlation-1");
}
