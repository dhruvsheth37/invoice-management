namespace InvoiceManagement.Domain.Common;

public abstract class TenantEntity : AuditableEntity, ITenantScoped
{
    protected TenantEntity()
    {
    }

    protected TenantEntity(Guid id, Guid tenantId, DateTime createdUtc, int createdBy)
        : base(id, createdUtc, createdBy)
    {
        TenantId = tenantId;
    }

    public Guid TenantId { get; protected set; }
}
