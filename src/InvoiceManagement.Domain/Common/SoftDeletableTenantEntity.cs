namespace InvoiceManagement.Domain.Common;

public abstract class SoftDeletableTenantEntity : TenantEntity
{
    protected SoftDeletableTenantEntity()
    {
    }

    protected SoftDeletableTenantEntity(Guid id, Guid tenantId, DateTime createdUtc, string createdBy)
        : base(id, tenantId, createdUtc, createdBy)
    {
    }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedUtc { get; private set; }

    public string? DeletedBy { get; private set; }

    protected void MarkDeleted(DateTime deletedUtc, string deletedBy)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedUtc = deletedUtc;
        DeletedBy = deletedBy;
        Touch(deletedUtc, deletedBy);
    }
}
