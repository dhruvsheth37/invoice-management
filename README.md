# Invoice Management

Production-minded, multi-tenant invoice management API designed for the Qwiik technical assessment.

## Current status

Phase 5 adds automated verification for invoice calculations, lifecycle rules, validation, numbering, HTTP behavior, tenant isolation, idempotency, temporal history, and optimistic concurrency.

## Foundation verification

```bash
dotnet tool restore
dotnet restore InvoiceManagement.sln
dotnet build InvoiceManagement.sln --no-restore
dotnet test InvoiceManagement.sln --no-build --no-restore
```

For local SQL Server setup, see [database/README.md](database/README.md).
For the test tiers and SQL Server integration setup, see [tests/README.md](tests/README.md).

Development API requests require `X-Tenant-Id`; production requires a validated bearer token containing `tenant_id`. See [Security and observability](docs/SECURITY_AND_OBSERVABILITY.md).

## Code quality

The solution enables the .NET SDK analyzers at the latest recommended analysis level and enforces code-style diagnostics during builds. Reliability, security, performance, and maintainability findings are reported as warnings through `.editorconfig`. `TreatWarningsAsErrors` remains `false` in `Directory.Build.props` so the warning baseline can be stabilized before making warnings build-breaking.

## Decision documents

- [Architecture](docs/ARCHITECTURE.md)
- [API design](docs/API_DESIGN.md)
- [Database design](docs/DATABASE_DESIGN.md)
- [Implementation plan](docs/IMPLEMENTATION_PLAN.md)
- [Security and observability](docs/SECURITY_AND_OBSERVABILITY.md)
- [Architecture decision records](docs/adr/README.md)
- [Diagrams](docs/diagrams/README.md)

Implementation begins only after the Phase 1 decisions are reviewed and approved.
