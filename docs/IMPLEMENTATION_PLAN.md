# Implementation Plan

## Delivery rules

- Phase 1 is documentation only and requires approval before implementation.
- Each later phase must build and test before it is pushed.
- Each push is a focused commit with no unrelated work.
- EF Core migrations remain the schema authority; SQL artifacts must be generated from or reconciled with them.
- Features listed as future enhancements are documented, not stubbed.

## Phase 1: architecture and database decision pack

Deliverables:

- Architecture, API, and database specifications.
- C4, component, ER, lifecycle, and sequence diagrams.
- Architecture decision records.
- Exact record-activation and temporal-table policies.
- Assessment-versus-future matrix.

Gate:

- User approves the model, API, and scope before code generation.

Commit: `docs: define architecture API and database design`

## Phase 2: solution foundation and database

Deliverables:

- Four source projects and two test projects.
- Domain model and EF Core mapping.
- Tenant query filters and tenant-aware composite relationships.
- `IsActive` filtering and Draft-only invoice deactivation constraints.
- Temporal mapping for invoices and lines.
- Initial migration, seed data, Docker Compose, and database artifacts.

Verification:

- Clean restore/build.
- Migration applies to a fresh SQL Server.
- Temporal and tenant verification scripts pass.
- Cross-tenant foreign keys fail.

Commit: `feat: establish solution foundation and database schema`

## Phase 3: core invoice API

Deliverables:

- Draft creation, list, details, issue, mark paid, void, and dashboard.
- Server-calculated totals and atomic invoice numbering.
- Validation, status history, idempotency, and concurrency.
- OpenAPI documentation.

Verification:

- Every assessment endpoint works.
- Invalid transitions are rejected.
- Inactive rows are invisible.
- Issued financial records cannot be deactivated or changed.

Commit: `feat: implement tenant-scoped invoice workflows`

## Phase 4: security, errors, and observability

Deliverables:

- Tenant resolution, development header policy, and production JWT strategy.
- W3C tracing and correlation middleware.
- Structured logging, `ProblemDetails`, rate limits, timeouts, and health checks.

Verification:

- Tenant context is mandatory.
- Correlation is present on successful and failed responses.
- Internal exception details are not exposed.

Commit: `feat: add tenant security error handling and observability`

## Phase 5: automated verification

Deliverables:

- Unit tests for calculations, lifecycle rules, validation, and numbering.
- SQL Server integration tests for API behavior, isolation, temporal history, idempotency, and concurrency.

Verification:

- `dotnet build` succeeds.
- `dotnet test` succeeds.
- Docker Compose configuration validates.

Commit: `test: verify invoice rules tenant isolation and persistence`

## Phase 6: submission and CI

Deliverables:

- Complete `README.md`, `SOLUTION_NOTES.md`, and `AI_USAGE.md`.
- GitHub Actions build/test workflow.
- Azure, security, performance, limitations, and roadmap documentation.
- Clean-clone run verification.

Commit: `docs: finalize assessment documentation and CI`

## Phase 7: architecture guardrails

Deliverables:

- Dedicated architecture-test project included in the solution test command.
- Exact inward project-reference and assembly-dependency checks.
- Controller boundary checks preventing Domain entities and Infrastructure types from entering HTTP contracts.
- API convention checks for routes, HTTP methods, response metadata, cancellation, and interface-based Application dependencies.

Verification:

- Architecture tests run without SQL Server or container infrastructure.
- Existing controllers pass the enforced boundaries and API conventions.
- `dotnet build` and the architecture test project succeed with zero warnings.

Commit: `test: enforce architecture and API boundaries`

## Implementation versus future enhancements

| Capability | Assessment implementation | Future enhancement |
|---|---|---|
| Architecture | Four-project modular monolith | Additional projects or service extraction when justified |
| SQL Server | Single primary database | Azure SQL read replicas |
| Temporal data | Invoices and line items | All appropriate transactional tables |
| Audit | Audit metadata, temporal rows, explicit status history | Centralized immutable audit stream |
| Record activation | `IsActive` and filters on business entities; Draft invoice policy | Administrative reactivation and retention workflows |
| Cache | None | Redis |
| Messaging | None | Azure Service Bus, outbox, idempotent consumers |
| Background work | None | Azure Functions and ETL |
| Hosting | Docker-ready, Azure-documented | AKS if operational requirements justify it |
| Database enforcement | Constraints and composite FKs | SQL triggers for selected immutable rules if required |
| Payments | External paid confirmation | Payment, allocation, reversal, and reconciliation module |

## Phase 1 review checklist

- Is the four-project modular monolith accepted?
- Are Customer and CustomerLocation required in the assessment implementation?
- Are the Draft, Issued, Paid, and Void states accepted?
- Are business-operation endpoints preferred over generic status patching?
- Is the activation policy accepted, especially Draft-only invoice deactivation?
- Are temporal tables limited to invoices and line items?
- Are EF migrations accepted as the schema authority?
- Is development-only `X-Tenant-Id` with production JWT claims accepted?
- Is the payment domain correctly deferred?
