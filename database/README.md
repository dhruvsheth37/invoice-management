# Database Deployment Artifacts

EF Core migrations in `InvoiceManagement.Infrastructure` are the schema authority. This directory contains reviewable and operational SQL artifacts aligned with those migrations.

## Layout

```text
database/
├── scripts/       Versioned review scripts
├── generated/     Idempotent EF migration script
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

`scripts/V1.0.0__InitialSchema.sql` is the readable initial migration script. Released scripts are immutable; later changes receive a new version.

## Local connection

Copy `.env.example` to `.env`, choose a development password, start SQL Server, and provide that password to the API through an environment-specific connection string:

```bash
docker compose up -d sqlserver
export ConnectionStrings__InvoiceDatabase='Server=localhost,1433;Database=InvoiceManagement;User Id=sa;Password=...;Encrypt=True;TrustServerCertificate=True'
```

Do not commit `.env`, production passwords, or access tokens.
