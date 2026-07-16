using InvoiceManagement.Domain.Common;

namespace InvoiceManagement.Domain.Customers;

public sealed record PostalAddress
{
    public PostalAddress(
        string addressLine1,
        string? addressLine2,
        string city,
        string? stateProvince,
        string? postalCode,
        string countryCode)
    {
        if (string.IsNullOrWhiteSpace(addressLine1) || string.IsNullOrWhiteSpace(city))
        {
            throw new DomainException("address.invalid", "Address line 1 and city are required.");
        }

        var normalizedCountryCode = countryCode.Trim().ToUpperInvariant();
        if (normalizedCountryCode.Length != 2)
        {
            throw new DomainException("address.country_invalid", "Country code must contain two characters.");
        }

        AddressLine1 = addressLine1.Trim();
        AddressLine2 = Normalize(addressLine2);
        City = city.Trim();
        StateProvince = Normalize(stateProvince);
        PostalCode = Normalize(postalCode);
        CountryCode = normalizedCountryCode;
    }

    public string AddressLine1 { get; }

    public string? AddressLine2 { get; }

    public string City { get; }

    public string? StateProvince { get; }

    public string? PostalCode { get; }

    public string CountryCode { get; }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
