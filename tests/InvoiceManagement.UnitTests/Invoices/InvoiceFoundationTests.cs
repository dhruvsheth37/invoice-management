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
        invoice.AddLine(Guid.NewGuid(), 1, "Freight", 2m, 125m, 0.18m, Now, 1);

        Assert.Equal(250m, invoice.Subtotal);
        Assert.Equal(45m, invoice.TaxTotal);
        Assert.Equal(295m, invoice.Total);

        invoice.DeactivateDraft(Now.AddMinutes(1), 1);

        Assert.False(invoice.IsActive);
        Assert.All(invoice.LineItems, line => Assert.False(line.IsActive));
    }

    [Fact]
    public void Issued_invoice_captures_bill_to_snapshot_and_cannot_be_deactivated()
    {
        var invoice = CreateDraft();
        invoice.AddLine(Guid.NewGuid(), 1, "Freight", 1m, 100m, 0.18m, Now, 1);
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
            1,
            "correlation-1");

        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
        Assert.Equal("Acme Logistics Ltd", invoice.BillToLegalName);
        Assert.Throws<DomainException>(() => invoice.DeactivateDraft(Now.AddMinutes(1), 1));
    }

    [Fact]
    public void Line_amounts_round_away_from_zero_to_four_decimal_places()
    {
        var invoice = CreateDraft();

        invoice.AddLine(Guid.NewGuid(), 1, "Fractional service", 1m, 1.23455m, 0.1m, Now, 7);

        var line = Assert.Single(invoice.LineItems);
        Assert.Equal(1.2346m, line.NetAmount);
        Assert.Equal(0.1235m, line.TaxAmount);
        Assert.Equal(1.3581m, line.TotalAmount);
        Assert.Equal(7, invoice.ModifiedBy);
    }

    [Theory]
    [InlineData(0, 1, 1, "invoice.line_invalid")]
    [InlineData(1, 1, -0.01, "invoice.line_invalid")]
    [InlineData(1, -1, 1, "invoice.line_invalid")]
    [InlineData(1, 1, 1.01, "invoice.line_invalid")]
    public void Invalid_line_values_are_rejected(decimal quantity, decimal unitPrice, decimal taxRate, string errorCode)
    {
        var invoice = CreateDraft();

        var exception = Assert.Throws<DomainException>(() =>
            invoice.AddLine(Guid.NewGuid(), 1, "Service", quantity, unitPrice, taxRate, Now, 1));

        Assert.Equal(errorCode, exception.Code);
    }

    [Fact]
    public void Duplicate_active_line_number_is_rejected()
    {
        var invoice = CreateDraft();
        invoice.AddLine(Guid.NewGuid(), 1, "First", 1, 10, 0, Now, 1);

        var exception = Assert.Throws<DomainException>(() =>
            invoice.AddLine(Guid.NewGuid(), 1, "Duplicate", 1, 20, 0, Now, 1));

        Assert.Equal("invoice.line_number_duplicate", exception.Code);
    }

    [Fact]
    public void Draft_normalizes_currency_and_optional_notes()
    {
        var invoice = Invoice.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            " usd ", null, "  handled carefully  ", Now, 1, "correlation-1");

        Assert.Equal("USD", invoice.CurrencyCode);
        Assert.Equal("handled carefully", invoice.Notes);
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
            1,
            "correlation-1");
}
