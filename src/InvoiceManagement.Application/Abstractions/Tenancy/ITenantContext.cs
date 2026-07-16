namespace InvoiceManagement.Application.Abstractions.Tenancy;

public interface ITenantContext
{
    Guid TenantId { get; }

    bool IsResolved { get; }
}
