namespace InvoiceManagement.Domain.Common;

public abstract class AuditableEntity
{
    protected AuditableEntity()
    {
    }

    protected AuditableEntity(Guid id, DateTime createdUtc, int createdBy)
    {
        Id = id;
        CreatedUtc = createdUtc;
        CreatedBy = createdBy;
        ModifiedBy = createdBy;
    }

    public Guid Id { get; protected set; }

    public DateTime CreatedUtc { get; protected set; }

    public int CreatedBy { get; protected set; } = 1;

    public DateTime? ModifiedUtc { get; protected set; }

    public int ModifiedBy { get; protected set; } = 1;

    public byte[] RowVersion { get; private set; } = [];

    protected void Touch(DateTime modifiedUtc, int modifiedBy)
    {
        ModifiedUtc = modifiedUtc;
        ModifiedBy = modifiedBy;
    }
}
