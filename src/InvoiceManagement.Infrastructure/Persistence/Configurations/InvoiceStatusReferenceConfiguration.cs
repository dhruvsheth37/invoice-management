using InvoiceManagement.Domain.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceManagement.Infrastructure.Persistence.Configurations;

internal sealed class InvoiceStatusReferenceConfiguration : IEntityTypeConfiguration<InvoiceStatusReference>
{
    public void Configure(EntityTypeBuilder<InvoiceStatusReference> builder)
    {
        builder.ToTable("InvoiceStatuses");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).HasConversion<byte>().ValueGeneratedNever();
        builder.Property(entity => entity.Code).HasColumnType("varchar(32)").IsRequired();
        builder.Property(entity => entity.DisplayName).HasMaxLength(50).IsRequired();
        builder.HasIndex(entity => entity.Code).IsUnique();

        builder.HasData(
            new { Id = InvoiceStatus.Draft, Code = "Draft", DisplayName = "Draft", SortOrder = (byte)1 },
            new { Id = InvoiceStatus.Issued, Code = "Issued", DisplayName = "Issued", SortOrder = (byte)2 },
            new { Id = InvoiceStatus.Paid, Code = "Paid", DisplayName = "Paid", SortOrder = (byte)3 },
            new { Id = InvoiceStatus.Void, Code = "Void", DisplayName = "Void", SortOrder = (byte)4 });
    }
}
