using System.Linq.Expressions;
using InvoiceManagement.Application.Abstractions.Persistence;
using InvoiceManagement.Application.Abstractions.Tenancy;
using InvoiceManagement.Domain.Common;
using InvoiceManagement.Domain.Customers;
using InvoiceManagement.Domain.Invoices;
using InvoiceManagement.Domain.Platform;
using InvoiceManagement.Domain.Tenants;
using Microsoft.EntityFrameworkCore;

namespace InvoiceManagement.Infrastructure.Persistence;

public sealed class InvoiceDbContext(DbContextOptions<InvoiceDbContext> options)
    : DbContext(options), IInvoiceDbContext
{
    private Guid CurrentTenantId { get; set; }

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

    public void SetTenant(Guid tenantId) => CurrentTenantId = tenantId;

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateTenantWrites();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ValidateTenantWrites();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoiceDbContext).Assembly);

        modelBuilder.Entity<Tenant>()
            .HasQueryFilter("TenantFilter", entity => entity.Id == CurrentTenantId);

        ApplyScopedQueryFilters(modelBuilder);
    }

    private void ApplyScopedQueryFilters(ModelBuilder modelBuilder)
    {
        Expression<Func<Guid>> currentTenant = () => CurrentTenantId;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var parameter = Expression.Parameter(clrType, "entity");
            var builder = modelBuilder.Entity(clrType);

            if (typeof(ITenantScoped).IsAssignableFrom(clrType))
            {
                var tenantId = Expression.Property(parameter, nameof(ITenantScoped.TenantId));
                var tenantFilter = Expression.Lambda(
                    Expression.Equal(tenantId, currentTenant.Body),
                    parameter);
                builder.HasQueryFilter("TenantFilter", tenantFilter);
            }

            if (typeof(IActivatable).IsAssignableFrom(clrType))
            {
                var isActive = Expression.Property(parameter, nameof(IActivatable.IsActive));
                var activeFilter = Expression.Lambda(isActive, parameter);
                builder.HasQueryFilter("ActiveFilter", activeFilter);
            }
        }
    }

    private void ValidateTenantWrites()
    {
        var changedEntries = ChangeTracker.Entries()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);

        foreach (var entry in changedEntries)
        {
            var entityTenantId = entry.Entity switch
            {
                ITenantScoped tenantScoped => tenantScoped.TenantId,
                Tenant tenant => tenant.Id,
                _ => (Guid?)null,
            };

            if (entityTenantId.HasValue &&
                (CurrentTenantId == Guid.Empty || entityTenantId.Value != CurrentTenantId))
            {
                throw new TenantIsolationException(
                    $"A {entry.Metadata.ClrType.Name} cannot be written outside the current tenant scope.");
            }
        }
    }
}
