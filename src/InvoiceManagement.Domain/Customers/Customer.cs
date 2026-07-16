using InvoiceManagement.Domain.Common;

namespace InvoiceManagement.Domain.Customers;

public sealed class Customer : SoftDeletableTenantEntity
{
    private Customer()
    {
    }

    private Customer(
        Guid id,
        Guid tenantId,
        string code,
        string legalName,
        string? taxNumber,
        string? email,
        DateTime createdUtc,
        string createdBy)
        : base(id, tenantId, createdUtc, createdBy)
    {
        Code = code;
        LegalName = legalName;
        TaxNumber = taxNumber;
        Email = email;
        IsActive = true;
    }

    public string Code { get; private set; } = string.Empty;

    public string LegalName { get; private set; } = string.Empty;

    public string? TaxNumber { get; private set; }

    public string? Email { get; private set; }

    public bool IsActive { get; private set; }

    public static Customer Create(
        Guid id,
        Guid tenantId,
        string code,
        string legalName,
        string? taxNumber,
        string? email,
        DateTime createdUtc,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(legalName))
        {
            throw new DomainException("customer.invalid", "Customer code and legal name are required.");
        }

        return new Customer(
            id,
            tenantId,
            code.Trim(),
            legalName.Trim(),
            Normalize(taxNumber),
            Normalize(email),
            createdUtc,
            createdBy);
    }

    public void SoftDelete(DateTime deletedUtc, string deletedBy) => MarkDeleted(deletedUtc, deletedBy);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
