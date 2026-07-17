# Component Architecture

```mermaid
flowchart LR
    subgraph API["InvoiceManagement.Api"]
        Controllers["Thin controllers"]
        Pipeline["Authentication, correlation,<br/>tenant, logging, limits, errors"]
        Operations["OpenAPI and health checks"]
    end

    subgraph Application["InvoiceManagement.Application"]
        Contracts["IInvoiceService and request contracts"]
        DTOs["Response DTOs and mapping extensions"]
        Abstractions["Persistence and tenant abstractions"]
    end

    subgraph Domain["InvoiceManagement.Domain"]
        CustomerDomain["Customer and location model"]
        InvoiceAggregate["Invoice aggregate and lifecycle"]
        Rules["Totals, rounding and invariants"]
        Markers["ITenantScoped and IActivatable"]
    end

    subgraph Infrastructure["InvoiceManagement.Infrastructure"]
        Service["InvoiceService implementation"]
        Allocator["Atomic invoice-number allocator"]
        DbContext["Pooled EF Core DbContext"]
        Protection["TenantFilter, ActiveFilter,<br/>tenant write guard"]
        Persistence["Entity mappings, temporal tables,<br/>idempotency and migrations"]
        Sql[("SQL Server")]
    end

    Pipeline --> Controllers
    Operations --> Controllers
    Controllers --> Contracts
    Controllers --> DTOs
    Service -.->|"implements"| Contracts
    Service --> Abstractions
    Service --> CustomerDomain
    Service --> InvoiceAggregate
    InvoiceAggregate --> Rules
    DbContext --> Markers
    Service --> DbContext
    Service --> Allocator
    DbContext --> Protection
    DbContext --> Persistence
    Allocator --> Sql
    Persistence --> Sql
```

Logical module boundaries are kept inside four projects to avoid assessment-time project proliferation. Architecture tests enforce inward references and prevent Domain entities or Infrastructure implementation types from entering controller contracts.
