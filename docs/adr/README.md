# Architecture Decision Records

| ADR | Decision | Status |
|---|---|---|
| [001](001-modular-monolith.md) | Modular monolith over microservices | Proposed |
| [002](002-tenant-isolation.md) | Layered tenant isolation | Proposed |
| [003](003-ef-migrations-and-sql-artifacts.md) | EF migrations as schema authority | Proposed |
| [004](004-selective-temporal-tables.md) | Selective temporal tables | Proposed |
| [005](005-business-status-operations.md) | Business lifecycle operations | Proposed |
| [006](006-correlation-and-idempotency.md) | Separate trace, correlation, and idempotency concerns | Proposed |
| [007](007-record-activation.md) | Single active-record flag for business entities | Accepted |

An ADR becomes Accepted after the Phase 1 review. Accepted records are not rewritten to hide later changes; a new ADR supersedes an earlier decision.
