using InvoiceManagement.Domain.Common;
using InvoiceManagement.Domain.Invoices;
using InvoiceManagement.Domain.Platform;

namespace InvoiceManagement.UnitTests.Invoices;

public sealed class InvoiceWorkflowTests
{
    private static readonly DateTime Now = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Sequence_allocates_monotonic_invoice_values()
    {
        var sequence = InvoiceNumberSequence.Create(Guid.NewGuid(), 2026, Now);

        Assert.Equal(1, sequence.Allocate(Now));
        Assert.Equal(2, sequence.Allocate(Now.AddSeconds(1)));
        Assert.Equal(2, sequence.CurrentValue);
    }

    [Fact]
    public void Idempotency_record_captures_completed_response()
    {
        var resourceId = Guid.NewGuid();
        var record = IdempotencyRequest.Start(Guid.NewGuid(), Guid.NewGuid(), "invoice.create", "key-1",
            new byte[32], "correlation-1", Now);

        record.Complete(resourceId, 201, "{}", Now.AddSeconds(1));

        Assert.Equal(IdempotencyState.Completed, record.State);
        Assert.Equal(resourceId, record.ResourceId);
        Assert.Equal<short?>((short)201, record.ResponseStatus);
    }

    [Fact]
    public void Paid_invoice_cannot_be_voided()
    {
        var invoice = Invoice.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "USD", new DateOnly(2026, 8, 15), null, Now, 1, "correlation-1");
        invoice.AddLine(Guid.NewGuid(), 1, "Freight", 1, 100, 0.18m, Now, 1);
        invoice.Issue("INV-2026-000001", new BillToSnapshot("ACME", "Acme Ltd", null, "1 Main St", null,
            "Mumbai", null, null, "IN"), new DateOnly(2026, 7, 16), new DateOnly(2026, 8, 15),
            Now, 1, "correlation-2");
        invoice.MarkPaid(new DateOnly(2026, 7, 17), "payment-1", Now.AddDays(1), 1, "correlation-3");

        Assert.Throws<DomainException>(() => invoice.Void("invalid", Now.AddDays(1), 1, "correlation-4"));
    }
}
