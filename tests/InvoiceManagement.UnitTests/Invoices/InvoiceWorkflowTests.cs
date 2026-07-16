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

    [Fact]
    public void Invoice_without_lines_cannot_be_issued()
    {
        var invoice = CreateDraft();

        var exception = Assert.Throws<DomainException>(() => Issue(invoice));

        Assert.Equal("invoice.lines_required", exception.Code);
    }

    [Fact]
    public void Due_date_before_issue_date_is_rejected()
    {
        var invoice = CreateDraftWithLine();

        var exception = Assert.Throws<DomainException>(() => Issue(
            invoice,
            issueDate: new DateOnly(2026, 7, 16),
            dueDate: new DateOnly(2026, 7, 15)));

        Assert.Equal("invoice.due_date_invalid", exception.Code);
    }

    [Fact]
    public void Draft_cannot_be_marked_paid()
    {
        var invoice = CreateDraftWithLine();

        var exception = Assert.Throws<DomainException>(() =>
            invoice.MarkPaid(new DateOnly(2026, 7, 17), "payment-1", Now, 1, "correlation-paid"));

        Assert.Equal("invoice.invalid_transition", exception.Code);
    }

    [Fact]
    public void Paid_date_before_issue_date_is_rejected()
    {
        var invoice = CreateDraftWithLine();
        Issue(invoice);

        var exception = Assert.Throws<DomainException>(() =>
            invoice.MarkPaid(new DateOnly(2026, 7, 15), "payment-1", Now, 1, "correlation-paid"));

        Assert.Equal("invoice.paid_date_invalid", exception.Code);
    }

    [Fact]
    public void Lifecycle_records_actor_reason_and_correlation()
    {
        var invoice = CreateDraftWithLine();
        Issue(invoice);
        invoice.Void("  customer cancellation  ", Now.AddMinutes(1), 42, "correlation-void");

        Assert.Equal(InvoiceStatus.Void, invoice.Status);
        Assert.Equal("customer cancellation", invoice.VoidReason);
        var history = invoice.StatusHistory.OrderBy(item => item.ChangedUtc).ToArray();
        Assert.Equal(3, history.Length);
        Assert.Equal(InvoiceStatus.Issued, history[^1].FromStatus);
        Assert.Equal(InvoiceStatus.Void, history[^1].ToStatus);
        Assert.Equal(42, history[^1].ChangedBy);
        Assert.Equal("correlation-void", history[^1].CorrelationId);
    }

    [Fact]
    public void Issued_invoice_cannot_be_edited()
    {
        var invoice = CreateDraftWithLine();
        Issue(invoice);

        var exception = Assert.Throws<DomainException>(() =>
            invoice.AddLine(Guid.NewGuid(), 2, "Late line", 1, 25, 0, Now, 1));

        Assert.Equal("invoice.not_editable", exception.Code);
    }

    private static Invoice CreateDraft() =>
        Invoice.CreateDraft(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "USD", new DateOnly(2026, 8, 15), null, Now, 1, "correlation-created");

    private static Invoice CreateDraftWithLine()
    {
        var invoice = CreateDraft();
        invoice.AddLine(Guid.NewGuid(), 1, "Freight", 1, 100, 0.18m, Now, 1);
        return invoice;
    }

    private static void Issue(
        Invoice invoice,
        DateOnly? issueDate = null,
        DateOnly? dueDate = null) =>
        invoice.Issue(
            "INV-2026-000001",
            new BillToSnapshot("ACME", "Acme Ltd", null, "1 Main St", null, "Mumbai", null, null, "IN"),
            issueDate ?? new DateOnly(2026, 7, 16),
            dueDate ?? new DateOnly(2026, 8, 15),
            Now,
            1,
            "correlation-issued");
}
