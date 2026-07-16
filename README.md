# Invoice Management

Production-minded, multi-tenant invoice management API designed for the Qwiik technical assessment.

## Current status

Phase 2 establishes the .NET 10 solution, domain foundation, EF Core SQL Server model, initial migration, selective temporal history, soft-deletion metadata, health checks, and database deployment artifacts. Core HTTP invoice workflows are planned for Phase 3.

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
