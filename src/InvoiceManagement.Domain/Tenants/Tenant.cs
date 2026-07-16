using InvoiceManagement.Domain.Common;

namespace InvoiceManagement.Domain.Tenants;

public sealed class Tenant : AuditableEntity
{
    private Tenant()
    {
    }

    private Tenant(Guid id, string slug, string name, DateTime createdUtc, int createdBy)
        : base(id, createdUtc, createdBy)
    {
        Slug = slug;
        Name = name;
        IsActive = true;
    }

    public string Slug { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public static Tenant Create(Guid id, string slug, string name, DateTime createdUtc, int createdBy)
    {
        if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("tenant.invalid", "Tenant slug and name are required.");
        }

        return new Tenant(id, slug.Trim(), name.Trim(), createdUtc, createdBy);
    }
}
