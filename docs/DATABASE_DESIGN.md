# Database Design

## 1. Goals

The SQL Server schema is designed to make tenant leakage and invalid financial relationships difficult even when application code is incorrect. It favors explicit constraints, predictable query paths, and auditable lifecycle changes.

EF Core migrations are the schema authority. Phase 2 will generate matching reviewable SQL artifacts under `database/`.

## 2. Conventions

- Schema: `dbo` for current tables; `history` for temporal history tables.
- Primary business identifiers: application-generated `uniqueidentifier` values.
- Tenant-owned tables include `TenantId uniqueidentifier NOT NULL`.
- Money: `decimal(19,4)`.
- Quantity: `decimal(18,4)`.
- Tax rate: `decimal(9,6)` represented as a fraction from 0 through 1.
- Business timestamps: `datetime2(7)` in UTC.
- Audit actors: `nvarchar(200)` containing the stable user or service subject.
- Currency: uppercase ISO 4217 `char(3)`.
- Country: uppercase ISO 3166-1 alpha-2 `char(2)`.
- Mutable aggregate rows use SQL Server `rowversion`.
- Foreign keys use `NO ACTION`; deletion is managed explicitly.
- Names use plural table names and singular C# entity names.

## 3. Shared column groups

### Audit metadata

Applied to mutable tenant-owned business tables:

| Column | SQL type | Null | Notes |
|---|---|---:|---|
| `CreatedUtc` | `datetime2(7)` | No | Set once using application `TimeProvider` |
| `CreatedBy` | `nvarchar(200)` | No | Authenticated subject or system identifier |
| `ModifiedUtc` | `datetime2(7)` | Yes | Updated on mutation |
| `ModifiedBy` | `nvarchar(200)` | Yes | Actor responsible for last mutation |
| `RowVersion` | `rowversion` | No | Optimistic concurrency token |

### Record activation

Applied to `Customers`, `CustomerLocations`, `Invoices`, and `InvoiceLineItems`:

| Column | SQL type | Null | Default |
|---|---|---:|---|
| `IsActive` | `bit` | No | `1` |

Only Draft invoices and their line items may be deactivated. Issued financial records remain active and are voided. `ModifiedUtc` and `ModifiedBy` identify the latest mutation; no deletion-specific columns are stored.

## 4. Tables

### 4.1 `Tenants`

Tenant registry. Tenant retirement also uses `IsActive`.

| Column | SQL type | Null | Notes |
|---|---|---:|---|
| `Id` | `uniqueidentifier` | No | Primary key |
| `Slug` | `nvarchar(100)` | No | Stable external tenant key |
| `Name` | `nvarchar(200)` | No | Display name |
| `IsActive` | `bit` | No | Default `1` |
| `CreatedUtc` | `datetime2(7)` | No | UTC |
| `CreatedBy` | `nvarchar(200)` | No | Provisioning actor |
| `ModifiedUtc` | `datetime2(7)` | Yes | UTC |
| `ModifiedBy` | `nvarchar(200)` | Yes | Last actor |
| `RowVersion` | `rowversion` | No | Concurrency |

Keys and constraints:

- `PK_Tenants (Id)`
- `UQ_Tenants_Slug (Slug)`
- Trimmed, non-empty `Slug` and `Name` checks.

### 4.2 `Customers`

| Column | SQL type | Null | Notes |
|---|---|---:|---|
| `Id` | `uniqueidentifier` | No | Primary key |
| `TenantId` | `uniqueidentifier` | No | FK to `Tenants` |
| `Code` | `nvarchar(50)` | No | Tenant-scoped business code |
| `LegalName` | `nvarchar(200)` | No | Required |
| `TaxNumber` | `nvarchar(50)` | Yes | Jurisdiction-specific identifier |
| `Email` | `nvarchar(254)` | Yes | Billing contact |
| `IsActive` | `bit` | No | Default `1` |
| Audit metadata |  |  | See shared columns |

Keys and relationships:

- `PK_Customers (Id)`
- `AK_Customers_Tenant_Id (TenantId, Id)` supports tenant-aware FKs.
- `FK_Customers_Tenants (TenantId) -> Tenants(Id)`.
- Active unique index on `(TenantId, Code) WHERE IsActive = 1`.

### 4.3 `CustomerLocations`

| Column | SQL type | Null | Notes |
|---|---|---:|---|
| `Id` | `uniqueidentifier` | No | Primary key |
| `TenantId` | `uniqueidentifier` | No | Required |
| `CustomerId` | `uniqueidentifier` | No | Must belong to same tenant |
| `Name` | `nvarchar(150)` | No | Location label |
| `AddressLine1` | `nvarchar(200)` | No | Required |
| `AddressLine2` | `nvarchar(200)` | Yes | Optional |
| `City` | `nvarchar(100)` | No | Required |
| `StateProvince` | `nvarchar(100)` | Yes | Optional |
| `PostalCode` | `nvarchar(20)` | Yes | Optional |
| `CountryCode` | `char(2)` | No | Uppercase ISO code |
| `TaxNumber` | `nvarchar(50)` | Yes | Location-specific identifier |
| `IsActive` | `bit` | No | Default `1` |
| Audit metadata |  |  | See shared columns |

Keys and relationships:

- `PK_CustomerLocations (Id)`
- `AK_CustomerLocations_Tenant_Customer_Id (TenantId, CustomerId, Id)`.
- `AK_CustomerLocations_Tenant_Id (TenantId, Id)` for direct tenant-aware lookups.
- `FK_CustomerLocations_Customers (TenantId, CustomerId) -> Customers(TenantId, Id)`.
- Active unique index on `(TenantId, CustomerId, Name) WHERE IsActive = 1`.

### 4.4 `InvoiceStatuses`

Global, seeded reference data.

| Column | SQL type | Null | Notes |
|---|---|---:|---|
| `Id` | `tinyint` | No | Primary key |
| `Code` | `varchar(32)` | No | Stable machine code |
| `DisplayName` | `nvarchar(50)` | No | Human-readable label |
| `SortOrder` | `tinyint` | No | UI ordering |

Seed values:

| Id | Code | Display name |
|---:|---|---|
| 1 | `Draft` | Draft |
| 2 | `Issued` | Issued |
| 3 | `Paid` | Paid |
| 4 | `Void` | Void |

Lifecycle validity remains a domain concern, not a lookup-table concern.

### 4.5 `Invoices`

System-versioned temporal table with history in `history.InvoicesHistory`.

| Column | SQL type | Null | Notes |
|---|---|---:|---|
| `Id` | `uniqueidentifier` | No | Primary key |
| `TenantId` | `uniqueidentifier` | No | Required |
| `CustomerId` | `uniqueidentifier` | No | Required |
| `CustomerLocationId` | `uniqueidentifier` | No | Must match customer and tenant |
| `BillToCustomerCode` | `nvarchar(50)` | Yes | Immutable issue-time snapshot |
| `BillToLegalName` | `nvarchar(200)` | Yes | Immutable issue-time snapshot |
| `BillToTaxNumber` | `nvarchar(50)` | Yes | Immutable issue-time snapshot |
| `BillToAddressLine1` | `nvarchar(200)` | Yes | Immutable issue-time snapshot |
| `BillToAddressLine2` | `nvarchar(200)` | Yes | Optional snapshot |
| `BillToCity` | `nvarchar(100)` | Yes | Immutable issue-time snapshot |
| `BillToStateProvince` | `nvarchar(100)` | Yes | Optional snapshot |
| `BillToPostalCode` | `nvarchar(20)` | Yes | Optional snapshot |
| `BillToCountryCode` | `char(2)` | Yes | Immutable issue-time snapshot |
| `InvoiceNumber` | `nvarchar(50)` | Yes | Null while Draft; assigned during issue |
| `StatusId` | `tinyint` | No | Default Draft |
| `CurrencyCode` | `char(3)` | No | Uppercase ISO code |
| `IssueDate` | `date` | Yes | Required for non-Draft records |
| `DueDate` | `date` | Yes | Required during issue |
| `PaidDate` | `date` | Yes | Required only for Paid |
| `PaymentReference` | `nvarchar(100)` | Yes | External confirmation reference |
| `Subtotal` | `decimal(19,4)` | No | Server-calculated |
| `TaxTotal` | `decimal(19,4)` | No | Server-calculated |
| `Total` | `decimal(19,4)` | No | `Subtotal + TaxTotal` |
| `Notes` | `nvarchar(1000)` | Yes | Optional |
| `VoidReason` | `nvarchar(500)` | Yes | Required for Void |
| `IsActive` | `bit` | No | Default `1`; only Draft may be deactivated |
| Audit metadata |  |  | See shared columns |
| `ValidFromUtc` | `datetime2(7)` | No | Hidden temporal period start |
| `ValidToUtc` | `datetime2(7)` | No | Hidden temporal period end |

Keys and relationships:

- `PK_Invoices (Id)`.
- `AK_Invoices_Tenant_Id (TenantId, Id)`.
- `FK_Invoices_Tenants (TenantId) -> Tenants(Id)`.
- `FK_Invoices_Customers (TenantId, CustomerId) -> Customers(TenantId, Id)`.
- `FK_Invoices_CustomerLocations (TenantId, CustomerId, CustomerLocationId) -> CustomerLocations(TenantId, CustomerId, Id)`.
- `FK_Invoices_Status (StatusId) -> InvoiceStatuses(Id)`.
- Filtered unique index `(TenantId, InvoiceNumber) WHERE InvoiceNumber IS NOT NULL`.

Important checks:

- `Subtotal >= 0`, `TaxTotal >= 0`, `Total >= 0`.
- `Total = Subtotal + TaxTotal` at four-decimal precision.
- `DueDate IS NULL OR IssueDate IS NULL OR DueDate >= IssueDate`.
- Draft has no invoice number or issue date.
- Issued and Paid records require the invoice number, issue/due dates, and required bill-to snapshot fields.
- Issued/Paid/Void records cannot be deactivated.
- Paid requires `PaidDate`; non-Paid records have no `PaidDate`.
- Void requires `VoidReason`.
- `IsActive = 0` is valid only for Draft records.

### 4.6 `InvoiceLineItems`

System-versioned temporal table with history in `history.InvoiceLineItemsHistory`.

| Column | SQL type | Null | Notes |
|---|---|---:|---|
| `Id` | `uniqueidentifier` | No | Primary key |
| `TenantId` | `uniqueidentifier` | No | Required |
| `InvoiceId` | `uniqueidentifier` | No | Tenant-aware FK |
| `LineNumber` | `smallint` | No | Stable display order |
| `Description` | `nvarchar(500)` | No | Required |
| `Quantity` | `decimal(18,4)` | No | Greater than zero |
| `UnitPrice` | `decimal(19,4)` | No | Non-negative |
| `TaxRate` | `decimal(9,6)` | No | Fraction from 0 through 1 |
| `NetAmount` | `decimal(19,4)` | No | Rounded server calculation |
| `TaxAmount` | `decimal(19,4)` | No | Rounded server calculation |
| `TotalAmount` | `decimal(19,4)` | No | Net plus tax |
| `IsActive` | `bit` | No | Default `1` |
| Audit metadata |  |  | See shared columns |
| `ValidFromUtc` | `datetime2(7)` | No | Hidden temporal period start |
| `ValidToUtc` | `datetime2(7)` | No | Hidden temporal period end |

Keys and relationships:

- `PK_InvoiceLineItems (Id)`.
- `AK_InvoiceLineItems_Tenant_Id (TenantId, Id)`.
- `FK_InvoiceLineItems_Invoices (TenantId, InvoiceId) -> Invoices(TenantId, Id)`.
- Active unique index `(TenantId, InvoiceId, LineNumber) WHERE IsActive = 1`.

Checks:

- `LineNumber > 0`.
- `Quantity > 0`.
- `UnitPrice >= 0`.
- `TaxRate BETWEEN 0 AND 1`.
- Amounts are non-negative and internally consistent after defined rounding.

Invoice totals cannot be SQL computed columns because they aggregate child rows. The domain calculates and persists line and invoice totals in one transaction.

### 4.7 `InvoiceStatusHistory`

Append-only business audit log; not temporal and not subject to activation filtering.

| Column | SQL type | Null | Notes |
|---|---|---:|---|
| `Id` | `uniqueidentifier` | No | Primary key |
| `TenantId` | `uniqueidentifier` | No | Required |
| `InvoiceId` | `uniqueidentifier` | No | Tenant-aware FK |
| `FromStatusId` | `tinyint` | Yes | Null for initial Draft creation |
| `ToStatusId` | `tinyint` | No | Required |
| `Reason` | `nvarchar(500)` | Yes | Business explanation |
| `ChangedUtc` | `datetime2(7)` | No | UTC |
| `ChangedBy` | `nvarchar(200)` | No | User or service subject |
| `CorrelationId` | `varchar(64)` | No | Request correlation |

Relationships:

- `FK_InvoiceStatusHistory_Invoices (TenantId, InvoiceId) -> Invoices(TenantId, Id)`.
- Status foreign keys to `InvoiceStatuses`.

### 4.8 `InvoiceNumberSequences`

| Column | SQL type | Null | Notes |
|---|---|---:|---|
| `TenantId` | `uniqueidentifier` | No | Composite primary key |
| `FiscalYear` | `smallint` | No | Composite primary key |
| `CurrentValue` | `bigint` | No | Last allocated value |
| `ModifiedUtc` | `datetime2(7)` | No | UTC |
| `RowVersion` | `rowversion` | No | Concurrency |

The issue transaction atomically increments the tenant/year row and formats a number such as `INV-2026-000001`. A filtered unique invoice index provides the final uniqueness guarantee.

### 4.9 `IdempotencyRequests`

Operational retry record; not subject to activation filtering or temporal history.

| Column | SQL type | Null | Notes |
|---|---|---:|---|
| `Id` | `uniqueidentifier` | No | Primary key |
| `TenantId` | `uniqueidentifier` | No | Scope |
| `Operation` | `varchar(100)` | No | Stable operation identifier |
| `IdempotencyKey` | `nvarchar(100)` | No | Client key |
| `RequestHash` | `binary(32)` | No | SHA-256 of canonical request |
| `State` | `tinyint` | No | Processing, Completed, or Failed |
| `ResourceId` | `uniqueidentifier` | Yes | Affected resource |
| `ResponseStatus` | `smallint` | Yes | Stored HTTP status |
| `ResponseBody` | `nvarchar(max)` | Yes | Replay payload |
| `CorrelationId` | `varchar(64)` | No | Original execution |
| `CreatedUtc` | `datetime2(7)` | No | UTC |
| `CompletedUtc` | `datetime2(7)` | Yes | UTC |
| `ExpiresUtc` | `datetime2(7)` | No | Retention boundary |

Unique constraint: `(TenantId, Operation, IdempotencyKey)`.

## 5. Index strategy

| Table | Index | Purpose |
|---|---|---|
| `Tenants` | Unique `(Slug)` | Tenant resolution/provisioning |
| `Customers` | Unique filtered `(TenantId, Code)` | Active tenant business key |
| `Customers` | `(TenantId, IsActive, LegalName)` | Active customer lookup |
| `CustomerLocations` | Unique filtered `(TenantId, CustomerId, Name)` | Active location name |
| `Invoices` | Unique filtered `(TenantId, InvoiceNumber)` | Number uniqueness |
| `Invoices` | `(TenantId, IsActive, StatusId, CreatedUtc DESC, Id DESC)` including summary columns | List and dashboard |
| `Invoices` | `(TenantId, IsActive, CustomerId, CreatedUtc DESC)` | Customer invoice history |
| `Invoices` | `(TenantId, IsActive, StatusId, DueDate)` including `CurrencyCode, Total` | Overdue/dashboard query |
| `InvoiceLineItems` | Unique filtered `(TenantId, InvoiceId, LineNumber)` | Stable active lines |
| `InvoiceStatusHistory` | `(TenantId, InvoiceId, ChangedUtc DESC)` | Audit timeline |
| `IdempotencyRequests` | Unique `(TenantId, Operation, IdempotencyKey)` | Request serialization/replay |
| `IdempotencyRequests` | `(ExpiresUtc)` | Future cleanup job |

The final migration and query plans will be verified before accepting additional indexes. Indexes are tenant-leading because all application queries are tenant-scoped.

## 6. Query filters

EF Core filters for activatable tenant entities use both predicates:

```csharp
entity => entity.TenantId == tenantContext.TenantId && entity.IsActive
```

For entities without activation state, only the tenant predicate applies. `IgnoreQueryFilters` is prohibited in normal request handlers and isolated to explicit administrative or integration-test code.

## 7. Retention and reactivation

- Normal reads exclude `IsActive = 0`.
- Customers and locations referenced by active Draft invoices cannot be deactivated.
- Issued invoice details use immutable bill-to snapshot fields, so later customer edits or deactivation do not alter the historical invoice.
- Reactivation, if later introduced, validates uniqueness before setting `IsActive = 1`.
- An invoice may be reactivated only if it remained Draft.
- Temporal history is retained according to a documented production retention policy; the assessment does not add an automated cleanup job.
- Idempotency records have an expiry column, but cleanup processing is a future enhancement.

## 8. Migration artifacts planned for Phase 2

```text
database/
├── README.md
├── scripts/
│   ├── V1.0.0__InitialSchema.sql
│   └── V1.0.1__SeedReferenceData.sql
├── generated/
│   └── InvoiceManagement_Idempotent.sql
├── verification/
│   ├── VerifySchema.sql
│   ├── VerifyTemporalTables.sql
│   └── VerifyTenantConstraints.sql
└── rollback/
    └── README.md
```

Generated SQL must match the committed EF migration. Production rollback guidance favors restoring the previous application image and applying a tested forward-fix migration rather than automatically running destructive down scripts.
