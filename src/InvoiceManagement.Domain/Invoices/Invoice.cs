using InvoiceManagement.Domain.Common;

namespace InvoiceManagement.Domain.Invoices;

public sealed class Invoice : SoftDeletableTenantEntity
{
    private readonly List<InvoiceLineItem> _lineItems = [];
    private readonly List<InvoiceStatusHistory> _statusHistory = [];

    private Invoice()
    {
    }

    private Invoice(
        Guid id,
        Guid tenantId,
        Guid customerId,
        Guid customerLocationId,
        string currencyCode,
        DateOnly? dueDate,
        string? notes,
        DateTime createdUtc,
        string createdBy)
        : base(id, tenantId, createdUtc, createdBy)
    {
        CustomerId = customerId;
        CustomerLocationId = customerLocationId;
        CurrencyCode = currencyCode;
        DueDate = dueDate;
        Notes = notes;
        Status = InvoiceStatus.Draft;
    }

    public Guid CustomerId { get; private set; }

    public Guid CustomerLocationId { get; private set; }

    public string? BillToCustomerCode { get; private set; }

    public string? BillToLegalName { get; private set; }

    public string? BillToTaxNumber { get; private set; }

    public string? BillToAddressLine1 { get; private set; }

    public string? BillToAddressLine2 { get; private set; }

    public string? BillToCity { get; private set; }

    public string? BillToStateProvince { get; private set; }

    public string? BillToPostalCode { get; private set; }

    public string? BillToCountryCode { get; private set; }

    public string? InvoiceNumber { get; private set; }

    public InvoiceStatus Status { get; private set; }

    public string CurrencyCode { get; private set; } = string.Empty;

    public DateOnly? IssueDate { get; private set; }

    public DateOnly? DueDate { get; private set; }

    public DateOnly? PaidDate { get; private set; }

    public string? PaymentReference { get; private set; }

    public decimal Subtotal { get; private set; }

    public decimal TaxTotal { get; private set; }

    public decimal Total { get; private set; }

    public string? Notes { get; private set; }

    public string? VoidReason { get; private set; }

    public IReadOnlyCollection<InvoiceLineItem> LineItems => _lineItems.AsReadOnly();

    public IReadOnlyCollection<InvoiceStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public static Invoice CreateDraft(
        Guid id,
        Guid tenantId,
        Guid customerId,
        Guid customerLocationId,
        string currencyCode,
        DateOnly? dueDate,
        string? notes,
        DateTime createdUtc,
        string createdBy,
        string correlationId)
    {
        var normalizedCurrency = currencyCode.Trim().ToUpperInvariant();
        if (normalizedCurrency.Length != 3)
        {
            throw new DomainException("invoice.currency_invalid", "Currency code must contain three characters.");
        }

        var invoice = new Invoice(
            id,
            tenantId,
            customerId,
            customerLocationId,
            normalizedCurrency,
            dueDate,
            Normalize(notes),
            createdUtc,
            createdBy);

        invoice.RecordStatusChange(null, InvoiceStatus.Draft, null, createdUtc, createdBy, correlationId);
        return invoice;
    }

    public void AddLine(
        Guid lineId,
        short lineNumber,
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal taxRate,
        DateTime modifiedUtc,
        string modifiedBy)
    {
        EnsureDraft();
        if (_lineItems.Any(line => !line.IsDeleted && line.LineNumber == lineNumber))
        {
            throw new DomainException("invoice.line_number_duplicate", "Invoice line number must be unique.");
        }

        _lineItems.Add(InvoiceLineItem.Create(
            lineId,
            TenantId,
            Id,
            lineNumber,
            description,
            quantity,
            unitPrice,
            taxRate,
            modifiedUtc,
            modifiedBy));

        RecalculateTotals();
        Touch(modifiedUtc, modifiedBy);
    }

    public void Issue(
        string invoiceNumber,
        BillToSnapshot billTo,
        DateOnly issueDate,
        DateOnly dueDate,
        DateTime changedUtc,
        string changedBy,
        string correlationId)
    {
        EnsureDraft();
        if (!_lineItems.Any(line => !line.IsDeleted))
        {
            throw new DomainException("invoice.lines_required", "An invoice requires at least one line item.");
        }

        if (dueDate < issueDate)
        {
            throw new DomainException("invoice.due_date_invalid", "Due date cannot precede issue date.");
        }

        InvoiceNumber = invoiceNumber;
        IssueDate = issueDate;
        DueDate = dueDate;
        ApplyBillTo(billTo);
        TransitionTo(InvoiceStatus.Issued, null, changedUtc, changedBy, correlationId);
    }

    public void MarkPaid(
        DateOnly paidDate,
        string externalReference,
        DateTime changedUtc,
        string changedBy,
        string correlationId)
    {
        if (Status != InvoiceStatus.Issued)
        {
            throw new DomainException("invoice.invalid_transition", "Only an issued invoice can be marked paid.");
        }

        if (IssueDate is not null && paidDate < IssueDate)
        {
            throw new DomainException("invoice.paid_date_invalid", "Paid date cannot precede issue date.");
        }

        PaidDate = paidDate;
        PaymentReference = externalReference.Trim();
        TransitionTo(InvoiceStatus.Paid, externalReference, changedUtc, changedBy, correlationId);
    }

    public void Void(
        string reason,
        DateTime changedUtc,
        string changedBy,
        string correlationId)
    {
        if (Status is not (InvoiceStatus.Draft or InvoiceStatus.Issued))
        {
            throw new DomainException("invoice.invalid_transition", "Only a draft or issued invoice can be voided.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("invoice.void_reason_required", "A void reason is required.");
        }

        VoidReason = reason.Trim();
        TransitionTo(InvoiceStatus.Void, VoidReason, changedUtc, changedBy, correlationId);
    }

    public void SoftDeleteDraft(DateTime deletedUtc, string deletedBy)
    {
        EnsureDraft();
        foreach (var line in _lineItems.Where(line => !line.IsDeleted))
        {
            line.SoftDelete(deletedUtc, deletedBy);
        }

        MarkDeleted(deletedUtc, deletedBy);
    }

    private void ApplyBillTo(BillToSnapshot billTo)
    {
        BillToCustomerCode = billTo.CustomerCode;
        BillToLegalName = billTo.LegalName;
        BillToTaxNumber = billTo.TaxNumber;
        BillToAddressLine1 = billTo.AddressLine1;
        BillToAddressLine2 = billTo.AddressLine2;
        BillToCity = billTo.City;
        BillToStateProvince = billTo.StateProvince;
        BillToPostalCode = billTo.PostalCode;
        BillToCountryCode = billTo.CountryCode;
    }

    private void TransitionTo(
        InvoiceStatus newStatus,
        string? reason,
        DateTime changedUtc,
        string changedBy,
        string correlationId)
    {
        var previousStatus = Status;
        Status = newStatus;
        RecordStatusChange(previousStatus, newStatus, reason, changedUtc, changedBy, correlationId);
        Touch(changedUtc, changedBy);
    }

    private void RecordStatusChange(
        InvoiceStatus? fromStatus,
        InvoiceStatus toStatus,
        string? reason,
        DateTime changedUtc,
        string changedBy,
        string correlationId) =>
        _statusHistory.Add(InvoiceStatusHistory.Record(
            Guid.CreateVersion7(),
            TenantId,
            Id,
            fromStatus,
            toStatus,
            reason,
            changedUtc,
            changedBy,
            correlationId));

    private void RecalculateTotals()
    {
        var activeLines = _lineItems.Where(line => !line.IsDeleted);
        Subtotal = activeLines.Sum(line => line.NetAmount);
        TaxTotal = activeLines.Sum(line => line.TaxAmount);
        Total = Subtotal + TaxTotal;
    }

    private void EnsureDraft()
    {
        if (Status != InvoiceStatus.Draft || IsDeleted)
        {
            throw new DomainException("invoice.not_editable", "Only an active draft invoice can be changed.");
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
