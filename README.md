# Invoice Management

Production-minded, multi-tenant invoice management API designed for the Qwiik technical assessment.

## Current status

Phase 3 adds the tenant-scoped HTTP workflows for Draft creation, invoice queries, issue, mark-paid, void, and dashboard summaries. It includes server-calculated totals, bill-to snapshots, atomic numbering, idempotency, ETags, validation, and the reviewable OpenAPI contract in `docs/openapi.yaml`.

## Foundation verification

```bash
dotnet tool restore
dotnet restore InvoiceManagement.sln
dotnet build InvoiceManagement.sln --no-restore
dotnet test InvoiceManagement.sln --no-build --no-restore
```

For local SQL Server setup, see [database/README.md](database/README.md).

## Decision documents

- [Architecture](docs/ARCHITECTURE.md)
- [API design](docs/API_DESIGN.md)
- [Database design](docs/DATABASE_DESIGN.md)
- [Implementation plan](docs/IMPLEMENTATION_PLAN.md)
- [Architecture decision records](docs/adr/README.md)
- [Diagrams](docs/diagrams/README.md)

Implementation begins only after the Phase 1 decisions are reviewed and approved.
