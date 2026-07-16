# Architecture

## 1. Purpose

Build a small, clean, production-minded multi-tenant invoice API using C#, ASP.NET Core, Entity Framework Core, and SQL Server. The design must satisfy the assessment within its 5-6 hour guideline while demonstrating a credible path to a larger SaaS platform.

The implementation is intentionally a modular monolith. It preserves explicit boundaries without introducing the operational cost of distributed services.

## 2. Architecture drivers

1. Strict tenant isolation.
2. Correct and auditable invoice lifecycle transitions.
3. Server-authoritative financial calculations.
4. Efficient, paginated tenant-scoped queries.
5. Consistent validation and error responses.
6. Traceable operations using W3C trace context and correlation IDs.
7. A schema that prevents cross-tenant relationships.
8. Focused implementation that fits the assessment timebox.

## 3. Assessment scope

### Implemented

- Create a draft invoice.
- List invoices with pagination and filters.
- Retrieve invoice details.
- Issue, mark paid, and void an invoice through business operations.
- Retrieve an invoice summary/dashboard.
- Manage customer and customer-location references needed by invoices.
- Enforce tenant isolation in request handling, EF Core, and SQL Server relationships.
- Apply optimistic concurrency, idempotency, structured logging, and standardized errors.
- Keep temporal history for invoices and invoice line items.

### Explicitly excluded from the assessment implementation

- Payment recording, allocation, reconciliation, and refunds.
- Redis.
- Azure Service Bus.
- Azure Functions and ETL jobs.
- AKS resources.
- Azure SQL read replicas.
- Temporal tables for every transactional entity.
- SQL triggers.
- Expansion into approximately ten projects.

These are documented as future enhancements rather than partially implemented abstractions.

## 4. Solution structure

```text
src/
├── InvoiceManagement.Api
├── InvoiceManagement.Application
├── InvoiceManagement.Domain
└── InvoiceManagement.Infrastructure

tests/
├── InvoiceManagement.UnitTests
└── InvoiceManagement.IntegrationTests

database/
├── README.md
├── scripts/
├── generated/
├── verification/
└── rollback/

docs/
```

### Responsibilities

| Project | Responsibility |
|---|---|
| `Api` | HTTP endpoints, middleware, authentication boundary, OpenAPI, composition root |
| `Application` | Use cases, DTOs, validation, ports, transaction orchestration |
| `Domain` | Entities, value rules, lifecycle transitions, domain errors |
| `Infrastructure` | EF Core, SQL Server mappings, tenant query filters, migrations, external implementations |
| `UnitTests` | Domain and application behavior |
| `IntegrationTests` | API, SQL Server, tenant isolation, concurrency, and temporal history |

Dependencies point inward:

```text
Api -> Application -> Domain
Api -> Infrastructure -> Application + Domain
```

No generic repository is planned. Application handlers use a narrow persistence abstraction implemented by the EF Core context, or the context directly where that keeps the code clearer.

## 5. Module boundaries

The first deployment contains four logical modules:

- Customers: customer identity and billing locations.
- Invoices: draft creation, totals, issue, paid confirmation, void, and history.
- Dashboard: read-only tenant summary queries.
- Platform: tenant context, idempotency, time, logging, errors, and persistence.

Modules are folders and namespaces within one deployment. They do not communicate through an in-process event bus merely to simulate microservices.

## 6. Invoice lifecycle

```text
Draft --issue--> Issued --mark paid--> Paid
  |                 |
  +------void-------+---------------> Void
```

Rules:

- A draft can be edited before issue.
- Invoice number, issue date, and due date are finalized during issue.
- Issued invoice financial fields and line items are immutable.
- Issuance snapshots the customer and billing-location identity onto the invoice so later master-data edits or deactivation do not rewrite financial history.
- `Paid` and `Void` are terminal in the assessment scope.
- `Overdue` is derived from `DueDate`; it is not persisted as a lifecycle status.
- `PartiallyPaid` is deferred until a payment/allocation model exists.
- The API exposes business operations rather than an unrestricted status patch.

## 7. Multi-tenancy

The assessment uses a shared database and shared schema with a required `TenantId` on tenant-owned data.

Isolation is layered:

1. Request layer resolves the tenant from a trusted JWT claim in production. `X-Tenant-Id` is allowed only in Development and integration tests.
2. Application commands never accept an authoritative tenant identifier in the request body.
3. EF Core global query filters apply both `TenantId` and `IsActive` predicates.
4. Tenant-leading alternate keys and composite foreign keys prevent cross-tenant references in SQL Server.
5. Integration tests attempt cross-tenant reads and writes.

Global query filters are defense in depth, not the sole security boundary. Administrative operations that bypass filters must be isolated and explicit.

## 8. Record activation

`Customers`, `CustomerLocations`, `Invoices`, and `InvoiceLineItems` contain only `IsActive bit NOT NULL DEFAULT 1` for application-level visibility. No separate soft-deletion flag or deletion-specific audit columns are used.

Policy:

- No hard-delete endpoint is part of the assessment API.
- Only Draft invoices and their line items may be deactivated.
- Issued, Paid, and Void invoices are financial records and must remain active and retained; issued invoices are voided rather than deactivated.
- Customers and locations referenced by an active Draft cannot be deactivated. Issued invoices remain readable from their immutable bill-to snapshots.
- EF query filters exclude inactive business rows by default.
- Standard `ModifiedUtc` and `ModifiedBy` audit fields record the last change, including deactivation.

Activation is distinct from temporal history. `IsActive` controls normal visibility; temporal tables preserve prior row versions.

## 9. Temporal auditing and business history

SQL Server system-versioned temporal tables are enabled only for:

- `Invoices` -> `history.InvoicesHistory`
- `InvoiceLineItems` -> `history.InvoiceLineItemsHistory`

The temporal period columns are hidden shadow properties in EF Core:

- `ValidFromUtc datetime2(7) GENERATED ALWAYS AS ROW START`
- `ValidToUtc datetime2(7) GENERATED ALWAYS AS ROW END`

`InvoiceStatusHistory` remains an explicit append-only table because temporal data does not explain who performed a transition, why it occurred, or which request caused it.

## 10. Financial integrity

- Clients provide quantities, unit prices, and tax rates, but never authoritative totals.
- The domain calculates line and invoice totals using `decimal` values with explicit rounding rules.
- Invoice totals are persisted in the same transaction as line items.
- SQL check constraints reject negative values and prevent deactivating non-Draft invoices.
- Invoice numbers are assigned only during issue using a tenant/fiscal-year sequence record.
- Customer legal name, tax identity, and billing address are copied to immutable invoice snapshot fields during issue.
- A filtered unique index guarantees invoice-number uniqueness per tenant.
- The design guarantees uniqueness, not legally gapless numbering.

## 11. Concurrency and idempotency

- Mutable business rows use SQL Server `rowversion`.
- State-changing operations require an `If-Match` value derived from the current row version.
- A stale version returns `409 Conflict` using `ProblemDetails`.
- `Idempotency-Key` is required on create and lifecycle commands.
- The key is scoped by tenant and operation.
- Reuse with an identical request returns the stored result; reuse with a different request hash returns `409 Conflict`.

Correlation IDs trace an execution. Idempotency keys prevent duplicate execution. They are not interchangeable.

## 12. Observability

- `System.Diagnostics.Activity` and W3C `traceparent` are the canonical distributed trace mechanism.
- The API accepts a validated `X-Correlation-ID` or creates one.
- Both identifiers are returned to the caller and added to structured logging scope.
- Logs include `TraceId`, `CorrelationId`, `TenantId`, `UserId`, and relevant `InvoiceId`.
- `CorrelationId` is stored in status history and idempotency records where it adds audit value, not on every domain table.
- Future HTTP calls and messages propagate W3C context and correlation metadata.

## 13. Database governance

EF Core migrations are the single schema authority for the assessment. The root `database/` folder contains reviewable deployment artifacts:

- Versioned scripts aligned with committed migrations.
- An idempotent deployment script generated from migrations.
- Verification queries for schema, tenant constraints, indexes, and temporal tables.
- Rollback guidance favoring roll-forward correction for production changes.

Maintaining independent hand-written DDL and EF migrations as competing authorities is intentionally avoided.

## 14. Azure production direction

The assessment implementation remains host-agnostic. The documented production path is:

- Azure Container Apps or App Service as the initial managed compute option.
- Azure SQL Database.
- Managed Identity and Key Vault.
- Application Insights and Azure Monitor using OpenTelemetry.
- Container Registry and GitHub Actions deployment.
- Health-based rollout and previous-image rollback.

AKS is a future option only when workload or platform requirements justify its operational cost.

## 15. Related diagrams

- [C4 context](diagrams/c4-context.md)
- [C4 container](diagrams/c4-container.md)
- [Component architecture](diagrams/component-architecture.md)
- [Database ERD](diagrams/database-erd.md)
- [Invoice lifecycle](diagrams/invoice-lifecycle.md)
- [Create invoice sequence](diagrams/create-invoice-sequence.md)
- [Issue invoice sequence](diagrams/issue-invoice-sequence.md)
- [Correlation propagation](diagrams/correlation-propagation.md)
