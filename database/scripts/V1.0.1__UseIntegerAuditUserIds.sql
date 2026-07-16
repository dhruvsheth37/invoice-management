BEGIN TRANSACTION;
UPDATE [Tenants]
SET [CreatedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [CreatedBy])), N'1'),
    [ModifiedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [ModifiedBy])), N'1');

UPDATE [Customers]
SET [CreatedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [CreatedBy])), N'1'),
    [ModifiedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [ModifiedBy])), N'1');

UPDATE [CustomerLocations]
SET [CreatedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [CreatedBy])), N'1'),
    [ModifiedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [ModifiedBy])), N'1');

UPDATE [InvoiceStatusHistory]
SET [ChangedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [ChangedBy])), N'1');

ALTER TABLE [Invoices] SET (SYSTEM_VERSIONING = OFF);

UPDATE [Invoices]
SET [CreatedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [CreatedBy])), N'1'),
    [ModifiedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [ModifiedBy])), N'1');

EXEC(N'UPDATE [history].[InvoicesHistory]
SET [CreatedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [CreatedBy])), N''1''),
    [ModifiedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [ModifiedBy])), N''1'');');

ALTER TABLE [Invoices]
    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [history].[InvoicesHistory]));

ALTER TABLE [InvoiceLineItems] SET (SYSTEM_VERSIONING = OFF);

UPDATE [InvoiceLineItems]
SET [CreatedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [CreatedBy])), N'1'),
    [ModifiedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [ModifiedBy])), N'1');

EXEC(N'UPDATE [history].[InvoiceLineItemsHistory]
SET [CreatedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [CreatedBy])), N''1''),
    [ModifiedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [ModifiedBy])), N''1'');');

ALTER TABLE [InvoiceLineItems]
    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [history].[InvoiceLineItemsHistory]));

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tenants]') AND [c].[name] = N'ModifiedBy');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Tenants] DROP CONSTRAINT ' + @var + ';');
UPDATE [Tenants] SET [ModifiedBy] = 1 WHERE [ModifiedBy] IS NULL;
ALTER TABLE [Tenants] ALTER COLUMN [ModifiedBy] int NOT NULL;
ALTER TABLE [Tenants] ADD DEFAULT 1 FOR [ModifiedBy];

DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tenants]') AND [c].[name] = N'CreatedBy');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Tenants] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [Tenants] ALTER COLUMN [CreatedBy] int NOT NULL;
ALTER TABLE [Tenants] ADD DEFAULT 1 FOR [CreatedBy];

DECLARE @var2 nvarchar(max);
SELECT @var2 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceStatusHistory]') AND [c].[name] = N'ChangedBy');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceStatusHistory] DROP CONSTRAINT ' + @var2 + ';');
ALTER TABLE [InvoiceStatusHistory] ALTER COLUMN [ChangedBy] int NOT NULL;
ALTER TABLE [InvoiceStatusHistory] ADD DEFAULT 1 FOR [ChangedBy];

ALTER TABLE [Invoices] SET (SYSTEM_VERSIONING = OFF)


DECLARE @var3 nvarchar(max);
SELECT @var3 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoices]') AND [c].[name] = N'ModifiedBy');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Invoices] DROP CONSTRAINT ' + @var3 + ';');
UPDATE [Invoices] SET [ModifiedBy] = 1 WHERE [ModifiedBy] IS NULL;
ALTER TABLE [Invoices] ALTER COLUMN [ModifiedBy] int NOT NULL;
ALTER TABLE [Invoices] ADD DEFAULT 1 FOR [ModifiedBy];

DECLARE @var4 nvarchar(max);
SELECT @var4 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[history].[InvoicesHistory]') AND [c].[name] = N'ModifiedBy');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [history].[InvoicesHistory] DROP CONSTRAINT ' + @var4 + ';');
EXEC(N'UPDATE [history].[InvoicesHistory] SET [ModifiedBy] = 1 WHERE [ModifiedBy] IS NULL');
ALTER TABLE [history].[InvoicesHistory] ALTER COLUMN [ModifiedBy] int NOT NULL;
ALTER TABLE [history].[InvoicesHistory] ADD DEFAULT 1 FOR [ModifiedBy];

DECLARE @var5 nvarchar(max);
SELECT @var5 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoices]') AND [c].[name] = N'CreatedBy');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Invoices] DROP CONSTRAINT ' + @var5 + ';');
ALTER TABLE [Invoices] ALTER COLUMN [CreatedBy] int NOT NULL;
ALTER TABLE [Invoices] ADD DEFAULT 1 FOR [CreatedBy];

DECLARE @var6 nvarchar(max);
SELECT @var6 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[history].[InvoicesHistory]') AND [c].[name] = N'CreatedBy');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [history].[InvoicesHistory] DROP CONSTRAINT ' + @var6 + ';');
ALTER TABLE [history].[InvoicesHistory] ALTER COLUMN [CreatedBy] int NOT NULL;
ALTER TABLE [history].[InvoicesHistory] ADD DEFAULT 1 FOR [CreatedBy];

ALTER TABLE [InvoiceLineItems] SET (SYSTEM_VERSIONING = OFF)


DECLARE @var7 nvarchar(max);
SELECT @var7 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLineItems]') AND [c].[name] = N'ModifiedBy');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLineItems] DROP CONSTRAINT ' + @var7 + ';');
UPDATE [InvoiceLineItems] SET [ModifiedBy] = 1 WHERE [ModifiedBy] IS NULL;
ALTER TABLE [InvoiceLineItems] ALTER COLUMN [ModifiedBy] int NOT NULL;
ALTER TABLE [InvoiceLineItems] ADD DEFAULT 1 FOR [ModifiedBy];

DECLARE @var8 nvarchar(max);
SELECT @var8 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[history].[InvoiceLineItemsHistory]') AND [c].[name] = N'ModifiedBy');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [history].[InvoiceLineItemsHistory] DROP CONSTRAINT ' + @var8 + ';');
EXEC(N'UPDATE [history].[InvoiceLineItemsHistory] SET [ModifiedBy] = 1 WHERE [ModifiedBy] IS NULL');
ALTER TABLE [history].[InvoiceLineItemsHistory] ALTER COLUMN [ModifiedBy] int NOT NULL;
ALTER TABLE [history].[InvoiceLineItemsHistory] ADD DEFAULT 1 FOR [ModifiedBy];

DECLARE @var9 nvarchar(max);
SELECT @var9 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLineItems]') AND [c].[name] = N'CreatedBy');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLineItems] DROP CONSTRAINT ' + @var9 + ';');
ALTER TABLE [InvoiceLineItems] ALTER COLUMN [CreatedBy] int NOT NULL;
ALTER TABLE [InvoiceLineItems] ADD DEFAULT 1 FOR [CreatedBy];

DECLARE @var10 nvarchar(max);
SELECT @var10 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[history].[InvoiceLineItemsHistory]') AND [c].[name] = N'CreatedBy');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [history].[InvoiceLineItemsHistory] DROP CONSTRAINT ' + @var10 + ';');
ALTER TABLE [history].[InvoiceLineItemsHistory] ALTER COLUMN [CreatedBy] int NOT NULL;
ALTER TABLE [history].[InvoiceLineItemsHistory] ADD DEFAULT 1 FOR [CreatedBy];

DECLARE @var11 nvarchar(max);
SELECT @var11 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Customers]') AND [c].[name] = N'ModifiedBy');
IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Customers] DROP CONSTRAINT ' + @var11 + ';');
UPDATE [Customers] SET [ModifiedBy] = 1 WHERE [ModifiedBy] IS NULL;
ALTER TABLE [Customers] ALTER COLUMN [ModifiedBy] int NOT NULL;
ALTER TABLE [Customers] ADD DEFAULT 1 FOR [ModifiedBy];

DECLARE @var12 nvarchar(max);
SELECT @var12 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Customers]') AND [c].[name] = N'CreatedBy');
IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Customers] DROP CONSTRAINT ' + @var12 + ';');
ALTER TABLE [Customers] ALTER COLUMN [CreatedBy] int NOT NULL;
ALTER TABLE [Customers] ADD DEFAULT 1 FOR [CreatedBy];

DECLARE @var13 nvarchar(max);
SELECT @var13 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CustomerLocations]') AND [c].[name] = N'ModifiedBy');
IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [CustomerLocations] DROP CONSTRAINT ' + @var13 + ';');
UPDATE [CustomerLocations] SET [ModifiedBy] = 1 WHERE [ModifiedBy] IS NULL;
ALTER TABLE [CustomerLocations] ALTER COLUMN [ModifiedBy] int NOT NULL;
ALTER TABLE [CustomerLocations] ADD DEFAULT 1 FOR [ModifiedBy];

DECLARE @var14 nvarchar(max);
SELECT @var14 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CustomerLocations]') AND [c].[name] = N'CreatedBy');
IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [CustomerLocations] DROP CONSTRAINT ' + @var14 + ';');
ALTER TABLE [CustomerLocations] ALTER COLUMN [CreatedBy] int NOT NULL;
ALTER TABLE [CustomerLocations] ADD DEFAULT 1 FOR [CreatedBy];

UPDATE [Tenants] SET [CreatedBy] = 1, [ModifiedBy] = 1
WHERE [Id] = '11111111-1111-1111-1111-111111111111';
SELECT @@ROWCOUNT;


ALTER TABLE [Invoices] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [history].[InvoicesHistory]))


ALTER TABLE [InvoiceLineItems] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [history].[InvoiceLineItemsHistory]))


INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260716134713_UseIntegerAuditUserIds', N'10.0.9');

COMMIT;
GO
