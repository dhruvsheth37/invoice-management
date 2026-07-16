using InvoiceManagement.Domain.Customers;
using InvoiceManagement.Domain.Invoices;
using InvoiceManagement.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceManagement.Infrastructure.Persistence.Configurations;

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices", table =>
        {
            table.IsTemporal(temporal =>
            {
                temporal.HasPeriodStart("ValidFromUtc").HasColumnName("ValidFromUtc");
                temporal.HasPeriodEnd("ValidToUtc").HasColumnName("ValidToUtc");
                temporal.UseHistoryTable("InvoicesHistory", "history");
            });
            table.HasCheckConstraint("CK_Invoices_Amounts", "[Subtotal] >= 0 AND [TaxTotal] >= 0 AND [Total] >= 0 AND [Total] = [Subtotal] + [TaxTotal]");
            table.HasCheckConstraint("CK_Invoices_Dates", "[DueDate] IS NULL OR [IssueDate] IS NULL OR [DueDate] >= [IssueDate]");
            table.HasCheckConstraint("CK_Invoices_Draft", "[StatusId] <> 1 OR ([InvoiceNumber] IS NULL AND [IssueDate] IS NULL AND [PaidDate] IS NULL)");
            table.HasCheckConstraint("CK_Invoices_Paid", "([StatusId] = 3 AND [PaidDate] IS NOT NULL) OR ([StatusId] <> 3 AND [PaidDate] IS NULL)");
            table.HasCheckConstraint("CK_Invoices_Void", "([StatusId] = 4 AND [VoidReason] IS NOT NULL) OR ([StatusId] <> 4 AND [VoidReason] IS NULL)");
            table.HasCheckConstraint("CK_Invoices_IssuedSnapshot", "[StatusId] NOT IN (2, 3) OR ([InvoiceNumber] IS NOT NULL AND [IssueDate] IS NOT NULL AND [DueDate] IS NOT NULL AND [BillToCustomerCode] IS NOT NULL AND [BillToLegalName] IS NOT NULL AND [BillToAddressLine1] IS NOT NULL AND [BillToCity] IS NOT NULL AND [BillToCountryCode] IS NOT NULL)");
            table.HasCheckConstraint("CK_Invoices_DeletionMetadata", "([IsDeleted] = 0 AND [DeletedUtc] IS NULL AND [DeletedBy] IS NULL) OR ([IsDeleted] = 1 AND [StatusId] = 1 AND [DeletedUtc] IS NOT NULL AND [DeletedBy] IS NOT NULL)");
        });

        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.TenantId, entity.Id });
        builder.Property(entity => entity.BillToCustomerCode).HasMaxLength(50);
        builder.Property(entity => entity.BillToLegalName).HasMaxLength(200);
        builder.Property(entity => entity.BillToTaxNumber).HasMaxLength(50);
        builder.Property(entity => entity.BillToAddressLine1).HasMaxLength(200);
        builder.Property(entity => entity.BillToAddressLine2).HasMaxLength(200);
        builder.Property(entity => entity.BillToCity).HasMaxLength(100);
        builder.Property(entity => entity.BillToStateProvince).HasMaxLength(100);
        builder.Property(entity => entity.BillToPostalCode).HasMaxLength(20);
        builder.Property(entity => entity.BillToCountryCode).HasColumnType("char(2)");
        builder.Property(entity => entity.InvoiceNumber).HasMaxLength(50);
        builder.Property(entity => entity.Status).HasColumnName("StatusId").HasConversion<byte>();
        builder.Property(entity => entity.CurrencyCode).HasColumnType("char(3)").IsRequired();
        builder.Property(entity => entity.Subtotal).HasPrecision(19, 4);
        builder.Property(entity => entity.TaxTotal).HasPrecision(19, 4);
        builder.Property(entity => entity.Total).HasPrecision(19, 4);
        builder.Property(entity => entity.PaymentReference).HasMaxLength(100);
        builder.Property(entity => entity.Notes).HasMaxLength(1000);
        builder.Property(entity => entity.VoidReason).HasMaxLength(500);
        builder.ConfigureAudit();
        builder.ConfigureSoftDeletion();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(entity => entity.TenantId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.CustomerId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id })
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<CustomerLocation>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.CustomerId, entity.CustomerLocationId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.CustomerId, entity.Id })
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<InvoiceStatusReference>()
            .WithMany()
            .HasForeignKey(entity => entity.Status)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(entity => entity.LineItems)
            .WithOne()
            .HasForeignKey(entity => new { entity.TenantId, entity.InvoiceId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id })
            .OnDelete(DeleteBehavior.NoAction);
        builder.Navigation(entity => entity.LineItems).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(entity => entity.StatusHistory)
            .WithOne()
            .HasForeignKey(entity => new { entity.TenantId, entity.InvoiceId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id })
            .OnDelete(DeleteBehavior.NoAction);
        builder.Navigation(entity => entity.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(entity => new { entity.TenantId, entity.InvoiceNumber })
            .IsUnique()
            .HasFilter("[InvoiceNumber] IS NOT NULL");
        builder.HasIndex(entity => new { entity.TenantId, entity.IsDeleted, entity.Status, entity.CreatedUtc, entity.Id })
            .IsDescending(false, false, false, true, true)
            .IncludeProperties(entity => new { entity.InvoiceNumber, entity.CustomerId, entity.Total, entity.CurrencyCode, entity.DueDate });
        builder.HasIndex(entity => new { entity.TenantId, entity.IsDeleted, entity.CustomerId, entity.CreatedUtc })
            .IsDescending(false, false, false, true);
        builder.HasIndex(entity => new { entity.TenantId, entity.IsDeleted, entity.Status, entity.DueDate })
            .IncludeProperties(entity => new { entity.CurrencyCode, entity.Total });
    }
}
