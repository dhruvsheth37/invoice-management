# Database ER Diagram

```mermaid
erDiagram
    TENANTS {
        uniqueidentifier Id PK
        nvarchar Slug UK
        nvarchar Name
        bit IsActive
        datetime2 CreatedUtc
        rowversion RowVersion
    }

    CUSTOMERS {
        uniqueidentifier Id PK
        uniqueidentifier TenantId FK
        nvarchar Code
        nvarchar LegalName
        nvarchar TaxNumber
        nvarchar Email
        bit IsActive
        bit IsDeleted
        datetime2 DeletedUtc
        nvarchar DeletedBy
        datetime2 CreatedUtc
        rowversion RowVersion
    }

    CUSTOMER_LOCATIONS {
        uniqueidentifier Id PK
        uniqueidentifier TenantId FK
        uniqueidentifier CustomerId FK
        nvarchar Name
        nvarchar AddressLine1
        nvarchar City
        char CountryCode
        bit IsActive
        bit IsDeleted
        datetime2 DeletedUtc
        nvarchar DeletedBy
        datetime2 CreatedUtc
        rowversion RowVersion
    }

    INVOICE_STATUSES {
        tinyint Id PK
        varchar Code UK
        nvarchar DisplayName
    }

    INVOICES {
        uniqueidentifier Id PK
        uniqueidentifier TenantId FK
        uniqueidentifier CustomerId FK
        uniqueidentifier CustomerLocationId FK
        nvarchar BillToLegalName
        char BillToCountryCode
        nvarchar InvoiceNumber
        tinyint StatusId FK
        char CurrencyCode
        date IssueDate
        date DueDate
        date PaidDate
        decimal Subtotal
        decimal TaxTotal
        decimal Total
        bit IsDeleted
        datetime2 DeletedUtc
        nvarchar DeletedBy
        datetime2 ValidFromUtc
        datetime2 ValidToUtc
        rowversion RowVersion
    }

    INVOICE_LINE_ITEMS {
        uniqueidentifier Id PK
        uniqueidentifier TenantId FK
        uniqueidentifier InvoiceId FK
        smallint LineNumber
        nvarchar Description
        decimal Quantity
        decimal UnitPrice
        decimal TaxRate
        decimal NetAmount
        decimal TaxAmount
        decimal TotalAmount
        bit IsDeleted
        datetime2 DeletedUtc
        nvarchar DeletedBy
        datetime2 ValidFromUtc
        datetime2 ValidToUtc
        rowversion RowVersion
    }

    INVOICE_STATUS_HISTORY {
        uniqueidentifier Id PK
        uniqueidentifier TenantId FK
        uniqueidentifier InvoiceId FK
        tinyint FromStatusId FK
        tinyint ToStatusId FK
        nvarchar Reason
        datetime2 ChangedUtc
        nvarchar ChangedBy
        varchar CorrelationId
    }

    INVOICE_NUMBER_SEQUENCES {
        uniqueidentifier TenantId PK,FK
        smallint FiscalYear PK
        bigint CurrentValue
        datetime2 ModifiedUtc
        rowversion RowVersion
    }

    IDEMPOTENCY_REQUESTS {
        uniqueidentifier Id PK
        uniqueidentifier TenantId FK
        varchar Operation
        nvarchar IdempotencyKey
        binary RequestHash
        tinyint State
        uniqueidentifier ResourceId
        smallint ResponseStatus
        varchar CorrelationId
        datetime2 CreatedUtc
        datetime2 ExpiresUtc
    }

    INVOICES_HISTORY {
        uniqueidentifier Id
        uniqueidentifier TenantId
        bit IsDeleted
        datetime2 ValidFromUtc
        datetime2 ValidToUtc
    }

    INVOICE_LINE_ITEMS_HISTORY {
        uniqueidentifier Id
        uniqueidentifier TenantId
        uniqueidentifier InvoiceId
        bit IsDeleted
        datetime2 ValidFromUtc
        datetime2 ValidToUtc
    }

    TENANTS ||--o{ CUSTOMERS : owns
    TENANTS ||--o{ CUSTOMER_LOCATIONS : owns
    CUSTOMERS ||--o{ CUSTOMER_LOCATIONS : has
    TENANTS ||--o{ INVOICES : owns
    CUSTOMERS ||--o{ INVOICES : billed_to
    CUSTOMER_LOCATIONS ||--o{ INVOICES : billed_at
    INVOICE_STATUSES ||--o{ INVOICES : current_status
    INVOICES ||--|{ INVOICE_LINE_ITEMS : contains
    INVOICES ||--o{ INVOICE_STATUS_HISTORY : records
    INVOICE_STATUSES ||--o{ INVOICE_STATUS_HISTORY : transition
    TENANTS ||--o{ INVOICE_NUMBER_SEQUENCES : numbers
    TENANTS ||--o{ IDEMPOTENCY_REQUESTS : scopes
    INVOICES ||--o{ INVOICES_HISTORY : temporal_versions
    INVOICE_LINE_ITEMS ||--o{ INVOICE_LINE_ITEMS_HISTORY : temporal_versions
```

The ERD summarizes the most decision-relevant fields. [Database Design](../DATABASE_DESIGN.md) is authoritative for exact SQL types, nullability, audit fields, constraints, and indexes.
