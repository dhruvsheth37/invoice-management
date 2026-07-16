using InvoiceManagement.Application.Abstractions.Tenancy;
using InvoiceManagement.Domain.Customers;
using InvoiceManagement.Domain.Invoices;
using InvoiceManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace InvoiceManagement.IntegrationTests.Persistence;

public sealed class PersistenceModelTests
{
    [Fact]
    public void Invoice_and_line_item_are_temporal_and_soft_deletable()
    {
        using var context = CreateContext();
        var invoice = context.Model.FindEntityType(typeof(Invoice));
        var lineItem = context.Model.FindEntityType(typeof(InvoiceLineItem));

        Assert.NotNull(invoice);
        Assert.NotNull(lineItem);
        Assert.True(invoice.IsTemporal());
        Assert.True(lineItem.IsTemporal());
        Assert.NotNull(invoice.FindProperty(nameof(Invoice.IsDeleted)));
        Assert.NotNull(lineItem.FindProperty(nameof(InvoiceLineItem.IsDeleted)));
    }

    [Fact]
    public void Invoice_customer_location_relationship_contains_tenant_and_customer_keys()
    {
        using var context = CreateContext();
        var invoice = context.Model.FindEntityType(typeof(Invoice))!;
        var foreignKey = invoice.GetForeignKeys().Single(key => key.PrincipalEntityType.ClrType == typeof(CustomerLocation));

        Assert.Equal(
            [nameof(Invoice.TenantId), nameof(Invoice.CustomerId), nameof(Invoice.CustomerLocationId)],
            foreignKey.Properties.Select(property => property.Name));
    }

    private static InvoiceDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InvoiceDbContext>()
            .UseSqlServer("Server=localhost;Database=ModelOnly;User Id=sa;TrustServerCertificate=True")
            .Options;

        return new InvoiceDbContext(options, new TestTenantContext());
    }

    private sealed class TestTenantContext : ITenantContext
    {
        public Guid TenantId { get; } = Guid.NewGuid();

        public bool IsResolved => true;
    }
}
