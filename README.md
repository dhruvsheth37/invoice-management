# Invoice Management

Production-minded, multi-tenant invoice management API designed for the Qwiik technical assessment.

## Current status

Phase 4 hardens the API boundary with environment-specific authentication, trusted tenant-claim resolution, correlation and W3C tracing, structured JSON logs, centralized ProblemDetails, tenant-partitioned rate limiting, request timeouts, and JSON health responses.

## Foundation verification

```bash
dotnet tool restore
dotnet restore InvoiceManagement.sln
dotnet build InvoiceManagement.sln --no-restore
dotnet test InvoiceManagement.sln --no-build --no-restore
```

For local SQL Server setup, see [database/README.md](database/README.md).

Development API requests require `X-Tenant-Id`; production requires a validated bearer token containing `tenant_id`. See [Security and observability](docs/SECURITY_AND_OBSERVABILITY.md).

## Decision documents

- [Architecture](docs/ARCHITECTURE.md)
- [API design](docs/API_DESIGN.md)
- [Database design](docs/DATABASE_DESIGN.md)
- [Implementation plan](docs/IMPLEMENTATION_PLAN.md)
- [Security and observability](docs/SECURITY_AND_OBSERVABILITY.md)
- [Architecture decision records](docs/adr/README.md)
- [Diagrams](docs/diagrams/README.md)

Implementation begins only after the Phase 1 decisions are reviewed and approved.
