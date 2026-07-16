namespace InvoiceManagement.Domain.Common;

public abstract class ActivatableTenantEntity : TenantEntity
{
    protected ActivatableTenantEntity()
    {
    }

    protected ActivatableTenantEntity(Guid id, Guid tenantId, DateTime createdUtc, int createdBy)
        : base(id, tenantId, createdUtc, createdBy)
    {
        IsActive = true;
    }

    public bool IsActive { get; private set; }

    protected void MarkInactive(DateTime modifiedUtc, int modifiedBy)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        Touch(modifiedUtc, modifiedBy);
    }
}
