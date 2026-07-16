# Database Deployment Artifacts

EF Core migrations in `InvoiceManagement.Infrastructure` are the schema authority. This directory contains reviewable and operational SQL artifacts aligned with those migrations.

## Layout

```text
database/
├── scripts/       Versioned review scripts
├── generated/     Idempotent EF migration script
├── seed/          Idempotent local demonstration data
├── verification/  Read-only post-deployment checks
└── rollback/      Recovery and roll-forward guidance
```

## Generate the idempotent script

```bash
dotnet tool restore
dotnet ef migrations script \
  --idempotent \
  --project src/InvoiceManagement.Infrastructure \
  --startup-project src/InvoiceManagement.Infrastructure \
  --output database/generated/InvoiceManagement_Idempotent.sql
```

`scripts/V1.0.0__InitialSchema.sql` is the readable initial migration script. `scripts/V1.0.1__UseIntegerAuditUserIds.sql` migrates audit actors to required integer user IDs with a default of `1`. `scripts/V1.0.2__AddInvoiceQueryIndexes.sql` adds the keyset-pagination and dashboard covering indexes. Released scripts are immutable; later changes receive a new version.

`seed/SeedDemoData.sql` is an idempotent local-only data set covering every application table and all invoice lifecycle states. Run it only after the migrations; it is not part of production deployment.

## Local connection

Copy `.env.example` to `.env`, choose a development password, start SQL Server, and provide that password to the API through an environment-specific connection string:

```bash
docker compose up -d sqlserver
export ConnectionStrings__InvoiceDatabase='Server=localhost,1433;Database=InvoiceManagement;User Id=sa;Password=${SQL_SA_PASSWORD};Encrypt=True;TrustServerCertificate=True'
```

Do not commit `.env`, production passwords, or access tokens.
