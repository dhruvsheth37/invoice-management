using InvoiceManagement.Application.Abstractions.Tenancy;
using InvoiceManagement.Domain.Customers;
using InvoiceManagement.Domain.Invoices;
using InvoiceManagement.Domain.Tenants;
using InvoiceManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace InvoiceManagement.IntegrationTests.Persistence;

public sealed class PersistenceModelTests
{
    [Fact]
    public void Invoice_and_line_item_are_temporal_and_activatable()
    {
        using var context = CreateContext();
        var invoice = context.Model.FindEntityType(typeof(Invoice));
        var lineItem = context.Model.FindEntityType(typeof(InvoiceLineItem));

        Assert.NotNull(invoice);
        Assert.NotNull(lineItem);
        Assert.True(invoice.IsTemporal());
        Assert.True(lineItem.IsTemporal());
        Assert.NotNull(invoice.FindProperty(nameof(Invoice.IsActive)));
        Assert.NotNull(lineItem.FindProperty(nameof(InvoiceLineItem.IsActive)));
        Assert.Null(invoice.FindProperty("IsDeleted"));
        Assert.Null(lineItem.FindProperty("IsDeleted"));
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

    [Theory]
    [InlineData(typeof(Tenant))]
    [InlineData(typeof(Customer))]
    [InlineData(typeof(CustomerLocation))]
    [InlineData(typeof(Invoice))]
    [InlineData(typeof(InvoiceLineItem))]
    public void Audit_user_columns_are_required_integers_with_default_user_id(Type entityType)
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(entityType)!;

        AssertRequiredUserId(entity.FindProperty("CreatedBy"));
        AssertRequiredUserId(entity.FindProperty("ModifiedBy"));
    }

    [Fact]
    public void Status_history_actor_is_a_required_integer_with_default_user_id()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(InvoiceStatusHistory))!;

        AssertRequiredUserId(entity.FindProperty(nameof(InvoiceStatusHistory.ChangedBy)));
    }

    private static void AssertRequiredUserId(IProperty? property)
    {
        Assert.NotNull(property);
        Assert.Equal(typeof(int), property.ClrType);
        Assert.False(property.IsNullable);
        Assert.Equal(1, property.GetDefaultValue());
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
