using InvoiceManagement.Domain.Platform;
using InvoiceManagement.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceManagement.Infrastructure.Persistence.Configurations;

internal sealed class IdempotencyRequestConfiguration : IEntityTypeConfiguration<IdempotencyRequest>
{
    public void Configure(EntityTypeBuilder<IdempotencyRequest> builder)
    {
        builder.ToTable("IdempotencyRequests");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Operation).HasColumnType("varchar(100)").IsRequired();
        builder.Property(entity => entity.IdempotencyKey).HasMaxLength(100).IsRequired();
        builder.Property(entity => entity.RequestHash).HasColumnType("binary(32)").IsRequired();
        builder.Property(entity => entity.State).HasConversion<byte>();
        builder.Property(entity => entity.ResponseBody).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.CorrelationId).HasColumnType("varchar(64)").IsRequired();
        builder.Property(entity => entity.CreatedUtc).HasColumnType("datetime2(7)");
        builder.Property(entity => entity.CompletedUtc).HasColumnType("datetime2(7)");
        builder.Property(entity => entity.ExpiresUtc).HasColumnType("datetime2(7)");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(entity => entity.TenantId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(entity => new { entity.TenantId, entity.Operation, entity.IdempotencyKey }).IsUnique();
        builder.HasIndex(entity => entity.ExpiresUtc);
    }
}
