using InvoiceManagement.Domain.Common;

namespace InvoiceManagement.Domain.Invoices;

public sealed record BillToSnapshot
{
    public BillToSnapshot(
        string customerCode,
        string legalName,
        string? taxNumber,
        string addressLine1,
        string? addressLine2,
        string city,
        string? stateProvince,
        string? postalCode,
        string countryCode)
    {
        if (string.IsNullOrWhiteSpace(customerCode) ||
            string.IsNullOrWhiteSpace(legalName) ||
            string.IsNullOrWhiteSpace(addressLine1) ||
            string.IsNullOrWhiteSpace(city))
        {
            throw new DomainException("invoice.bill_to_invalid", "Required bill-to values are missing.");
        }

        var normalizedCountryCode = countryCode.Trim().ToUpperInvariant();
        if (normalizedCountryCode.Length != 2)
        {
            throw new DomainException("invoice.bill_to_country_invalid", "Bill-to country code must contain two characters.");
        }

        CustomerCode = customerCode.Trim();
        LegalName = legalName.Trim();
        TaxNumber = Normalize(taxNumber);
        AddressLine1 = addressLine1.Trim();
        AddressLine2 = Normalize(addressLine2);
        City = city.Trim();
        StateProvince = Normalize(stateProvince);
        PostalCode = Normalize(postalCode);
        CountryCode = normalizedCountryCode;
    }

    public string CustomerCode { get; }

    public string LegalName { get; }

    public string? TaxNumber { get; }

    public string AddressLine1 { get; }

    public string? AddressLine2 { get; }

    public string City { get; }

    public string? StateProvince { get; }

    public string? PostalCode { get; }

    public string CountryCode { get; }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
