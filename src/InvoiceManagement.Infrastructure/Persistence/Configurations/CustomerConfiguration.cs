using InvoiceManagement.Domain.Customers;
using InvoiceManagement.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceManagement.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers", table =>
            table.HasCheckConstraint(
                "CK_Customers_DeletionMetadata",
                "([IsDeleted] = 0 AND [DeletedUtc] IS NULL AND [DeletedBy] IS NULL) OR " +
                "([IsDeleted] = 1 AND [DeletedUtc] IS NOT NULL AND [DeletedBy] IS NOT NULL)"));

        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.TenantId, entity.Id });
        builder.Property(entity => entity.Code).HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.LegalName).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.TaxNumber).HasMaxLength(50);
        builder.Property(entity => entity.Email).HasMaxLength(254);
        builder.Property(entity => entity.IsActive).HasDefaultValue(true);
        builder.ConfigureAudit();
        builder.ConfigureSoftDeletion();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(entity => entity.TenantId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(entity => new { entity.TenantId, entity.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(entity => new { entity.TenantId, entity.IsDeleted, entity.LegalName });
    }
}
