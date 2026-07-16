using InvoiceManagement.Domain.Invoices;
using InvoiceManagement.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceManagement.Infrastructure.Persistence.Configurations;

internal sealed class InvoiceNumberSequenceConfiguration : IEntityTypeConfiguration<InvoiceNumberSequence>
{
    public void Configure(EntityTypeBuilder<InvoiceNumberSequence> builder)
    {
        builder.ToTable("InvoiceNumberSequences", table =>
            table.HasCheckConstraint("CK_InvoiceNumberSequences_Value", "[CurrentValue] >= 0"));
        builder.HasKey(entity => new { entity.TenantId, entity.FiscalYear });
        builder.Property(entity => entity.ModifiedUtc).HasColumnType("datetime2(7)");
        builder.Property(entity => entity.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(entity => entity.TenantId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
