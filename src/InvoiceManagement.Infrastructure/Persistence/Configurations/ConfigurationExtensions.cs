using InvoiceManagement.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceManagement.Infrastructure.Persistence.Configurations;

internal static class ConfigurationExtensions
{
    public static void ConfigureAudit<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : AuditableEntity
    {
        builder.Property(entity => entity.CreatedUtc).HasColumnType("datetime2(7)");
        builder.Property(entity => entity.CreatedBy).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.ModifiedUtc).HasColumnType("datetime2(7)");
        builder.Property(entity => entity.ModifiedBy).HasMaxLength(200);
        builder.Property(entity => entity.RowVersion).IsRowVersion().IsConcurrencyToken();
    }

    public static void ConfigureActivation<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : ActivatableTenantEntity
    {
        builder.Property(entity => entity.IsActive).HasDefaultValue(true);
    }
}
