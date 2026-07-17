# Tenant Isolation

```mermaid
flowchart TB
    Client["API client"]
    Auth["Authentication<br/>Development claims or production JWT"]
    Middleware["TenantResolutionMiddleware<br/>validate trusted tenant_id claim"]
    Context["RequestTenantContext<br/>immutable after resolution"]
    Factory["Scoped pooled-context lease<br/>SetTenant on every lease"]
    Model["EF Core model conventions<br/>ITenantScoped and IActivatable"]
    TenantFilter["Named TenantFilter<br/>always retained in request queries"]
    ActiveFilter["Named ActiveFilter<br/>may be selectively disabled for audit"]
    WriteGuard["DbContext write guard<br/>reject mismatched tracked entities"]
    Keys["Tenant-leading alternate keys,<br/>composite foreign keys and indexes"]
    Database[("SQL Server")]

    Client --> Auth
    Auth --> Middleware
    Middleware --> Context
    Context --> Factory
    Factory --> Model
    Model --> TenantFilter
    Model --> ActiveFilter
    TenantFilter --> Database
    ActiveFilter --> Database
    Factory --> WriteGuard
    WriteGuard --> Keys
    Keys --> Database
```

`CustomerId` and `CustomerLocationId` remain business/query criteria, not tenant-context substitutes. Billing location is not a global authorization scope in the implemented model. Parallel work must use a separate initialized `DbContext` per operation; a context is never shared concurrently.
