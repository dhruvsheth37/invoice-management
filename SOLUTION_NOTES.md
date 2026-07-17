# Solution Notes

This document is the assessment handoff for the Invoice Management API. More detailed decision records are available under [`docs`](docs/), while [`Execute.md`](Execute.md) contains an expanded local walkthrough and ready-to-run endpoint examples.

## 1. How to run the project

### Prerequisites

- .NET 10 SDK
- Docker with Docker Compose, or Podman with Podman Compose
- `curl`, Postman, or an editor capable of running `.http` files

Run commands from the repository root.

### Recommended Podman workflow

Create the local environment file and set a strong local SQL Server password:

```bash
cp .env.example .env
```

Start SQL Server, apply migrations, and load demo data for every application table:

```bash
./scripts/podman/setup.sh
```

Run the API:

```bash
./scripts/podman/run-api.sh
```

If port `1433` is already occupied, set `PODMAN_SQL_PORT=1434` in `.env` before running setup. The individual Podman operations are `start.sh`, `migrate.sh`, `seed.sh`, `status.sh`, and `stop.sh` under `scripts/podman`.

### Docker or manually managed SQL Server

```bash
docker compose up -d --wait sqlserver

export DB_CONNECTION="Server=localhost,1433;Database=InvoiceManagement;User Id=sa;Password=<strong-local-password>;Encrypt=True;TrustServerCertificate=True"
export INVOICE_DATABASE_CONNECTION="$DB_CONNECTION"
export ConnectionStrings__InvoiceDatabase="$DB_CONNECTION"

dotnet tool restore
dotnet restore InvoiceManagement.sln
dotnet ef database update \
  --project src/InvoiceManagement.Infrastructure \
  --startup-project src/InvoiceManagement.Api

dotnet run --project src/InvoiceManagement.Api
```

Migrations intentionally do not run at API startup. The idempotent sample-data script is [`database/seed/SeedDemoData.sql`](database/seed/SeedDemoData.sql); it must be run separately after migrations. `dotnet ef database update` does not execute that script.

The Development launch profile listens on `http://localhost:5080`. Verify it with:

```bash
curl http://localhost:5080/health/live
curl http://localhost:5080/health/ready
```

Development API requests require `X-Tenant-Id`. The demo tenant ID is `11111111-1111-1111-1111-111111111111`; `X-Development-User-Id` is optional and defaults to `1`. Ready-to-run requests are in [`src/InvoiceManagement.Api/InvoiceManagement.Api.http`](src/InvoiceManagement.Api/InvoiceManagement.Api.http).

Run all automated tests with:

```bash
dotnet test InvoiceManagement.sln
```

SQL integration tests use `INVOICE_TEST_SQLSERVER`. They create and drop an isolated test database and are skipped when that variable is absent.

## 2. Assumptions

- The first release is a modular monolith backed by one primary SQL Server/Azure SQL database.
- Tenants share a database and schema; tenant isolation is enforced in every data-access path.
- Customer and customer-location records are reference data for this assessment. Full customer-management APIs are outside the current scope.
- An invoice uses one ISO currency code. Cross-currency conversion and consolidated FX reporting are outside the current scope.
- Invoice totals are calculated by the server, not trusted from client input.
- Invoice numbers are unique within a tenant and year. They are monotonic but not guaranteed to be gapless after rollback or failure.
- The supported statuses are Draft, Issued, Paid, and Void. Paid and Void are terminal.
- `IsActive` is the only logical-deactivation field; there is no separate `IsDeleted` state.
- All business timestamps are stored as UTC, while invoice and due dates are date-only business values.
- Identity and payment providers are external integrations. Development-only header authentication and mark-paid confirmation are substitutes for the assessment.
- Normal writes go through the API. Direct database updates can bypass domain transition rules and are an operational exception.

## 3. Architecture overview

The solution uses a four-project modular monolith with inward-only dependencies:

| Project | Responsibility | Allowed dependencies |
|---|---|---|
| `InvoiceManagement.Domain` | Entities, value rules, calculations, and lifecycle invariants | None |
| `InvoiceManagement.Application` | Use-case interfaces, commands, DTOs, and mapping extensions | Domain |
| `InvoiceManagement.Infrastructure` | EF Core, SQL Server mappings, migrations, and use-case implementations | Application and Domain |
| `InvoiceManagement.Api` | HTTP controllers, authentication, middleware, errors, OpenAPI, and composition root | Application and Infrastructure composition |

Controllers depend on Application interfaces and DTOs. They do not expose Domain entities or use Infrastructure implementation types. Architecture tests enforce these boundaries.

A request passes through exception handling, correlation, routing, authentication, tenant resolution, logging, rate limiting, timeout, authorization, the controller, an Application contract, and the Infrastructure implementation. The service applies domain rules and persists through EF Core. The API remains stateless and can scale horizontally.

This structure provides clear ownership without creating premature microservice deployment and operational overhead. `CorrelationId`, tenant context, application interfaces, and transaction boundaries leave a practical path to split services later if scale or team ownership requires it.

## 4. Domain model explanation

The principal aggregate is `Invoice`.

- **Tenant** is the ownership boundary for all business data.
- **Customer** represents the invoiced party.
- **CustomerLocation** provides a tenant-owned billing location.
- **Invoice** holds customer references, dates, currency, lifecycle state, monetary totals, bill-to snapshot, concurrency token, and audit data.
- **InvoiceLineItem** holds description, quantity, unit price, tax rate, and server-calculated values.
- **InvoiceStatusHistory** is an append-only audit of lifecycle changes, including the previous and new states, reason, actor, timestamp, and correlation identifier.
- **InvoiceNumberSequence** allocates tenant/year invoice numbers without scanning the invoice table.
- **IdempotencyRequest** stores mutation keys, request hashes, and replayable responses.

The lifecycle rules are:

| Current status | Allowed next status |
|---|---|
| Draft | Issued or Void |
| Issued | Paid or Void |
| Paid | None |
| Void | None |

Issue allocates an invoice number and freezes the bill-to snapshot. Mark-paid records payment confirmation data. Void records an explicit reason. Separate operations make illegal transitions harder to express and preserve a meaningful audit trail.

Line amounts and totals use decimal arithmetic. Quantities must be positive, prices non-negative, and tax rates within the supported range. Monetary values are rounded consistently to four decimal places.

## 5. Database design explanation

EF Core migrations are the executable schema authority. The database contains:

- `Tenants`
- `Customers`
- `CustomerLocations`
- `InvoiceStatuses`
- `Invoices`
- `InvoiceLineItems`
- `InvoiceStatusHistory`
- `InvoiceNumberSequences`
- `IdempotencyRequests`

Tenant-owned tables contain `TenantId`. Tenant-leading alternate/composite keys and composite foreign keys prevent a record from referencing another tenant's customer, location, invoice, sequence, or idempotency record.

`Invoices` and `InvoiceLineItems` are SQL Server temporal tables. They preserve business-critical before-and-after data without applying temporal overhead to every table. Status history remains an explicit domain audit rather than being inferred from temporal rows.

`CreatedBy` and `ModifiedBy` are required integer user IDs with a local/default value of `1`; status-history `ChangedBy` is also an integer user ID. `IsActive` provides logical deactivation. `rowversion` supports optimistic concurrency.

Check constraints protect important invariants even if an unexpected writer bypasses the application, including valid monetary values, dates, and status-dependent field combinations. Demo data is provided as an idempotent SQL script, separate from schema migrations.

## 6. API design explanation

The versioned JSON API is rooted at `/api/v1`.

| Method and route | Purpose |
|---|---|
| `POST /invoices` | Create a Draft invoice |
| `POST /invoices/search` | Filter and cursor-page invoices using a request body |
| `GET /invoices/{invoiceId}` | Retrieve invoice details and lifecycle history |
| `POST /invoices/{invoiceId}/issue` | Issue a Draft invoice |
| `POST /invoices/{invoiceId}/mark-paid` | Mark an Issued invoice as Paid |
| `POST /invoices/{invoiceId}/void` | Void a Draft or Issued invoice |
| `GET /dashboard/invoice-summary` | Return SQL-aggregated invoice summaries |

Search uses POST because its optional filters, date ranges, sorting, continuation token, and count preference form a structured query contract that would become unwieldy in a query string. It remains a read-only operation.

Lifecycle endpoints remain separate rather than accepting a generic status update. Issue, payment, and voiding have distinct validation, request data, audit meaning, authorization opportunities, and future side effects. Explicit commands make the API safer and more cohesive.

Responses use ISO dates, UTC timestamps, ISO currency codes, typed DTOs, and standard HTTP status codes. Errors use Problem Details and include trace/correlation context. Mutations support `Idempotency-Key`; invoice responses expose an `ETag`, and lifecycle mutations require `If-Match` to prevent lost updates.

## 7. Validation approach

Validation is deliberately layered:

1. **HTTP contract validation** rejects malformed JSON, invalid route values, and missing required headers.
2. **Application validation** checks tenant-owned references, active records, command-specific requirements, date relationships, idempotency, and concurrency preconditions.
3. **Domain validation** protects calculations and lifecycle invariants regardless of the caller.
4. **Database constraints** provide a final integrity boundary for relationships and data shape.

Validation failures are converted by centralized exception handling into consistent Problem Details responses. Clients never provide authoritative totals or lifecycle audit fields.

## 8. Tenant isolation approach

The application uses shared-database, shared-schema multi-tenancy with defence in depth:

- production JWTs must contain a valid `tenant_id` claim;
- Development can use `X-Tenant-Id`, but that header path is not enabled as production authentication;
- tenant middleware resolves one immutable request tenant context;
- EF Core global query filters apply `TenantId` and `IsActive` to tenant-owned reads;
- all service lookups and writes carry the tenant identifier;
- tenant-leading keys and composite foreign keys reject cross-tenant relationships at the database layer;
- idempotency and invoice-number sequences are tenant-scoped;
- logs and status-history records preserve correlation and actor context.

Pooled EF contexts do not retain tenant state between requests; the tenant is supplied through request-scoped context. Integration tests exercise tenant isolation and cross-tenant access attempts.

## 9. Indexing and performance strategy

The implemented strategy targets the expected access patterns rather than adding indexes indiscriminately:

- indexes lead with `TenantId`, followed by status, dates, customer, or invoice number as required by supported filters;
- filtered unique indexes enforce issued invoice-number uniqueness while allowing Draft numbers to remain null;
- active-row and covering indexes support common invoice search, cursor sorting, and dashboard aggregation;
- invoice search uses keyset/cursor pagination based on the selected sort and `Id`, avoiding increasingly expensive large offsets;
- exact total count is optional so clients can avoid an additional `COUNT` query;
- dashboard grouping, counts, and sums execute in SQL instead of loading invoices into API memory;
- detail queries use split-query loading to avoid Cartesian expansion of line items and status history;
- a narrow atomic allocation command reduces invoice-number contention;
- EF contexts are pooled, with request tenant state kept outside persistent context state;
- source-generated logging reduces hot-path formatting work, successful request-completion logs are kept at Debug, and health-check noise is excluded.

Compiled EF queries are intentionally deferred until profiling shows query-compilation cost is material. Query design, indexes, round trips, and database execution plans have higher priority.

## 10. Testing approach

The solution has three complementary test tiers:

- **Unit tests** cover calculations, validation, lifecycle transitions, and application behavior without external infrastructure.
- **Integration tests** exercise EF Core and API behavior against SQL Server, including migrations, tenant isolation, numbering, idempotency, concurrency, temporal behavior, and HTTP contracts.
- **Architecture tests** enforce dependency direction, keep ASP.NET Core and EF Core out of inner layers, prevent controllers from using Domain entities or Infrastructure implementation types, and require explicit HTTP metadata and cancellation support.

The SDK analyzers run at the latest recommended analysis level. Warnings are currently visible but are not yet build-breaking; the intended next step is to stabilize the warning baseline and enable warnings-as-errors.

The normal verification command is:

```bash
dotnet build InvoiceManagement.sln
dotnet test InvoiceManagement.sln
```

## 11. Azure deployment and monitoring considerations

The recommended initial Azure topology is:

- Azure Container Apps or Azure App Service for the stateless API;
- Azure SQL Database for persistence;
- Managed Identity for service-to-service authentication;
- Azure Key Vault for secrets and signing/configuration material;
- Application Insights and Azure Monitor, ideally through OpenTelemetry-compatible instrumentation;
- Azure Container Registry for immutable application images.

Database migrations should run as a controlled deployment step using a dedicated identity, not automatically during application startup. Deployment should use staged environments, health probes, backward-compatible schema changes, and a rollback strategy. The existing `/health/live` and `/health/ready` endpoints map naturally to platform probes.

Production monitoring should cover error rate, p50/p95/p99 latency, request volume, dependency duration, SQL CPU and capacity, deadlocks, connection-pool pressure, rate-limit rejections, health failures, and migration failures. Logs should include tenant, correlation, trace, route, status, and duration while excluding secrets and sensitive invoice content. Alerts should be actionable and tied to service-level objectives.

AKS is not recommended for the initial workload unless organizational platform standards or operational requirements justify its additional complexity.

## 12. Security considerations

- Production uses JWT bearer authentication with configured authority and audience, HTTPS metadata, and validated `tenant_id` and positive `user_id` claims.
- Controllers require authorization; only health endpoints are anonymous.
- Secrets must come from environment configuration or Key Vault. Local sample credentials are not production credentials.
- Parameterized EF Core/SQL operations reduce SQL-injection exposure.
- Tenant filters and composite keys reduce insecure direct-object-reference risk.
- ETags and `If-Match` prevent accidental lost updates.
- Idempotency keys prevent duplicate mutation effects during retries.
- Tenant-partitioned rate limiting and request timeouts bound abusive or unexpectedly expensive requests.
- Problem Details avoids exposing stack traces in normal API responses.
- Structured logs should avoid request bodies, tokens, credentials, and unnecessary personal or financial data.
- Dependencies and container images should be scanned in CI, and production identities should use least privilege.

The Development header-authentication handler is intentionally convenient and must never be treated as a production authentication mechanism.

## 13. Known limitations

- Customer and customer-location management endpoints are not implemented.
- Draft editing and explicit deactivation endpoints are not implemented.
- Payment processing, partial payments, refunds, allocations, and reconciliation are outside scope; mark-paid records an external confirmation.
- The dashboard currently provides core currency/status/overdue aggregation. It does not yet provide an executive summary, month-wise trends, or InvoiceType breakdowns.
- Invoice types such as standard, pro-forma, credit note, or debit note have not been modelled. Credit notes would require additional amount and lifecycle rules.
- There is no foreign-exchange conversion or consolidated multi-currency reporting.
- The current rate limiter is process-local, so limits are approximate when multiple API instances run.
- There is no outbox, event bus, background worker, distributed cache, or read-replica routing.
- Authorization is authenticated-user based; fine-grained roles and permissions are not yet modelled.
- Azure infrastructure-as-code and an end-to-end production deployment pipeline are not included.
- Direct database writes can bypass application lifecycle rules; database triggers were intentionally not added.

## 14. What I would improve with more time

In priority order, I would:

1. Add production CI/CD with formatting, analyzers, warnings-as-errors, unit/integration/architecture tests, dependency scanning, container scanning, migration validation, and deployment gates.
2. Complete production authentication and authorization integration with an enterprise identity provider. Add role- and claim-based authorization policies for invoice creation, issuing, payment confirmation, voiding, reporting, and tenant administration, together with policy tests and least-privilege service identities.
3. Evolve the clean architecture toward deeper Domain-Driven Design as the domain grows: define bounded contexts, aggregate ownership, domain events, value objects, and anti-corruption layers. Preserve the current inward dependency rules and extract a bounded context into a microservice only when independent scaling, deployment, or team ownership provides a concrete benefit.
4. Replace the current process-local rate limiter with a distributed, tenant-aware strategy for multi-instance deployment. Introduce per-route limits, tenant plans or quotas, retry headers, metrics, administrative overrides, and load tests. A managed gateway could enforce coarse external limits while the API retains business-aware protection.
5. Introduce CQRS where read and write needs genuinely diverge. Keep transactional commands on the primary database and build independently optimized read models for dashboards and reporting. Read replicas may serve latency-tolerant queries; an outbox plus ETL or event-driven projections would update analytical stores while explicitly handling replication lag, retries, ordering, and reconciliation.
6. Strengthen the middleware pipeline with automated ordering tests and dedicated components for security headers, API version policy, richer audit context, and response compression where appropriate. Continue excluding sensitive payloads from logs and keep tenant resolution, authentication, correlation, exception handling, rate limiting, and timeout responsibilities independently testable.
7. Add OpenTelemetry traces and metrics, an Application Insights exporter, dashboards, service-level objectives, and tested alerts.
8. Expand the reporting model after confirming business definitions for executive metrics, month-wise grouping, aging, and InvoiceType. Use SQL projection first and a stored procedure only if measured plans justify it.
9. Add draft amendment, customer/reference management, payment allocation, credit-note, and reconciliation workflows with matching audit and tests.
10. Add infrastructure-as-code, managed identities, Key Vault integration, backup/restore testing, temporal-retention policy, and zero-downtime migration practices.
11. Add an outbox and asynchronous integration events when real downstream consumers exist; this would also provide a reliable source for CQRS read-model projections and ETL processes.
12. Profile realistic production-scale data and tune indexes, query plans, pool sizes, rate-limit thresholds, replica routing, and archival policies from measurements.

Redis, Azure Service Bus, Azure Functions, AKS, read replicas, temporal tables on every table, SQL triggers, and splitting the solution into ten separate projects are future enhancements only. Each should be introduced in response to measured scale, reliability, integration, or team-ownership needs rather than by default.
