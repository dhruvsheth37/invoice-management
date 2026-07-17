# Architecture Decision Records

| ADR | Decision | Status |
|---|---|---|
| [001](001-modular-monolith.md) | Modular monolith over microservices | Accepted |
| [002](002-tenant-isolation.md) | Layered tenant isolation | Accepted |
| [003](003-ef-migrations-and-sql-artifacts.md) | EF migrations as schema authority | Accepted |
| [004](004-selective-temporal-tables.md) | Selective temporal tables | Accepted |
| [005](005-business-status-operations.md) | Business lifecycle operations | Accepted |
| [006](006-correlation-and-idempotency.md) | Separate trace, correlation, and idempotency concerns | Accepted |
| [007](007-record-activation.md) | Single active-record flag for business entities | Accepted |

All Phase 1 decisions were accepted and implemented. Accepted records are not rewritten to hide later changes; a new ADR supersedes an earlier decision.
