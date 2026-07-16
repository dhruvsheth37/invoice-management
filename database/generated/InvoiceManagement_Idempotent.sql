IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    CREATE TABLE [InvoiceStatuses] (
        [Id] tinyint NOT NULL,
        [Code] varchar(32) NOT NULL,
        [DisplayName] nvarchar(50) NOT NULL,
        [SortOrder] tinyint NOT NULL,
        CONSTRAINT [PK_InvoiceStatuses] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    CREATE TABLE [Tenants] (
        [Id] uniqueidentifier NOT NULL,
        [Slug] nvarchar(100) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedUtc] datetime2(7) NOT NULL,
        [CreatedBy] nvarchar(200) NOT NULL,
        [ModifiedUtc] datetime2(7) NULL,
        [ModifiedBy] nvarchar(200) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Tenants_Name_NotBlank] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
        CONSTRAINT [CK_Tenants_Slug_NotBlank] CHECK (LEN(LTRIM(RTRIM([Slug]))) > 0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    CREATE TABLE [Customers] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [LegalName] nvarchar(200) NOT NULL,
        [TaxNumber] nvarchar(50) NULL,
        [Email] nvarchar(254) NULL,
        [CreatedUtc] datetime2(7) NOT NULL,
        [CreatedBy] nvarchar(200) NOT NULL,
        [ModifiedUtc] datetime2(7) NULL,
        [ModifiedBy] nvarchar(200) NULL,
        [RowVersion] rowversion NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_Customers_TenantId_Id] UNIQUE ([TenantId], [Id]),
        CONSTRAINT [FK_Customers_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    CREATE TABLE [IdempotencyRequests] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [Operation] varchar(100) NOT NULL,
        [IdempotencyKey] nvarchar(100) NOT NULL,
        [RequestHash] binary(32) NOT NULL,
        [State] tinyint NOT NULL,
        [ResourceId] uniqueidentifier NULL,
        [ResponseStatus] smallint NULL,
        [ResponseBody] nvarchar(max) NULL,
        [CorrelationId] varchar(64) NOT NULL,
        [CreatedUtc] datetime2(7) NOT NULL,
        [CompletedUtc] datetime2(7) NULL,
        [ExpiresUtc] datetime2(7) NOT NULL,
        CONSTRAINT [PK_IdempotencyRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_IdempotencyRequests_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    CREATE TABLE [InvoiceNumberSequences] (
        [TenantId] uniqueidentifier NOT NULL,
        [FiscalYear] smallint NOT NULL,
        [CurrentValue] bigint NOT NULL,
        [ModifiedUtc] datetime2(7) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_InvoiceNumberSequences] PRIMARY KEY ([TenantId], [FiscalYear]),
        CONSTRAINT [CK_InvoiceNumberSequences_Value] CHECK ([CurrentValue] >= 0),
        CONSTRAINT [FK_InvoiceNumberSequences_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    CREATE TABLE [CustomerLocations] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [AddressLine1] nvarchar(200) NOT NULL,
        [AddressLine2] nvarchar(200) NULL,
        [City] nvarchar(100) NOT NULL,
        [StateProvince] nvarchar(100) NULL,
        [PostalCode] nvarchar(20) NULL,
        [CountryCode] char(2) NOT NULL,
        [TaxNumber] nvarchar(50) NULL,
        [CreatedUtc] datetime2(7) NOT NULL,
        [CreatedBy] nvarchar(200) NOT NULL,
        [ModifiedUtc] datetime2(7) NULL,
        [ModifiedBy] nvarchar(200) NULL,
        [RowVersion] rowversion NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_CustomerLocations] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_CustomerLocations_TenantId_CustomerId_Id] UNIQUE ([TenantId], [CustomerId], [Id]),
        CONSTRAINT [AK_CustomerLocations_TenantId_Id] UNIQUE ([TenantId], [Id]),
        CONSTRAINT [FK_CustomerLocations_Customers_TenantId_CustomerId] FOREIGN KEY ([TenantId], [CustomerId]) REFERENCES [Customers] ([TenantId], [Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    IF SCHEMA_ID(N'history') IS NULL EXEC(N'CREATE SCHEMA [history];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    CREATE TABLE [Invoices] (
        [Id] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NOT NULL,
        [CustomerLocationId] uniqueidentifier NOT NULL,
        [BillToCustomerCode] nvarchar(50) NULL,
        [BillToLegalName] nvarchar(200) NULL,
        [BillToTaxNumber] nvarchar(50) NULL,
        [BillToAddressLine1] nvarchar(200) NULL,
        [BillToAddressLine2] nvarchar(200) NULL,
        [BillToCity] nvarchar(100) NULL,
        [BillToStateProvince] nvarchar(100) NULL,
        [BillToPostalCode] nvarchar(20) NULL,
        [BillToCountryCode] char(2) NULL,
        [InvoiceNumber] nvarchar(50) NULL,
        [StatusId] tinyint NOT NULL,
        [CurrencyCode] char(3) NOT NULL,
        [IssueDate] date NULL,
        [DueDate] date NULL,
        [PaidDate] date NULL,
        [PaymentReference] nvarchar(100) NULL,
        [Subtotal] decimal(19,4) NOT NULL,
        [TaxTotal] decimal(19,4) NOT NULL,
        [Total] decimal(19,4) NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [VoidReason] nvarchar(500) NULL,
        [ValidFromUtc] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [ValidToUtc] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [CreatedUtc] datetime2(7) NOT NULL,
        [CreatedBy] nvarchar(200) NOT NULL,
        [ModifiedUtc] datetime2(7) NULL,
        [ModifiedBy] nvarchar(200) NULL,
        [RowVersion] rowversion NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_Invoices_TenantId_Id] UNIQUE ([TenantId], [Id]),
        CONSTRAINT [CK_Invoices_Amounts] CHECK ([Subtotal] >= 0 AND [TaxTotal] >= 0 AND [Total] >= 0 AND [Total] = [Subtotal] + [TaxTotal]),
        CONSTRAINT [CK_Invoices_Dates] CHECK ([DueDate] IS NULL OR [IssueDate] IS NULL OR [DueDate] >= [IssueDate]),
        CONSTRAINT [CK_Invoices_Deactivation] CHECK ([IsActive] = 1 OR [StatusId] = 1),
        CONSTRAINT [CK_Invoices_Draft] CHECK ([StatusId] <> 1 OR ([InvoiceNumber] IS NULL AND [IssueDate] IS NULL AND [PaidDate] IS NULL)),
        CONSTRAINT [CK_Invoices_IssuedSnapshot] CHECK ([StatusId] NOT IN (2, 3) OR ([InvoiceNumber] IS NOT NULL AND [IssueDate] IS NOT NULL AND [DueDate] IS NOT NULL AND [BillToCustomerCode] IS NOT NULL AND [BillToLegalName] IS NOT NULL AND [BillToAddressLine1] IS NOT NULL AND [BillToCity] IS NOT NULL AND [BillToCountryCode] IS NOT NULL)),
        CONSTRAINT [CK_Invoices_Paid] CHECK (([StatusId] = 3 AND [PaidDate] IS NOT NULL) OR ([StatusId] <> 3 AND [PaidDate] IS NULL)),
        CONSTRAINT [CK_Invoices_Void] CHECK (([StatusId] = 4 AND [VoidReason] IS NOT NULL) OR ([StatusId] <> 4 AND [VoidReason] IS NULL)),
        CONSTRAINT [FK_Invoices_CustomerLocations_TenantId_CustomerId_CustomerLocationId] FOREIGN KEY ([TenantId], [CustomerId], [CustomerLocationId]) REFERENCES [CustomerLocations] ([TenantId], [CustomerId], [Id]),
        CONSTRAINT [FK_Invoices_Customers_TenantId_CustomerId] FOREIGN KEY ([TenantId], [CustomerId]) REFERENCES [Customers] ([TenantId], [Id]),
        CONSTRAINT [FK_Invoices_InvoiceStatuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [InvoiceStatuses] ([Id]),
        CONSTRAINT [FK_Invoices_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]),
        PERIOD FOR SYSTEM_TIME([ValidFromUtc], [ValidToUtc])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [history].[InvoicesHistory]));
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    CREATE TABLE [InvoiceLineItems] (
        [Id] uniqueidentifier NOT NULL,
        [InvoiceId] uniqueidentifier NOT NULL,
        [LineNumber] smallint NOT NULL,
        [Description] nvarchar(500) NOT NULL,
        [Quantity] decimal(18,4) NOT NULL,
        [UnitPrice] decimal(19,4) NOT NULL,
        [TaxRate] decimal(9,6) NOT NULL,
        [NetAmount] decimal(19,4) NOT NULL,
        [TaxAmount] decimal(19,4) NOT NULL,
        [TotalAmount] decimal(19,4) NOT NULL,
        [ValidFromUtc] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [ValidToUtc] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [CreatedUtc] datetime2(7) NOT NULL,
        [CreatedBy] nvarchar(200) NOT NULL,
        [ModifiedUtc] datetime2(7) NULL,
        [ModifiedBy] nvarchar(200) NULL,
        [RowVersion] rowversion NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_InvoiceLineItems] PRIMARY KEY ([Id]),
        CONSTRAINT [AK_InvoiceLineItems_TenantId_Id] UNIQUE ([TenantId], [Id]),
        CONSTRAINT [CK_InvoiceLineItems_Amounts] CHECK ([NetAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] = [NetAmount] + [TaxAmount]),
        CONSTRAINT [CK_InvoiceLineItems_Values] CHECK ([LineNumber] > 0 AND [Quantity] > 0 AND [UnitPrice] >= 0 AND [TaxRate] >= 0 AND [TaxRate] <= 1),
        CONSTRAINT [FK_InvoiceLineItems_Invoices_TenantId_InvoiceId] FOREIGN KEY ([TenantId], [InvoiceId]) REFERENCES [Invoices] ([TenantId], [Id]),
        PERIOD FOR SYSTEM_TIME([ValidFromUtc], [ValidToUtc])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [history].[InvoiceLineItemsHistory]));
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    CREATE TABLE [InvoiceStatusHistory] (
        [Id] uniqueidentifier NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        [InvoiceId] uniqueidentifier NOT NULL,
        [FromStatusId] tinyint NULL,
        [ToStatusId] tinyint NOT NULL,
        [Reason] nvarchar(500) NULL,
        [ChangedUtc] datetime2(7) NOT NULL,
        [ChangedBy] nvarchar(200) NOT NULL,
        [CorrelationId] varchar(64) NOT NULL,
        CONSTRAINT [PK_InvoiceStatusHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InvoiceStatusHistory_InvoiceStatuses_FromStatusId] FOREIGN KEY ([FromStatusId]) REFERENCES [InvoiceStatuses] ([Id]),
        CONSTRAINT [FK_InvoiceStatusHistory_InvoiceStatuses_ToStatusId] FOREIGN KEY ([ToStatusId]) REFERENCES [InvoiceStatuses] ([Id]),
        CONSTRAINT [FK_InvoiceStatusHistory_Invoices_TenantId_InvoiceId] FOREIGN KEY ([TenantId], [InvoiceId]) REFERENCES [Invoices] ([TenantId], [Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'DisplayName', N'SortOrder') AND [object_id] = OBJECT_ID(N'[InvoiceStatuses]'))
        SET IDENTITY_INSERT [InvoiceStatuses] ON;
    EXEC(N'INSERT INTO [InvoiceStatuses] ([Id], [Code], [DisplayName], [SortOrder])
    VALUES (CAST(1 AS tinyint), ''Draft'', N''Draft'', CAST(1 AS tinyint)),
    (CAST(2 AS tinyint), ''Issued'', N''Issued'', CAST(2 AS tinyint)),
    (CAST(3 AS tinyint), ''Paid'', N''Paid'', CAST(3 AS tinyint)),
    (CAST(4 AS tinyint), ''Void'', N''Void'', CAST(4 AS tinyint))');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'DisplayName', N'SortOrder') AND [object_id] = OBJECT_ID(N'[InvoiceStatuses]'))
        SET IDENTITY_INSERT [InvoiceStatuses] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedBy', N'CreatedUtc', N'IsActive', N'ModifiedBy', N'ModifiedUtc', N'Name', N'Slug') AND [object_id] = OBJECT_ID(N'[Tenants]'))
        SET IDENTITY_INSERT [Tenants] ON;
    EXEC(N'INSERT INTO [Tenants] ([Id], [CreatedBy], [CreatedUtc], [IsActive], [ModifiedBy], [ModifiedUtc], [Name], [Slug])
    VALUES (''11111111-1111-1111-1111-111111111111'', N''seed'', ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), NULL, NULL, N''Demo Tenant'', N''demo'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedBy', N'CreatedUtc', N'IsActive', N'ModifiedBy', N'ModifiedUtc', N'Name', N'Slug') AND [object_id] = OBJECT_ID(N'[Tenants]'))
        SET IDENTITY_INSERT [Tenants] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CustomerLocations_TenantId_CustomerId_Name] ON [CustomerLocations] ([TenantId], [CustomerId], [Name]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Customers_TenantId_Code] ON [Customers] ([TenantId], [Code]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Customers_TenantId_IsActive_LegalName] ON [Customers] ([TenantId], [IsActive], [LegalName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_IdempotencyRequests_ExpiresUtc] ON [IdempotencyRequests] ([ExpiresUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_IdempotencyRequests_TenantId_Operation_IdempotencyKey] ON [IdempotencyRequests] ([TenantId], [Operation], [IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_InvoiceLineItems_TenantId_InvoiceId_LineNumber] ON [InvoiceLineItems] ([TenantId], [InvoiceId], [LineNumber]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Invoices_StatusId] ON [Invoices] ([StatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Invoices_TenantId_CustomerId_CustomerLocationId] ON [Invoices] ([TenantId], [CustomerId], [CustomerLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Invoices_TenantId_InvoiceNumber] ON [Invoices] ([TenantId], [InvoiceNumber]) WHERE [InvoiceNumber] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Invoices_TenantId_IsActive_CustomerId_CreatedUtc] ON [Invoices] ([TenantId], [IsActive], [CustomerId], [CreatedUtc] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Invoices_TenantId_IsActive_StatusId_CreatedUtc_Id] ON [Invoices] ([TenantId], [IsActive], [StatusId], [CreatedUtc] DESC, [Id] DESC) INCLUDE ([InvoiceNumber], [CustomerId], [Total], [CurrencyCode], [DueDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Invoices_TenantId_IsActive_StatusId_DueDate] ON [Invoices] ([TenantId], [IsActive], [StatusId], [DueDate]) INCLUDE ([CurrencyCode], [Total]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_InvoiceStatuses_Code] ON [InvoiceStatuses] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_InvoiceStatusHistory_FromStatusId] ON [InvoiceStatusHistory] ([FromStatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_InvoiceStatusHistory_TenantId_InvoiceId_ChangedUtc] ON [InvoiceStatusHistory] ([TenantId], [InvoiceId], [ChangedUtc] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_InvoiceStatusHistory_ToStatusId] ON [InvoiceStatusHistory] ([ToStatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tenants_Slug] ON [Tenants] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122805_InitialSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260716122805_InitialSchema', N'10.0.9');
END;

COMMIT;
GO
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716134713_UseIntegerAuditUserIds'
)
BEGIN
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

    UPDATE [history].[InvoicesHistory]
    SET [CreatedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [CreatedBy])), N'1'),
        [ModifiedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [ModifiedBy])), N'1');

    ALTER TABLE [Invoices]
        SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [history].[InvoicesHistory]));

    ALTER TABLE [InvoiceLineItems] SET (SYSTEM_VERSIONING = OFF);

    UPDATE [InvoiceLineItems]
    SET [CreatedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [CreatedBy])), N'1'),
        [ModifiedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [ModifiedBy])), N'1');

    UPDATE [history].[InvoiceLineItemsHistory]
    SET [CreatedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [CreatedBy])), N'1'),
        [ModifiedBy] = COALESCE(CONVERT(nvarchar(200), TRY_CONVERT(int, [ModifiedBy])), N'1');

    ALTER TABLE [InvoiceLineItems]
        SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [history].[InvoiceLineItemsHistory]));
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716134713_UseIntegerAuditUserIds'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tenants]') AND [c].[name] = N'ModifiedBy');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Tenants] DROP CONSTRAINT ' + @var + ';');
    EXEC(N'UPDATE [Tenants] SET [ModifiedBy] = 1 WHERE [ModifiedBy] IS NULL');
    ALTER TABLE [Tenants] ALTER COLUMN [ModifiedBy] int NOT NULL;
    ALTER TABLE [Tenants] ADD DEFAULT 1 FOR [ModifiedBy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716134713_UseIntegerAuditUserIds'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tenants]') AND [c].[name] = N'CreatedBy');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Tenants] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Tenants] ALTER COLUMN [CreatedBy] int NOT NULL;
    ALTER TABLE [Tenants] ADD DEFAULT 1 FOR [CreatedBy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716134713_UseIntegerAuditUserIds'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceStatusHistory]') AND [c].[name] = N'ChangedBy');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceStatusHistory] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [InvoiceStatusHistory] ALTER COLUMN [ChangedBy] int NOT NULL;
    ALTER TABLE [InvoiceStatusHistory] ADD DEFAULT 1 FOR [ChangedBy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716134713_UseIntegerAuditUserIds'
)
BEGIN
    ALTER TABLE [Invoices] SET (SYSTEM_VERSIONING = OFF)

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716134713_UseIntegerAuditUserIds'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoices]') AND [c].[name] = N'ModifiedBy');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Invoices] DROP CONSTRAINT ' + @var3 + ';');
    EXEC(N'UPDATE [Invoices] SET [ModifiedBy] = 1 WHERE [ModifiedBy] IS NULL');
    ALTER TABLE [Invoices] ALTER COLUMN [ModifiedBy] int NOT NULL;
    ALTER TABLE [Invoices] ADD DEFAULT 1 FOR [ModifiedBy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716134713_UseIntegerAuditUserIds'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[history].[InvoicesHistory]') AND [c].[name] = N'ModifiedBy');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [history].[InvoicesHistory] DROP CONSTRAINT ' + @var4 + ';');
    EXEC(N'UPDATE [history].[InvoicesHistory] SET [ModifiedBy] = 1 WHERE [ModifiedBy] IS NULL');
    ALTER TABLE [history].[InvoicesHistory] ALTER COLUMN [ModifiedBy] int NOT NULL;
    ALTER TABLE [history].[InvoicesHistory] ADD DEFAULT 1 FOR [ModifiedBy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716134713_UseIntegerAuditUserIds'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Invoices]') AND [c].[name] = N'CreatedBy');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Invoices] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [Invoices] ALTER COLUMN [CreatedBy] int NOT NULL;
    ALTER TABLE [Invoices] ADD DEFAULT 1 FOR [CreatedBy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716134713_UseIntegerAuditUserIds'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[history].[InvoicesHistory]') AND [c].[name] = N'CreatedBy');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [history].[InvoicesHistory] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [history].[InvoicesHistory] ALTER COLUMN [CreatedBy] int NOT NULL;
    ALTER TABLE [history].[InvoicesHistory] ADD DEFAULT 1 FOR [CreatedBy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716134713_UseIntegerAuditUserIds'
)
BEGIN
    ALTER TABLE [InvoiceLineItems] SET (SYSTEM_VERSIONING = OFF)

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716134713_UseIntegerAuditUserIds'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLineItems]') AND [c].[name] = N'ModifiedBy');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLineItems] DROP CONSTRAINT ' + @var7 + ';');
    EXEC(N'UPDATE [InvoiceLineItems] SET [ModifiedBy] = 1 WHERE [ModifiedBy] IS NULL');
    ALTER TABLE [InvoiceLineItems] ALTER COLUMN [ModifiedBy] int NOT NULL;
    ALTER TABLE [InvoiceLineItems] ADD DEFAULT 1 FOR [ModifiedBy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716134713_UseIntegerAuditUserIds'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[history].[InvoiceLineItemsHistory]') AND [c].[name] = N'ModifiedBy');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [history].[InvoiceLineItemsHistory] DROP CONSTRAINT ' + @var8 + ';');
    EXEC(N'UPDATE [history].[InvoiceLineItemsHistory] SET [ModifiedBy] = 1 WHERE [ModifiedBy] IS NULL');
    ALTER TABLE [history].[InvoiceLineItemsHistory] ALTER COLUMN [ModifiedBy] int NOT NULL;
    ALTER TABLE [history].[InvoiceLineItemsHistory] ADD DEFAULT 1 FOR [ModifiedBy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716134713_UseIntegerAuditUserIds'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[InvoiceLineItems]') AND [c].[name] = N'CreatedBy');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [InvoiceLineItems] DROP CONSTRAINT ' + @var9 + ';');
    ALTER TABLE [InvoiceLineItems] ALTER COLUMN [CreatedBy] int NOT NULL;
    ALTER TABLE [InvoiceLineItems] ADD DEFAULT 1 FOR [CreatedBy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716134713_UseIntegerAuditUserIds'
)
BEGIN
    DECLARE @var10 nvarchar(max);
    SELECT @var10 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[history].[InvoiceLineItemsHistory]') AND [c].[name] = N'CreatedBy');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [history].[InvoiceLineItemsHistory] DROP CONSTRAINT ' + @var10 + ';');
    ALTER TABLE [history].[InvoiceLineItemsHistory] ALTER COLUMN [CreatedBy] int NOT NULL;
    ALTER TABLE [history].[InvoiceLineItemsHistory] ADD DEFAULT 1 FOR [CreatedBy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716134713_UseIntegerAuditUserIds'
)
BEGIN
    DECLARE @var11 nvarchar(max);
    SELECT @var11 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Customers]') AND [c].[name] = N'ModifiedBy');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Customers] DROP CONSTRAINT ' + @var11 + ';');
    EXEC(N'UPDATE [Customers] SET [ModifiedBy] = 1 WHERE [ModifiedBy] IS NULL');
    ALTER TABLE [Customers] ALTER COLUMN [ModifiedBy] int NOT NULL;
    ALTER TABLE [Customers] ADD DEFAULT 1 FOR [ModifiedBy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716134713_UseIntegerAuditUserIds'
)
BEGIN
    DECLARE @var12 nvarchar(max);
    SELECT @var12 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Customers]') AND [c].[name] = N'CreatedBy');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Customers] DROP CONSTRAINT ' + @var12 + ';');
    ALTER TABLE [Customers] ALTER COLUMN [CreatedBy] int NOT NULL;
    ALTER TABLE [Customers] ADD DEFAULT 1 FOR [CreatedBy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716134713_UseIntegerAuditUserIds'
)
BEGIN
    DECLARE @var13 nvarchar(max);
    SELECT @var13 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CustomerLocations]') AND [c].[name] = N'ModifiedBy');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [CustomerLocations] DROP CONSTRAINT ' + @var13 + ';');
    EXEC(N'UPDATE [CustomerLocations] SET [ModifiedBy] = 1 WHERE [ModifiedBy] IS NULL');
    ALTER TABLE [CustomerLocations] ALTER COLUMN [ModifiedBy] int NOT NULL;
    ALTER TABLE [CustomerLocations] ADD DEFAULT 1 FOR [ModifiedBy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716134713_UseIntegerAuditUserIds'
)
BEGIN
    DECLARE @var14 nvarchar(max);
    SELECT @var14 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CustomerLocations]') AND [c].[name] = N'CreatedBy');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [CustomerLocations] DROP CONSTRAINT ' + @var14 + ';');
    ALTER TABLE [CustomerLocations] ALTER COLUMN [CreatedBy] int NOT NULL;
    ALTER TABLE [CustomerLocations] ADD DEFAULT 1 FOR [CreatedBy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716134713_UseIntegerAuditUserIds'
)
BEGIN
    EXEC(N'UPDATE [Tenants] SET [CreatedBy] = 1, [ModifiedBy] = 1
    WHERE [Id] = ''11111111-1111-1111-1111-111111111111'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716134713_UseIntegerAuditUserIds'
)
BEGIN
    ALTER TABLE [Invoices] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [history].[InvoicesHistory]))

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716134713_UseIntegerAuditUserIds'
)
BEGIN
    ALTER TABLE [InvoiceLineItems] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [history].[InvoiceLineItemsHistory]))

END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716134713_UseIntegerAuditUserIds'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260716134713_UseIntegerAuditUserIds', N'10.0.9');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716173156_AddInvoiceQueryIndexes'
)
BEGIN
    CREATE INDEX [IX_Invoices_TenantId_IsActive_CreatedUtc_Id] ON [Invoices] ([TenantId], [IsActive], [CreatedUtc] DESC, [Id] DESC) INCLUDE ([InvoiceNumber], [StatusId], [CustomerId], [CurrencyCode], [Total], [IssueDate], [DueDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716173156_AddInvoiceQueryIndexes'
)
BEGIN
    CREATE INDEX [IX_Invoices_TenantId_IsActive_CurrencyCode_StatusId_DueDate] ON [Invoices] ([TenantId], [IsActive], [CurrencyCode], [StatusId], [DueDate]) INCLUDE ([Total]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716173156_AddInvoiceQueryIndexes'
)
BEGIN
    CREATE INDEX [IX_Invoices_TenantId_IsActive_DueDate_Id] ON [Invoices] ([TenantId], [IsActive], [DueDate], [Id]) INCLUDE ([InvoiceNumber], [StatusId], [CustomerId], [CurrencyCode], [Total], [IssueDate], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716173156_AddInvoiceQueryIndexes'
)
BEGIN
    CREATE INDEX [IX_Invoices_TenantId_IsActive_Total_Id] ON [Invoices] ([TenantId], [IsActive], [Total], [Id]) INCLUDE ([InvoiceNumber], [StatusId], [CustomerId], [CurrencyCode], [IssueDate], [DueDate], [CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716173156_AddInvoiceQueryIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260716173156_AddInvoiceQueryIndexes', N'10.0.9');
END;

COMMIT;
GO
