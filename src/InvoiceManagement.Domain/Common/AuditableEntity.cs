namespace InvoiceManagement.Domain.Common;

public abstract class AuditableEntity
{
    protected AuditableEntity()
    {
    }

    protected AuditableEntity(Guid id, DateTime createdUtc, string createdBy)
    {
        Id = id;
        CreatedUtc = createdUtc;
        CreatedBy = createdBy;
    }

    public Guid Id { get; protected set; }

    public DateTime CreatedUtc { get; protected set; }

    public string CreatedBy { get; protected set; } = string.Empty;

    public DateTime? ModifiedUtc { get; protected set; }

    public string? ModifiedBy { get; protected set; }

    public byte[] RowVersion { get; private set; } = [];

    protected void Touch(DateTime modifiedUtc, string modifiedBy)
    {
        ModifiedUtc = modifiedUtc;
        ModifiedBy = modifiedBy;
    }
}
