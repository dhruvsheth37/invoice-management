using InvoiceManagement.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceManagement.Infrastructure.Persistence.Configurations;

internal sealed class CustomerLocationConfiguration : IEntityTypeConfiguration<CustomerLocation>
{
    public void Configure(EntityTypeBuilder<CustomerLocation> builder)
    {
        builder.ToTable("CustomerLocations", table =>
            table.HasCheckConstraint(
                "CK_CustomerLocations_DeletionMetadata",
                "([IsDeleted] = 0 AND [DeletedUtc] IS NULL AND [DeletedBy] IS NULL) OR " +
                "([IsDeleted] = 1 AND [DeletedUtc] IS NOT NULL AND [DeletedBy] IS NOT NULL)"));

        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.TenantId, entity.CustomerId, entity.Id });
        builder.HasAlternateKey(entity => new { entity.TenantId, entity.Id });
        builder.Property(entity => entity.Name).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.AddressLine1).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.AddressLine2).HasMaxLength(200);
        builder.Property(entity => entity.City).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.StateProvince).HasMaxLength(100);
        builder.Property(entity => entity.PostalCode).HasMaxLength(20);
        builder.Property(entity => entity.CountryCode).HasColumnType("char(2)").IsRequired();
        builder.Property(entity => entity.TaxNumber).HasMaxLength(50);
        builder.Property(entity => entity.IsActive).HasDefaultValue(true);
        builder.ConfigureAudit();
        builder.ConfigureSoftDeletion();

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(entity => new { entity.TenantId, entity.CustomerId })
            .HasPrincipalKey(entity => new { entity.TenantId, entity.Id })
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(entity => new { entity.TenantId, entity.CustomerId, entity.Name })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
