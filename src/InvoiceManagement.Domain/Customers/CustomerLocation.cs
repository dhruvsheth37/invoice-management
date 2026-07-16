using InvoiceManagement.Domain.Common;

namespace InvoiceManagement.Domain.Customers;

public sealed class CustomerLocation : ActivatableTenantEntity
{
    private CustomerLocation()
    {
    }

    private CustomerLocation(
        Guid id,
        Guid tenantId,
        Guid customerId,
        string name,
        PostalAddress address,
        string? taxNumber,
        DateTime createdUtc,
        int createdBy)
        : base(id, tenantId, createdUtc, createdBy)
    {
        CustomerId = customerId;
        Name = name;
        AddressLine1 = address.AddressLine1;
        AddressLine2 = address.AddressLine2;
        City = address.City;
        StateProvince = address.StateProvince;
        PostalCode = address.PostalCode;
        CountryCode = address.CountryCode;
        TaxNumber = taxNumber;
    }

    public Guid CustomerId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string AddressLine1 { get; private set; } = string.Empty;

    public string? AddressLine2 { get; private set; }

    public string City { get; private set; } = string.Empty;

    public string? StateProvince { get; private set; }

    public string? PostalCode { get; private set; }

    public string CountryCode { get; private set; } = string.Empty;

    public string? TaxNumber { get; private set; }

    public static CustomerLocation Create(
        Guid id,
        Guid tenantId,
        Guid customerId,
        string name,
        PostalAddress address,
        string? taxNumber,
        DateTime createdUtc,
        int createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("customer_location.invalid", "Customer location name is required.");
        }

        return new CustomerLocation(
            id,
            tenantId,
            customerId,
            name.Trim(),
            address,
            string.IsNullOrWhiteSpace(taxNumber) ? null : taxNumber.Trim(),
            createdUtc,
            createdBy);
    }

    public void Deactivate(DateTime modifiedUtc, int modifiedBy) => MarkInactive(modifiedUtc, modifiedBy);
}
