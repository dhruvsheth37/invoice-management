BEGIN TRANSACTION;
CREATE INDEX [IX_Invoices_TenantId_IsActive_CreatedUtc_Id] ON [Invoices] ([TenantId], [IsActive], [CreatedUtc] DESC, [Id] DESC) INCLUDE ([InvoiceNumber], [StatusId], [CustomerId], [CurrencyCode], [Total], [IssueDate], [DueDate]);

CREATE INDEX [IX_Invoices_TenantId_IsActive_CurrencyCode_StatusId_DueDate] ON [Invoices] ([TenantId], [IsActive], [CurrencyCode], [StatusId], [DueDate]) INCLUDE ([Total]);

CREATE INDEX [IX_Invoices_TenantId_IsActive_DueDate_Id] ON [Invoices] ([TenantId], [IsActive], [DueDate], [Id]) INCLUDE ([InvoiceNumber], [StatusId], [CustomerId], [CurrencyCode], [Total], [IssueDate], [CreatedUtc]);

CREATE INDEX [IX_Invoices_TenantId_IsActive_Total_Id] ON [Invoices] ([TenantId], [IsActive], [Total], [Id]) INCLUDE ([InvoiceNumber], [StatusId], [CustomerId], [CurrencyCode], [IssueDate], [DueDate], [CreatedUtc]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260716173156_AddInvoiceQueryIndexes', N'10.0.9');

COMMIT;
GO
