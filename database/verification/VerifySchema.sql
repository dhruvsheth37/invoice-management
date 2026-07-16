SET NOCOUNT ON;

DECLARE @RequiredTables TABLE (TableName sysname NOT NULL);
INSERT INTO @RequiredTables (TableName)
VALUES
    ('Tenants'),
    ('Customers'),
    ('CustomerLocations'),
    ('InvoiceStatuses'),
    ('Invoices'),
    ('InvoiceLineItems'),
    ('InvoiceStatusHistory'),
    ('InvoiceNumberSequences'),
    ('IdempotencyRequests');

IF EXISTS
(
    SELECT 1
    FROM @RequiredTables AS required
    WHERE OBJECT_ID(QUOTENAME('dbo') + '.' + QUOTENAME(required.TableName), 'U') IS NULL
)
BEGIN
    THROW 51000, 'One or more required invoice-management tables are missing.', 1;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Invoices')
      AND name = 'IsActive'
)
BEGIN
    THROW 51001, 'Invoices.IsActive is missing.', 1;
END;

IF EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id IN
    (
        OBJECT_ID('dbo.Customers'),
        OBJECT_ID('dbo.CustomerLocations'),
        OBJECT_ID('dbo.Invoices'),
        OBJECT_ID('dbo.InvoiceLineItems')
    )
      AND name IN ('IsDeleted', 'DeletedUtc', 'DeletedBy')
)
BEGIN
    THROW 51002, 'Deletion-specific columns must not exist.', 1;
END;

DECLARE @RequiredAuditColumns TABLE
(
    SchemaName sysname NOT NULL,
    TableName sysname NOT NULL,
    ColumnName sysname NOT NULL
);

INSERT INTO @RequiredAuditColumns (SchemaName, TableName, ColumnName)
VALUES
    ('dbo', 'Tenants', 'CreatedBy'),
    ('dbo', 'Tenants', 'ModifiedBy'),
    ('dbo', 'Customers', 'CreatedBy'),
    ('dbo', 'Customers', 'ModifiedBy'),
    ('dbo', 'CustomerLocations', 'CreatedBy'),
    ('dbo', 'CustomerLocations', 'ModifiedBy'),
    ('dbo', 'Invoices', 'CreatedBy'),
    ('dbo', 'Invoices', 'ModifiedBy'),
    ('dbo', 'InvoiceLineItems', 'CreatedBy'),
    ('dbo', 'InvoiceLineItems', 'ModifiedBy'),
    ('dbo', 'InvoiceStatusHistory', 'ChangedBy'),
    ('history', 'InvoicesHistory', 'CreatedBy'),
    ('history', 'InvoicesHistory', 'ModifiedBy'),
    ('history', 'InvoiceLineItemsHistory', 'CreatedBy'),
    ('history', 'InvoiceLineItemsHistory', 'ModifiedBy');

IF EXISTS
(
    SELECT 1
    FROM @RequiredAuditColumns AS required
    LEFT JOIN sys.columns AS column_definition
        ON column_definition.object_id = OBJECT_ID(QUOTENAME(required.SchemaName) + '.' + QUOTENAME(required.TableName))
       AND column_definition.name = required.ColumnName
    LEFT JOIN sys.types AS type_definition
        ON type_definition.user_type_id = column_definition.user_type_id
    WHERE column_definition.column_id IS NULL
       OR type_definition.name <> 'int'
       OR column_definition.is_nullable = 1
       OR column_definition.default_object_id = 0
       OR OBJECT_DEFINITION(column_definition.default_object_id) NOT LIKE '%1%'
)
BEGIN
    THROW 51003, 'Audit user columns must be required integers with a default of 1.', 1;
END;

SELECT 'Schema verification passed.' AS Result;
