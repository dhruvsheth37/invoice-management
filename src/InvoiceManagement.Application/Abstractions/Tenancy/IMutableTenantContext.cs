namespace InvoiceManagement.Application.Abstractions.Tenancy;

public interface IMutableTenantContext : ITenantContext
{
    void SetTenant(Guid tenantId);
}
