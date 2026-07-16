using InvoiceManagement.Domain.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceManagement.Infrastructure.Persistence.Configurations;

internal sealed class InvoiceStatusHistoryConfiguration : IEntityTypeConfiguration<InvoiceStatusHistory>
{
    public void Configure(EntityTypeBuilder<InvoiceStatusHistory> builder)
    {
        builder.ToTable("InvoiceStatusHistory");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.FromStatus).HasColumnName("FromStatusId").HasConversion<byte?>();
        builder.Property(entity => entity.ToStatus).HasColumnName("ToStatusId").HasConversion<byte>();
        builder.Property(entity => entity.Reason).HasMaxLength(500);
        builder.Property(entity => entity.ChangedUtc).HasColumnType("datetime2(7)");
        builder.Property(entity => entity.ChangedBy).HasDefaultValue(1).IsRequired();
        builder.Property(entity => entity.CorrelationId).HasColumnType("varchar(64)").IsRequired();

        builder.HasOne<InvoiceStatusReference>()
            .WithMany()
            .HasForeignKey(entity => entity.FromStatus)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<InvoiceStatusReference>()
            .WithMany()
            .HasForeignKey(entity => entity.ToStatus)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(entity => new { entity.TenantId, entity.InvoiceId, entity.ChangedUtc })
            .IsDescending(false, false, true);
    }
}
