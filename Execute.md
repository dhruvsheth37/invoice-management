# Run and Exercise the Invoice Management API

This guide starts SQL Server, applies the EF Core migrations, adds the minimum demo customer data, runs the API, and exercises the invoice endpoints.

## Prerequisites

- .NET 10 SDK
- Docker with Docker Compose, or Podman with Podman Compose
- `curl`, Postman, or an editor that can execute `.http` files

Run all commands from the repository root:

```bash
cd /Users/dhruv.sheth/Documents/Codex/2026-07-16/i-am/work/invoice-management
```

## 1. Start SQL Server

Create the local environment file the first time:

```bash
cp .env.example .env
```

Review `.env` and choose a strong local password. Then start SQL Server and export the variables from that file:

```bash
docker compose up -d --wait sqlserver

set -a
source .env
set +a
```

Check the container if startup does not complete:

```bash
docker compose ps
docker compose logs sqlserver
```

### Podman alternative

The repository includes scripts for the complete Podman workflow. To start SQL Server, apply migrations, and load all demo data in one operation:

```bash
./scripts/podman/setup.sh
```

The individual operations are:

```bash
./scripts/podman/start.sh
./scripts/podman/migrate.sh
./scripts/podman/seed.sh
./scripts/podman/run-api.sh
./scripts/podman/status.sh
./scripts/podman/stop.sh
```

Podman uses host port `1433` by default. If Docker SQL Server is already using that port, either stop the Docker service or set a different Podman port in `.env`:

```dotenv
PODMAN_SQL_PORT=1434
```

## 2. Configure and migrate the database

The API and EF design-time factory use different environment-variable names, so configure both from one connection string:

```bash
export DB_CONNECTION="Server=localhost,1433;Database=InvoiceManagement;User Id=sa;Password=Replace_with_a_strong_local_password_123!;Encrypt=True;TrustServerCertificate=True"
export INVOICE_DATABASE_CONNECTION="$DB_CONNECTION"
export ConnectionStrings__InvoiceDatabase="$DB_CONNECTION"
```

Restore the solution and apply all migrations:

```bash
dotnet tool restore
dotnet restore InvoiceManagement.sln

dotnet ef database update \
  --project src/InvoiceManagement.Infrastructure \
  --startup-project src/InvoiceManagement.Api \
  --verbose
```

Database migrations are deliberately not executed during API startup.

## 3. Seed comprehensive demo data

The EF migrations seed:

- Demo tenant: `11111111-1111-1111-1111-111111111111`
- Invoice statuses: Draft, Issued, Paid, and Void

Run the dedicated idempotent demo script after applying migrations. It adds examples for every application table, including Draft, Issued, Paid, and Void invoices, lifecycle history, idempotency, number allocation, and system-generated temporal history. The IDs match `InvoiceManagement.Api.http`.

```bash
docker compose exec -T sqlserver \
  /opt/mssql-tools18/bin/sqlcmd \
  -S localhost \
  -U sa \
  -P "Replace_with_a_strong_local_password_123!" \
  -C \
  -b \
  -d InvoiceManagement \
  < database/seed/SeedDemoData.sql
```

With Podman, use the same script with the Podman Compose file:

```bash
podman compose --file podman-compose.yml exec -T sqlserver \
  /opt/mssql-tools18/bin/sqlcmd \
  -S localhost \
  -U sa \
  -P 'Replace_with_a_strong_local_password_123!' \
  -C \
  -b \
  -d InvoiceManagement \
  < database/seed/SeedDemoData.sql
```

`CreatedBy`, `ModifiedBy`, and `ChangedBy` use demo user ID `1`. The script can be run repeatedly without duplicating its fixed-ID sample rows.

## 4. Run the API

Keep the connection-string variables exported in the same terminal and run:

```bash
dotnet run --project src/InvoiceManagement.Api
```

The Development launch profile serves the API at `http://localhost:5080`.

Check process and database health from another terminal:

```bash
curl http://localhost:5080/health/live
curl http://localhost:5080/health/ready
```

## 5. Authentication and operation headers

Development requests use headers instead of a JWT:

| Header | Usage |
|---|---|
| `X-Tenant-Id` | Required for API endpoints. Use the demo tenant ID. |
| `X-Development-User-Id` | Optional positive integer user ID; defaults to `1`. |
| `X-Correlation-ID` | Optional operation correlation identifier. |
| `Idempotency-Key` | Required for create and lifecycle mutations. Use a new value for each distinct operation. |
| `If-Match` | Required for issue, mark-paid, and void. Copy the current invoice `ETag` exactly. |

Health endpoints do not require authentication headers.

## 6. Create a Draft invoice

```bash
curl -i -X POST http://localhost:5080/api/v1/invoices \
  -H "X-Tenant-Id: 11111111-1111-1111-1111-111111111111" \
  -H "X-Development-User-Id: 1" \
  -H "Idempotency-Key: create-example-1" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "00000000-0000-0000-0000-000000000001",
    "customerLocationId": "00000000-0000-0000-0000-000000000002",
    "currencyCode": "USD",
    "dueDate": "2026-08-15",
    "notes": "Net 30",
    "lineItems": [
      {
        "description": "Freight handling",
        "quantity": 2,
        "unitPrice": 125,
        "taxRate": 0.18
      }
    ]
  }'
```

Save the returned invoice `id` and `ETag`. Reusing `create-example-1` with the same body replays the stored result; reusing it with a different body is rejected.

## 7. Search and retrieve invoices

Search:

```bash
curl -X POST http://localhost:5080/api/v1/invoices/search \
  -H "X-Tenant-Id: 11111111-1111-1111-1111-111111111111" \
  -H "Content-Type: application/json" \
  -d '{
    "pageSize": 25,
    "status": null,
    "customerId": null,
    "from": null,
    "to": null,
    "dueFrom": null,
    "dueTo": null,
    "invoiceNumber": null,
    "sort": "-createdUtc",
    "continuationToken": null,
    "includeTotalCount": true
  }'
```

If `continuationToken` is returned, pass it unchanged in the next request. Set `includeTotalCount` only when an exact count is required; omitting it avoids the additional count query.

Retrieve one invoice, replacing `{invoiceId}`:

```bash
curl -i \
  -H "X-Tenant-Id: 11111111-1111-1111-1111-111111111111" \
  http://localhost:5080/api/v1/invoices/{invoiceId}
```

## 8. Execute lifecycle operations

Every lifecycle response returns a new `ETag`. Use that new value for the next mutation.

Issue a Draft:

```bash
curl -i -X POST http://localhost:5080/api/v1/invoices/{invoiceId}/issue \
  -H "X-Tenant-Id: 11111111-1111-1111-1111-111111111111" \
  -H "X-Development-User-Id: 1" \
  -H "Idempotency-Key: issue-example-1" \
  -H 'If-Match: "{base64-etag}"' \
  -H "Content-Type: application/json" \
  -d '{
    "issueDate": "2026-07-16",
    "dueDate": "2026-08-15"
  }'
```

Mark an Issued invoice as Paid:

```bash
curl -i -X POST http://localhost:5080/api/v1/invoices/{invoiceId}/mark-paid \
  -H "X-Tenant-Id: 11111111-1111-1111-1111-111111111111" \
  -H "X-Development-User-Id: 1" \
  -H "Idempotency-Key: paid-example-1" \
  -H 'If-Match: "{base64-etag}"' \
  -H "Content-Type: application/json" \
  -d '{
    "paidDate": "2026-07-17",
    "reference": "PAYMENT-1001"
  }'
```

Void a Draft or Issued invoice:

```bash
curl -i -X POST http://localhost:5080/api/v1/invoices/{invoiceId}/void \
  -H "X-Tenant-Id: 11111111-1111-1111-1111-111111111111" \
  -H "X-Development-User-Id: 1" \
  -H "Idempotency-Key: void-example-1" \
  -H 'If-Match: "{base64-etag}"' \
  -H "Content-Type: application/json" \
  -d '{
    "reason": "Customer cancellation"
  }'
```

A Paid invoice cannot be voided. To test both Paid and Void paths, create separate Draft invoices.

## 9. Dashboard summary

```bash
curl \
  -H "X-Tenant-Id: 11111111-1111-1111-1111-111111111111" \
  "http://localhost:5080/api/v1/dashboard/invoice-summary?asOf=2026-08-20"
```

## 10. Alternative request runner

The repository includes ready-to-run requests in:

```text
src/InvoiceManagement.Api/InvoiceManagement.Api.http
```

Update `@invoiceId` after creating an invoice. The project does not currently expose Swagger UI, so use this file, `curl`, or Postman.

## 11. Stop the environment

Stop the containers while retaining SQL Server data:

```bash
docker compose down
```

Remove containers and the local SQL Server volume:

```bash
docker compose down -v
```
