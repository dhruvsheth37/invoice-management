SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'[dbo].[Invoices]', N'U') IS NULL
    THROW 50000, 'Run the EF Core database migrations before seeding demo data.', 1;

DECLARE @TenantId uniqueidentifier = '11111111-1111-1111-1111-111111111111';
DECLARE @CustomerId uniqueidentifier = '00000000-0000-0000-0000-000000000001';
DECLARE @LocationId uniqueidentifier = '00000000-0000-0000-0000-000000000002';
DECLARE @DraftInvoiceId uniqueidentifier = '10000000-0000-0000-0000-000000000001';
DECLARE @IssuedInvoiceId uniqueidentifier = '10000000-0000-0000-0000-000000000002';
DECLARE @PaidInvoiceId uniqueidentifier = '10000000-0000-0000-0000-000000000003';
DECLARE @VoidInvoiceId uniqueidentifier = '10000000-0000-0000-0000-000000000004';
DECLARE @SeedUtc datetime2(7) = '2026-01-15T09:00:00Z';

BEGIN TRY
    BEGIN TRANSACTION;

    -- Reference rows are normally inserted by EF migrations. These checks make
    -- the demo script independently idempotent and document their stored values.
    INSERT INTO [InvoiceStatuses] ([Id], [Code], [DisplayName], [SortOrder])
    SELECT source.[Id], source.[Code], source.[DisplayName], source.[SortOrder]
    FROM (VALUES
        (CAST(1 AS tinyint), 'Draft', N'Draft', CAST(1 AS tinyint)),
        (CAST(2 AS tinyint), 'Issued', N'Issued', CAST(2 AS tinyint)),
        (CAST(3 AS tinyint), 'Paid', N'Paid', CAST(3 AS tinyint)),
        (CAST(4 AS tinyint), 'Void', N'Void', CAST(4 AS tinyint))
    ) source ([Id], [Code], [DisplayName], [SortOrder])
    WHERE NOT EXISTS (SELECT 1 FROM [InvoiceStatuses] existing WHERE existing.[Id] = source.[Id]);

    IF NOT EXISTS (SELECT 1 FROM [Tenants] WHERE [Id] = @TenantId)
    BEGIN
        INSERT INTO [Tenants]
            ([Id], [Slug], [Name], [IsActive], [CreatedUtc], [CreatedBy], [ModifiedUtc], [ModifiedBy])
        VALUES
            (@TenantId, N'demo', N'Demo Tenant', 1, @SeedUtc, 1, @SeedUtc, 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM [Customers] WHERE [Id] = @CustomerId)
    BEGIN
        INSERT INTO [Customers]
            ([Id], [TenantId], [Code], [LegalName], [TaxNumber], [Email],
             [CreatedUtc], [CreatedBy], [ModifiedUtc], [ModifiedBy], [IsActive])
        VALUES
            (@CustomerId, @TenantId, N'ACME', N'Acme Logistics Ltd', N'TAX-ACME-01',
             N'accounts@acme.test', @SeedUtc, 1, @SeedUtc, 1, 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM [CustomerLocations] WHERE [Id] = @LocationId)
    BEGIN
        INSERT INTO [CustomerLocations]
            ([Id], [TenantId], [CustomerId], [Name], [AddressLine1], [AddressLine2],
             [City], [StateProvince], [PostalCode], [CountryCode], [TaxNumber],
             [CreatedUtc], [CreatedBy], [ModifiedUtc], [ModifiedBy], [IsActive])
        VALUES
            (@LocationId, @TenantId, @CustomerId, N'Head Office', N'1 Main Street', NULL,
             N'Mumbai', N'Maharashtra', N'400001', 'IN', N'TAX-ACME-01',
             @SeedUtc, 1, @SeedUtc, 1, 1);
    END;

    IF NOT EXISTS (
        SELECT 1 FROM [InvoiceNumberSequences]
        WHERE [TenantId] = @TenantId AND [FiscalYear] = 2026)
    BEGIN
        INSERT INTO [InvoiceNumberSequences] ([TenantId], [FiscalYear], [CurrentValue], [ModifiedUtc])
        VALUES (@TenantId, 2026, 3, @SeedUtc);
    END
    ELSE
    BEGIN
        UPDATE [InvoiceNumberSequences]
        SET [CurrentValue] = 3, [ModifiedUtc] = @SeedUtc
        WHERE [TenantId] = @TenantId AND [FiscalYear] = 2026 AND [CurrentValue] < 3;
    END;

    -- Draft invoice.
    IF NOT EXISTS (SELECT 1 FROM [Invoices] WHERE [Id] = @DraftInvoiceId)
    BEGIN
        INSERT INTO [Invoices]
            ([Id], [TenantId], [CustomerId], [CustomerLocationId], [InvoiceNumber], [StatusId],
             [CurrencyCode], [IssueDate], [DueDate], [PaidDate], [PaymentReference],
             [Subtotal], [TaxTotal], [Total], [Notes], [VoidReason],
             [CreatedUtc], [CreatedBy], [ModifiedUtc], [ModifiedBy], [IsActive])
        VALUES
            (@DraftInvoiceId, @TenantId, @CustomerId, @LocationId, NULL, 1,
             'USD', NULL, '2026-08-15', NULL, NULL,
             100.0000, 18.0000, 118.0000, N'Draft sample invoice', NULL,
             '2026-01-15T09:10:00Z', 1, '2026-01-15T09:10:00Z', 1, 1);
    END;

    -- Issued invoice with an overdue due date for dashboard testing.
    IF NOT EXISTS (SELECT 1 FROM [Invoices] WHERE [Id] = @IssuedInvoiceId)
    BEGIN
        INSERT INTO [Invoices]
            ([Id], [TenantId], [CustomerId], [CustomerLocationId],
             [BillToCustomerCode], [BillToLegalName], [BillToTaxNumber],
             [BillToAddressLine1], [BillToAddressLine2], [BillToCity], [BillToStateProvince],
             [BillToPostalCode], [BillToCountryCode], [InvoiceNumber], [StatusId],
             [CurrencyCode], [IssueDate], [DueDate], [PaidDate], [PaymentReference],
             [Subtotal], [TaxTotal], [Total], [Notes], [VoidReason],
             [CreatedUtc], [CreatedBy], [ModifiedUtc], [ModifiedBy], [IsActive])
        VALUES
            (@IssuedInvoiceId, @TenantId, @CustomerId, @LocationId,
             N'ACME', N'Acme Logistics Ltd', N'TAX-ACME-01',
             N'1 Main Street', NULL, N'Mumbai', N'Maharashtra', N'400001', 'IN',
             N'INV-2026-000001', 2, 'USD', '2026-01-16', '2026-02-15', NULL, NULL,
             200.0000, 36.0000, 236.0000, N'Issued sample before temporal update', NULL,
             '2026-01-16T09:00:00Z', 1, '2026-01-16T09:05:00Z', 1, 1);
    END;

    -- Paid invoice.
    IF NOT EXISTS (SELECT 1 FROM [Invoices] WHERE [Id] = @PaidInvoiceId)
    BEGIN
        INSERT INTO [Invoices]
            ([Id], [TenantId], [CustomerId], [CustomerLocationId],
             [BillToCustomerCode], [BillToLegalName], [BillToTaxNumber],
             [BillToAddressLine1], [BillToAddressLine2], [BillToCity], [BillToStateProvince],
             [BillToPostalCode], [BillToCountryCode], [InvoiceNumber], [StatusId],
             [CurrencyCode], [IssueDate], [DueDate], [PaidDate], [PaymentReference],
             [Subtotal], [TaxTotal], [Total], [Notes], [VoidReason],
             [CreatedUtc], [CreatedBy], [ModifiedUtc], [ModifiedBy], [IsActive])
        VALUES
            (@PaidInvoiceId, @TenantId, @CustomerId, @LocationId,
             N'ACME', N'Acme Logistics Ltd', N'TAX-ACME-01',
             N'1 Main Street', NULL, N'Mumbai', N'Maharashtra', N'400001', 'IN',
             N'INV-2026-000002', 3, 'USD', '2026-01-10', '2026-02-10', '2026-01-20', N'PAY-DEMO-001',
             50.0000, 9.0000, 59.0000, N'Paid sample invoice', NULL,
             '2026-01-10T10:00:00Z', 1, '2026-01-20T11:00:00Z', 1, 1);
    END;

    -- Invoice that was issued and then voided.
    IF NOT EXISTS (SELECT 1 FROM [Invoices] WHERE [Id] = @VoidInvoiceId)
    BEGIN
        INSERT INTO [Invoices]
            ([Id], [TenantId], [CustomerId], [CustomerLocationId],
             [BillToCustomerCode], [BillToLegalName], [BillToTaxNumber],
             [BillToAddressLine1], [BillToAddressLine2], [BillToCity], [BillToStateProvince],
             [BillToPostalCode], [BillToCountryCode], [InvoiceNumber], [StatusId],
             [CurrencyCode], [IssueDate], [DueDate], [PaidDate], [PaymentReference],
             [Subtotal], [TaxTotal], [Total], [Notes], [VoidReason],
             [CreatedUtc], [CreatedBy], [ModifiedUtc], [ModifiedBy], [IsActive])
        VALUES
            (@VoidInvoiceId, @TenantId, @CustomerId, @LocationId,
             N'ACME', N'Acme Logistics Ltd', N'TAX-ACME-01',
             N'1 Main Street', NULL, N'Mumbai', N'Maharashtra', N'400001', 'IN',
             N'INV-2026-000003', 4, 'USD', '2026-01-12', '2026-02-12', NULL, NULL,
             150.0000, 27.0000, 177.0000, N'Void sample invoice', N'Customer cancellation',
             '2026-01-12T10:00:00Z', 1, '2026-01-13T11:00:00Z', 1, 1);
    END;

    -- One line item per invoice keeps each invoice total easy to reconcile.
    IF NOT EXISTS (SELECT 1 FROM [InvoiceLineItems] WHERE [Id] = '20000000-0000-0000-0000-000000000001')
        INSERT INTO [InvoiceLineItems]
            ([Id], [TenantId], [InvoiceId], [LineNumber], [Description], [Quantity], [UnitPrice],
             [TaxRate], [NetAmount], [TaxAmount], [TotalAmount],
             [CreatedUtc], [CreatedBy], [ModifiedUtc], [ModifiedBy], [IsActive])
        VALUES
            ('20000000-0000-0000-0000-000000000001', @TenantId, @DraftInvoiceId, 1,
             N'Freight handling', 1.0000, 100.0000, 0.180000, 100.0000, 18.0000, 118.0000,
             '2026-01-15T09:10:00Z', 1, '2026-01-15T09:10:00Z', 1, 1);

    IF NOT EXISTS (SELECT 1 FROM [InvoiceLineItems] WHERE [Id] = '20000000-0000-0000-0000-000000000002')
        INSERT INTO [InvoiceLineItems]
            ([Id], [TenantId], [InvoiceId], [LineNumber], [Description], [Quantity], [UnitPrice],
             [TaxRate], [NetAmount], [TaxAmount], [TotalAmount],
             [CreatedUtc], [CreatedBy], [ModifiedUtc], [ModifiedBy], [IsActive])
        VALUES
            ('20000000-0000-0000-0000-000000000002', @TenantId, @IssuedInvoiceId, 1,
             N'Warehouse service', 2.0000, 100.0000, 0.180000, 200.0000, 36.0000, 236.0000,
             '2026-01-16T09:00:00Z', 1, '2026-01-16T09:05:00Z', 1, 1);

    IF NOT EXISTS (SELECT 1 FROM [InvoiceLineItems] WHERE [Id] = '20000000-0000-0000-0000-000000000003')
        INSERT INTO [InvoiceLineItems]
            ([Id], [TenantId], [InvoiceId], [LineNumber], [Description], [Quantity], [UnitPrice],
             [TaxRate], [NetAmount], [TaxAmount], [TotalAmount],
             [CreatedUtc], [CreatedBy], [ModifiedUtc], [ModifiedBy], [IsActive])
        VALUES
            ('20000000-0000-0000-0000-000000000003', @TenantId, @PaidInvoiceId, 1,
             N'Consulting service', 1.0000, 50.0000, 0.180000, 50.0000, 9.0000, 59.0000,
             '2026-01-10T10:00:00Z', 1, '2026-01-20T11:00:00Z', 1, 1);

    IF NOT EXISTS (SELECT 1 FROM [InvoiceLineItems] WHERE [Id] = '20000000-0000-0000-0000-000000000004')
        INSERT INTO [InvoiceLineItems]
            ([Id], [TenantId], [InvoiceId], [LineNumber], [Description], [Quantity], [UnitPrice],
             [TaxRate], [NetAmount], [TaxAmount], [TotalAmount],
             [CreatedUtc], [CreatedBy], [ModifiedUtc], [ModifiedBy], [IsActive])
        VALUES
            ('20000000-0000-0000-0000-000000000004', @TenantId, @VoidInvoiceId, 1,
             N'Cancelled shipment', 1.0000, 150.0000, 0.180000, 150.0000, 27.0000, 177.0000,
             '2026-01-12T10:00:00Z', 1, '2026-01-13T11:00:00Z', 1, 1);

    -- Status histories illustrate each supported lifecycle path.
    IF NOT EXISTS (SELECT 1 FROM [InvoiceStatusHistory] WHERE [Id] = '30000000-0000-0000-0000-000000000001')
        INSERT INTO [InvoiceStatusHistory]
            ([Id], [TenantId], [InvoiceId], [FromStatusId], [ToStatusId], [Reason], [ChangedUtc], [ChangedBy], [CorrelationId])
        VALUES
            ('30000000-0000-0000-0000-000000000001', @TenantId, @DraftInvoiceId, NULL, 1, NULL,
             '2026-01-15T09:10:00Z', 1, 'seed-draft-created');

    IF NOT EXISTS (SELECT 1 FROM [InvoiceStatusHistory] WHERE [Id] = '30000000-0000-0000-0000-000000000002')
        INSERT INTO [InvoiceStatusHistory]
            ([Id], [TenantId], [InvoiceId], [FromStatusId], [ToStatusId], [Reason], [ChangedUtc], [ChangedBy], [CorrelationId])
        VALUES
            ('30000000-0000-0000-0000-000000000002', @TenantId, @IssuedInvoiceId, NULL, 1, NULL,
             '2026-01-16T09:00:00Z', 1, 'seed-issued-created');

    IF NOT EXISTS (SELECT 1 FROM [InvoiceStatusHistory] WHERE [Id] = '30000000-0000-0000-0000-000000000003')
        INSERT INTO [InvoiceStatusHistory]
            ([Id], [TenantId], [InvoiceId], [FromStatusId], [ToStatusId], [Reason], [ChangedUtc], [ChangedBy], [CorrelationId])
        VALUES
            ('30000000-0000-0000-0000-000000000003', @TenantId, @IssuedInvoiceId, 1, 2, NULL,
             '2026-01-16T09:05:00Z', 1, 'seed-issued-issued');

    IF NOT EXISTS (SELECT 1 FROM [InvoiceStatusHistory] WHERE [Id] = '30000000-0000-0000-0000-000000000004')
        INSERT INTO [InvoiceStatusHistory]
            ([Id], [TenantId], [InvoiceId], [FromStatusId], [ToStatusId], [Reason], [ChangedUtc], [ChangedBy], [CorrelationId])
        VALUES
            ('30000000-0000-0000-0000-000000000004', @TenantId, @PaidInvoiceId, NULL, 1, NULL,
             '2026-01-10T10:00:00Z', 1, 'seed-paid-created');

    IF NOT EXISTS (SELECT 1 FROM [InvoiceStatusHistory] WHERE [Id] = '30000000-0000-0000-0000-000000000005')
        INSERT INTO [InvoiceStatusHistory]
            ([Id], [TenantId], [InvoiceId], [FromStatusId], [ToStatusId], [Reason], [ChangedUtc], [ChangedBy], [CorrelationId])
        VALUES
            ('30000000-0000-0000-0000-000000000005', @TenantId, @PaidInvoiceId, 1, 2, NULL,
             '2026-01-10T10:05:00Z', 1, 'seed-paid-issued');

    IF NOT EXISTS (SELECT 1 FROM [InvoiceStatusHistory] WHERE [Id] = '30000000-0000-0000-0000-000000000006')
        INSERT INTO [InvoiceStatusHistory]
            ([Id], [TenantId], [InvoiceId], [FromStatusId], [ToStatusId], [Reason], [ChangedUtc], [ChangedBy], [CorrelationId])
        VALUES
            ('30000000-0000-0000-0000-000000000006', @TenantId, @PaidInvoiceId, 2, 3, N'PAY-DEMO-001',
             '2026-01-20T11:00:00Z', 1, 'seed-paid-paid');

    IF NOT EXISTS (SELECT 1 FROM [InvoiceStatusHistory] WHERE [Id] = '30000000-0000-0000-0000-000000000007')
        INSERT INTO [InvoiceStatusHistory]
            ([Id], [TenantId], [InvoiceId], [FromStatusId], [ToStatusId], [Reason], [ChangedUtc], [ChangedBy], [CorrelationId])
        VALUES
            ('30000000-0000-0000-0000-000000000007', @TenantId, @VoidInvoiceId, NULL, 1, NULL,
             '2026-01-12T10:00:00Z', 1, 'seed-void-created');

    IF NOT EXISTS (SELECT 1 FROM [InvoiceStatusHistory] WHERE [Id] = '30000000-0000-0000-0000-000000000008')
        INSERT INTO [InvoiceStatusHistory]
            ([Id], [TenantId], [InvoiceId], [FromStatusId], [ToStatusId], [Reason], [ChangedUtc], [ChangedBy], [CorrelationId])
        VALUES
            ('30000000-0000-0000-0000-000000000008', @TenantId, @VoidInvoiceId, 1, 2, NULL,
             '2026-01-12T10:05:00Z', 1, 'seed-void-issued');

    IF NOT EXISTS (SELECT 1 FROM [InvoiceStatusHistory] WHERE [Id] = '30000000-0000-0000-0000-000000000009')
        INSERT INTO [InvoiceStatusHistory]
            ([Id], [TenantId], [InvoiceId], [FromStatusId], [ToStatusId], [Reason], [ChangedUtc], [ChangedBy], [CorrelationId])
        VALUES
            ('30000000-0000-0000-0000-000000000009', @TenantId, @VoidInvoiceId, 2, 4, N'Customer cancellation',
             '2026-01-13T11:00:00Z', 1, 'seed-void-voided');

    -- This is a realistic processing record for understanding the idempotency table.
    -- Do not reuse this key in API requests.
    IF NOT EXISTS (SELECT 1 FROM [IdempotencyRequests] WHERE [Id] = '40000000-0000-0000-0000-000000000001')
        INSERT INTO [IdempotencyRequests]
            ([Id], [TenantId], [Operation], [IdempotencyKey], [RequestHash], [State],
             [ResourceId], [ResponseStatus], [ResponseBody], [CorrelationId],
             [CreatedUtc], [CompletedUtc], [ExpiresUtc])
        VALUES
            ('40000000-0000-0000-0000-000000000001', @TenantId, 'invoice.create',
             N'seed-reference-only', HASHBYTES('SHA2_256', N'seed-reference-only'), 1,
             NULL, NULL, NULL, 'seed-idempotency-processing',
             @SeedUtc, NULL, '2099-01-01T00:00:00Z');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

-- Generate representative rows in the system-maintained temporal history tables.
-- A separate transaction ensures the history period is distinct from the insert.
WAITFOR DELAY '00:00:00.010';

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM [Invoices] WHERE [Id] = @IssuedInvoiceId)
       AND NOT EXISTS (SELECT 1 FROM [history].[InvoicesHistory] WHERE [Id] = @IssuedInvoiceId)
    BEGIN
        UPDATE [Invoices]
        SET [Notes] = N'Issued sample invoice (temporal history generated)',
            [ModifiedUtc] = DATEADD(second, 1, [ModifiedUtc]),
            [ModifiedBy] = 1
        WHERE [Id] = @IssuedInvoiceId;
    END;

    IF EXISTS (SELECT 1 FROM [InvoiceLineItems] WHERE [Id] = '20000000-0000-0000-0000-000000000003')
       AND NOT EXISTS (
           SELECT 1 FROM [history].[InvoiceLineItemsHistory]
           WHERE [Id] = '20000000-0000-0000-0000-000000000003')
    BEGIN
        UPDATE [InvoiceLineItems]
        SET [Description] = N'Consulting service (temporal history generated)',
            [ModifiedUtc] = DATEADD(second, 1, [ModifiedUtc]),
            [ModifiedBy] = 1
        WHERE [Id] = '20000000-0000-0000-0000-000000000003';
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT N'Tenants' AS [TableName], COUNT_BIG(*) AS [DemoRows] FROM [Tenants] WHERE [Id] = @TenantId
UNION ALL SELECT N'InvoiceStatuses', COUNT_BIG(*) FROM [InvoiceStatuses] WHERE [Id] BETWEEN 1 AND 4
UNION ALL SELECT N'Customers', COUNT_BIG(*) FROM [Customers] WHERE [TenantId] = @TenantId
UNION ALL SELECT N'CustomerLocations', COUNT_BIG(*) FROM [CustomerLocations] WHERE [TenantId] = @TenantId
UNION ALL SELECT N'Invoices', COUNT_BIG(*) FROM [Invoices] WHERE [TenantId] = @TenantId
UNION ALL SELECT N'InvoiceLineItems', COUNT_BIG(*) FROM [InvoiceLineItems] WHERE [TenantId] = @TenantId
UNION ALL SELECT N'InvoiceStatusHistory', COUNT_BIG(*) FROM [InvoiceStatusHistory] WHERE [TenantId] = @TenantId
UNION ALL SELECT N'InvoiceNumberSequences', COUNT_BIG(*) FROM [InvoiceNumberSequences] WHERE [TenantId] = @TenantId
UNION ALL SELECT N'IdempotencyRequests', COUNT_BIG(*) FROM [IdempotencyRequests] WHERE [TenantId] = @TenantId
UNION ALL SELECT N'history.InvoicesHistory', COUNT_BIG(*) FROM [history].[InvoicesHistory] WHERE [TenantId] = @TenantId
UNION ALL SELECT N'history.InvoiceLineItemsHistory', COUNT_BIG(*) FROM [history].[InvoiceLineItemsHistory] WHERE [TenantId] = @TenantId;
