using InvoiceManagement.Application.Abstractions.Tenancy;

namespace InvoiceManagement.Api.Tenancy;

public sealed class RequestTenantContext : IMutableTenantContext
{
    public Guid TenantId { get; private set; }

    public bool IsResolved { get; private set; }

    public void SetTenant(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant identifier cannot be empty.", nameof(tenantId));
        }

        if (IsResolved && TenantId != tenantId)
        {
            throw new InvalidOperationException("Tenant context is already resolved for this request.");
        }

        TenantId = tenantId;
        IsResolved = true;
    }
}
