using InvoiceManagement.Domain.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceManagement.Infrastructure.Persistence.Configurations;

internal sealed class InvoiceLineItemConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItem> builder)
    {
        builder.ToTable("InvoiceLineItems", table =>
        {
            table.IsTemporal(temporal =>
            {
                temporal.HasPeriodStart("ValidFromUtc").HasColumnName("ValidFromUtc");
                temporal.HasPeriodEnd("ValidToUtc").HasColumnName("ValidToUtc");
                temporal.UseHistoryTable("InvoiceLineItemsHistory", "history");
            });
            table.HasCheckConstraint("CK_InvoiceLineItems_Values", "[LineNumber] > 0 AND [Quantity] > 0 AND [UnitPrice] >= 0 AND [TaxRate] >= 0 AND [TaxRate] <= 1");
            table.HasCheckConstraint("CK_InvoiceLineItems_Amounts", "[NetAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] = [NetAmount] + [TaxAmount]");
            table.HasCheckConstraint("CK_InvoiceLineItems_DeletionMetadata", "([IsDeleted] = 0 AND [DeletedUtc] IS NULL AND [DeletedBy] IS NULL) OR ([IsDeleted] = 1 AND [DeletedUtc] IS NOT NULL AND [DeletedBy] IS NOT NULL)");
        });

        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.TenantId, entity.Id });
        builder.Property(entity => entity.Description).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.Quantity).HasPrecision(18, 4);
        builder.Property(entity => entity.UnitPrice).HasPrecision(19, 4);
        builder.Property(entity => entity.TaxRate).HasPrecision(9, 6);
        builder.Property(entity => entity.NetAmount).HasPrecision(19, 4);
        builder.Property(entity => entity.TaxAmount).HasPrecision(19, 4);
        builder.Property(entity => entity.TotalAmount).HasPrecision(19, 4);
        builder.ConfigureAudit();
        builder.ConfigureSoftDeletion();

        builder.HasIndex(entity => new { entity.TenantId, entity.InvoiceId, entity.LineNumber })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
