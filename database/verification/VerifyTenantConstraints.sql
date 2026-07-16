SET NOCOUNT ON;

DECLARE @RequiredForeignKeys TABLE (ForeignKeyName sysname NOT NULL);
INSERT INTO @RequiredForeignKeys (ForeignKeyName)
VALUES
    ('FK_CustomerLocations_Customers_TenantId_CustomerId'),
    ('FK_Invoices_Customers_TenantId_CustomerId'),
    ('FK_Invoices_CustomerLocations_TenantId_CustomerId_CustomerLocationId'),
    ('FK_InvoiceLineItems_Invoices_TenantId_InvoiceId');

IF EXISTS
(
    SELECT 1
    FROM @RequiredForeignKeys AS required
    LEFT JOIN sys.foreign_keys AS actual
        ON actual.name = required.ForeignKeyName
    WHERE actual.object_id IS NULL
)
BEGIN
    THROW 51020, 'One or more tenant-aware foreign keys are missing.', 1;
END;

SELECT 'Tenant-constraint verification passed.' AS Result;
