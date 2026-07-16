using InvoiceManagement.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceManagement.Infrastructure.Persistence.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    private static readonly Guid DevelopmentTenantId = new("11111111-1111-1111-1111-111111111111");

    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants", table =>
        {
            table.HasCheckConstraint("CK_Tenants_Slug_NotBlank", "LEN(LTRIM(RTRIM([Slug]))) > 0");
            table.HasCheckConstraint("CK_Tenants_Name_NotBlank", "LEN(LTRIM(RTRIM([Name]))) > 0");
        });

        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Slug).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.IsActive).HasDefaultValue(true);
        builder.HasIndex(entity => entity.Slug).IsUnique();
        builder.ConfigureAudit();

        builder.HasData(new
        {
            Id = DevelopmentTenantId,
            Slug = "demo",
            Name = "Demo Tenant",
            IsActive = true,
            CreatedUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "seed",
        });
    }
}
