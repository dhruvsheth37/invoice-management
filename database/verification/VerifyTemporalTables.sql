SET NOCOUNT ON;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.tables AS current_table
    INNER JOIN sys.tables AS history_table
        ON history_table.object_id = current_table.history_table_id
    INNER JOIN sys.schemas AS history_schema
        ON history_schema.schema_id = history_table.schema_id
    WHERE current_table.object_id = OBJECT_ID('dbo.Invoices')
      AND current_table.temporal_type = 2
      AND history_schema.name = 'history'
      AND history_table.name = 'InvoicesHistory'
)
BEGIN
    THROW 51010, 'Invoices temporal configuration is invalid.', 1;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.tables AS current_table
    INNER JOIN sys.tables AS history_table
        ON history_table.object_id = current_table.history_table_id
    INNER JOIN sys.schemas AS history_schema
        ON history_schema.schema_id = history_table.schema_id
    WHERE current_table.object_id = OBJECT_ID('dbo.InvoiceLineItems')
      AND current_table.temporal_type = 2
      AND history_schema.name = 'history'
      AND history_table.name = 'InvoiceLineItemsHistory'
)
BEGIN
    THROW 51011, 'InvoiceLineItems temporal configuration is invalid.', 1;
END;

SELECT 'Temporal-table verification passed.' AS Result;
