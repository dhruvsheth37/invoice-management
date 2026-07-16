using InvoiceManagement.Domain.Invoices;
using Microsoft.EntityFrameworkCore;

namespace InvoiceManagement.IntegrationTests.SqlServer;

[Collection(SqlServerTestGroup.Name)]
public sealed class PersistenceSqlServerTests(SqlServerFixture fixture)
{
    private static readonly DateTime Now = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

    [SqlServerFact]
    public async Task Query_filters_isolate_tenants_and_hide_inactive_drafts()
    {
        var firstTenant = await fixture.SeedTenantAsync("filter-a");
        var secondTenant = await fixture.SeedTenantAsync("filter-b");
        var invoice = CreateDraft(firstTenant);
        await using (var firstContext = fixture.CreateContext(firstTenant.TenantId))
        {
            firstContext.Invoices.Add(invoice);
            await firstContext.SaveChangesAsync();
        }

        await using (var secondContext = fixture.CreateContext(secondTenant.TenantId))
        {
            Assert.False(await secondContext.Invoices.AnyAsync(item => item.Id == invoice.Id));
        }

        await using (var firstContext = fixture.CreateContext(firstTenant.TenantId))
        {
            var tracked = await firstContext.Invoices.Include(item => item.LineItems).SingleAsync(item => item.Id == invoice.Id);
            tracked.DeactivateDraft(Now.AddMinutes(1), 22);
            await firstContext.SaveChangesAsync();
        }

        await using var verificationContext = fixture.CreateContext(firstTenant.TenantId);
        Assert.False(await verificationContext.Invoices.AnyAsync(item => item.Id == invoice.Id));
        Assert.True(await verificationContext.Invoices.IgnoreQueryFilters().AnyAsync(item => item.Id == invoice.Id && !item.IsActive));
    }

    [SqlServerFact]
    public async Task Temporal_history_and_rowversion_concurrency_are_enforced_by_sql_server()
    {
        var tenant = await fixture.SeedTenantAsync("temporal");
        var invoice = CreateDraft(tenant);
        await using (var setup = fixture.CreateContext(tenant.TenantId))
        {
            setup.Invoices.Add(invoice);
            await setup.SaveChangesAsync();
        }

        await using var firstContext = fixture.CreateContext(tenant.TenantId);
        await using var secondContext = fixture.CreateContext(tenant.TenantId);
        var first = await firstContext.Invoices.Include(item => item.StatusHistory).SingleAsync(item => item.Id == invoice.Id);
        var second = await secondContext.Invoices.Include(item => item.StatusHistory).SingleAsync(item => item.Id == invoice.Id);
        first.Void("first writer", Now.AddMinutes(1), 31, "first-writer");
        second.Void("second writer", Now.AddMinutes(2), 32, "second-writer");

        await firstContext.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());

        await using var verification = fixture.CreateContext(tenant.TenantId);
        var historyCount = await verification.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [history].[InvoicesHistory] WHERE [Id] = {invoice.Id}")
            .SingleAsync();
        Assert.True(historyCount >= 1);
        var persisted = await verification.Invoices.IgnoreQueryFilters().SingleAsync(item => item.Id == invoice.Id);
        Assert.Equal("first writer", persisted.VoidReason);
        Assert.Equal(31, persisted.ModifiedBy);
    }

    private static Invoice CreateDraft(TenantData tenant)
    {
        var invoice = Invoice.CreateDraft(
            Guid.CreateVersion7(),
            tenant.TenantId,
            tenant.CustomerId,
            tenant.LocationId,
            "USD",
            new DateOnly(2026, 8, 15),
            null,
            Now,
            1,
            "created");
        invoice.AddLine(Guid.CreateVersion7(), 1, "Freight", 1, 100, 0.18m, Now, 1);
        return invoice;
    }
}
