using InvoiceManagement.Domain.Customers;
using InvoiceManagement.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceManagement.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(entity => entity.Id);
        builder.HasAlternateKey(entity => new { entity.TenantId, entity.Id });
        builder.Property(entity => entity.Code).HasMaxLength(50).IsRequired();
        builder.Property(entity => entity.LegalName).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.TaxNumber).HasMaxLength(50);
        builder.Property(entity => entity.Email).HasMaxLength(254);
        builder.ConfigureAudit();
        builder.ConfigureActivation();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(entity => entity.TenantId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(entity => new { entity.TenantId, entity.Code })
            .IsUnique()
            .HasFilter("[IsActive] = 1");
        builder.HasIndex(entity => new { entity.TenantId, entity.IsActive, entity.LegalName });
    }
}
