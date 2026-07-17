namespace InvoiceManagement.Domain.Common;

public interface ITenantScoped
{
    Guid TenantId { get; }
}
