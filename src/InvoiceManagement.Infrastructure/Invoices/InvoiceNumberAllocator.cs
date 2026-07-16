using System.Data;
using InvoiceManagement.Application.Abstractions.Tenancy;
using InvoiceManagement.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace InvoiceManagement.Infrastructure.Invoices;

internal sealed class InvoiceNumberAllocator(
    InvoiceDbContext dbContext,
    ITenantContext tenantContext)
{
    private const string AllocateSql =
        """
        SET NOCOUNT ON;

        DECLARE @Allocated TABLE ([Value] bigint NOT NULL);

        UPDATE [dbo].[InvoiceNumberSequences] WITH (UPDLOCK, HOLDLOCK)
        SET [CurrentValue] = [CurrentValue] + 1,
            [ModifiedUtc] = @ModifiedUtc
        OUTPUT INSERTED.[CurrentValue] INTO @Allocated ([Value])
        WHERE [TenantId] = @TenantId
          AND [FiscalYear] = @FiscalYear;

        IF NOT EXISTS (SELECT 1 FROM @Allocated)
        BEGIN
            INSERT INTO [dbo].[InvoiceNumberSequences]
                ([TenantId], [FiscalYear], [CurrentValue], [ModifiedUtc])
            OUTPUT INSERTED.[CurrentValue] INTO @Allocated ([Value])
            VALUES
                (@TenantId, @FiscalYear, 1, @ModifiedUtc);
        END;

        SELECT [Value] FROM @Allocated;
        """;

    public async Task<long> AllocateAsync(short fiscalYear, DateTime modifiedUtc, CancellationToken cancellationToken)
    {
        var transaction = dbContext.Database.CurrentTransaction
            ?? throw new InvalidOperationException("Invoice number allocation requires an active transaction.");
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = AllocateSql;
        command.Parameters.Add(new SqlParameter("@TenantId", SqlDbType.UniqueIdentifier) { Value = tenantContext.TenantId });
        command.Parameters.Add(new SqlParameter("@FiscalYear", SqlDbType.SmallInt) { Value = fiscalYear });
        command.Parameters.Add(new SqlParameter("@ModifiedUtc", SqlDbType.DateTime2) { Value = modifiedUtc });
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result, System.Globalization.CultureInfo.InvariantCulture);
    }
}
