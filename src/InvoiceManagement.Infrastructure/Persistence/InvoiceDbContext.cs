using InvoiceManagement.Application.Abstractions.Persistence;
using InvoiceManagement.Application.Abstractions.Tenancy;
using InvoiceManagement.Domain.Customers;
using InvoiceManagement.Domain.Invoices;
using InvoiceManagement.Domain.Platform;
using InvoiceManagement.Domain.Tenants;
using Microsoft.EntityFrameworkCore;

namespace InvoiceManagement.Infrastructure.Persistence;

public sealed class InvoiceDbContext(
    DbContextOptions<InvoiceDbContext> options,
    ITenantContext tenantContext)
    : DbContext(options), IInvoiceDbContext
{
    private Guid CurrentTenantId => tenantContext.TenantId;

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<CustomerLocation> CustomerLocations => Set<CustomerLocation>();

    public DbSet<InvoiceStatusReference> InvoiceStatuses => Set<InvoiceStatusReference>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();

    public DbSet<InvoiceStatusHistory> InvoiceStatusHistory => Set<InvoiceStatusHistory>();

    public DbSet<InvoiceNumberSequence> InvoiceNumberSequences => Set<InvoiceNumberSequence>();

    public DbSet<IdempotencyRequest> IdempotencyRequests => Set<IdempotencyRequest>();

    IQueryable<Customer> IInvoiceDbContext.Customers => Customers;

    IQueryable<CustomerLocation> IInvoiceDbContext.CustomerLocations => CustomerLocations;

    IQueryable<Invoice> IInvoiceDbContext.Invoices => Invoices;

    public void Add(Invoice invoice) => Invoices.Add(invoice);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoiceDbContext).Assembly);

        modelBuilder.Entity<Tenant>()
            .HasQueryFilter(entity => entity.Id == CurrentTenantId);
        modelBuilder.Entity<Customer>()
            .HasQueryFilter(entity => entity.TenantId == CurrentTenantId && entity.IsActive);
        modelBuilder.Entity<CustomerLocation>()
            .HasQueryFilter(entity => entity.TenantId == CurrentTenantId && entity.IsActive);
        modelBuilder.Entity<Invoice>()
            .HasQueryFilter(entity => entity.TenantId == CurrentTenantId && entity.IsActive);
        modelBuilder.Entity<InvoiceLineItem>()
            .HasQueryFilter(entity => entity.TenantId == CurrentTenantId && entity.IsActive);
        modelBuilder.Entity<InvoiceStatusHistory>()
            .HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        modelBuilder.Entity<InvoiceNumberSequence>()
            .HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
        modelBuilder.Entity<IdempotencyRequest>()
            .HasQueryFilter(entity => entity.TenantId == CurrentTenantId);
    }
}
