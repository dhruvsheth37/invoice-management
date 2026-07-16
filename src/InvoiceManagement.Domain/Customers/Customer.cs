using InvoiceManagement.Domain.Common;

namespace InvoiceManagement.Domain.Customers;

public sealed class Customer : ActivatableTenantEntity
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
    }

    public string Code { get; private set; } = string.Empty;

    public string LegalName { get; private set; } = string.Empty;

    public string? TaxNumber { get; private set; }

    public string? Email { get; private set; }

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

    public void Deactivate(DateTime modifiedUtc, string modifiedBy) => MarkInactive(modifiedUtc, modifiedBy);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
