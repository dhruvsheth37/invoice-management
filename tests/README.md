# Automated Verification

The solution has three complementary test tiers:

- Unit and boundary tests run without infrastructure and cover calculations, rounding, lifecycle rules, validation, correlation, tenant resolution, and EF model contracts.
- Architecture tests enforce exact project references, inward-only assembly dependencies, framework-independent Domain/Application layers, and controller boundaries that expose Application contracts rather than Domain entities or Infrastructure types.
- SQL Server integration tests create a uniquely named database, apply all EF Core migrations, exercise the API in-process, and then drop the database. They cover HTTP workflows, idempotency, tenant isolation, active-row filtering, numbering, temporal history, and row-version concurrency.

## Fast verification

```bash
dotnet restore InvoiceManagement.sln
dotnet build InvoiceManagement.sln --no-restore
dotnet test InvoiceManagement.sln --no-build --no-restore
```

Run only the fast architecture guardrails with:

```bash
dotnet test tests/InvoiceManagement.ArchitectureTests/InvoiceManagement.ArchitectureTests.csproj
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
