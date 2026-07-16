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

SELECT 'Schema verification passed.' AS Result;
