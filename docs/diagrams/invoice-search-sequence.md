# Invoice Search Sequence

```mermaid
sequenceDiagram
    autonumber
    actor Client
    participant API as InvoicesController
    participant Tenant as Tenant middleware/context
    participant Service as InvoiceService
    participant EF as EF Core
    participant DB as SQL Server

    Client->>Tenant: POST /api/v1/invoices/search<br/>filters, sort and optional cursor
    Tenant->>Tenant: Validate tenant_id claim
    Tenant->>API: Continue with resolved tenant
    API->>Service: InvoiceListQuery and cancellation token
    Service->>Service: Validate page size and allow-listed sort
    Service->>EF: Compose explicit business filters
    EF->>EF: Add TenantFilter and ActiveFilter
    opt includeTotalCount is true
        EF->>DB: Execute COUNT for filtered scope
        DB-->>EF: Exact total
    end
    Service->>Service: Decode cursor and verify filter/sort scope hash
    Service->>EF: Apply keyset predicate, stable ordering,<br/>Take(pageSize + 1), DTO projection
    EF->>DB: Execute tenant-scoped page query
    DB-->>EF: Projected rows
    Service->>Service: Detect next page and encode opaque cursor
    Service-->>Client: CursorResult with optional total count
```

Offset pagination is not used. A future focused Specification may encapsulate reusable business predicates, but tenant isolation remains in the EF global filter and cursor/order/projection behavior remains explicit in the query workflow.
