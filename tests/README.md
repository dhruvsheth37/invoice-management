# Automated Verification

The solution has two complementary test tiers:

- Unit and boundary tests run without infrastructure and cover calculations, rounding, lifecycle rules, validation, correlation, tenant resolution, and EF model contracts.
- SQL Server integration tests create a uniquely named database, apply all EF Core migrations, exercise the API in-process, and then drop the database. They cover HTTP workflows, idempotency, tenant isolation, active-row filtering, numbering, temporal history, and row-version concurrency.

## Fast verification

```bash
dotnet restore InvoiceManagement.sln
dotnet build InvoiceManagement.sln --no-restore
dotnet test InvoiceManagement.sln --no-build --no-restore
```

## SQL Server verification

Start the repository SQL Server and export an administrative test connection. The account must be allowed to create and drop databases because each run uses an isolated database.

```bash
cp .env.example .env
docker compose up -d --wait sqlserver

export INVOICE_TEST_SQLSERVER='Server=localhost,1433;Database=master;User Id=sa;Password=Replace_with_a_strong_local_password_123!;Encrypt=True;TrustServerCertificate=True'
dotnet test tests/InvoiceManagement.IntegrationTests/InvoiceManagement.IntegrationTests.csproj
```

When `INVOICE_TEST_SQLSERVER` is absent, the five SQL-dependent tests are reported as skipped while infrastructure-free integration tests continue to run. A supplied connection that cannot create, migrate, or drop the isolated database fails the suite.

To stop the local database:

```bash
docker compose down
```
