using InvoiceManagement.Application.Abstractions.Persistence;
using InvoiceManagement.Application.Abstractions.Tenancy;
using InvoiceManagement.Application.Invoices;
using InvoiceManagement.Infrastructure.Invoices;
using InvoiceManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("InvoiceDatabase")
            ?? throw new InvalidOperationException("Connection string 'InvoiceDatabase' is required.");

        var poolSize = int.TryParse(configuration["Database:DbContextPoolSize"], out var configuredPoolSize)
            && configuredPoolSize > 0
                ? configuredPoolSize
                : 128;
        services.AddPooledDbContextFactory<InvoiceDbContext>(
            options => options.UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(typeof(InvoiceDbContext).Assembly.FullName)),
            poolSize);

        services.AddScoped(provider =>
        {
            var dbContext = provider.GetRequiredService<IDbContextFactory<InvoiceDbContext>>().CreateDbContext();
            var tenantContext = provider.GetRequiredService<ITenantContext>();
            dbContext.SetTenant(tenantContext.IsResolved ? tenantContext.TenantId : Guid.Empty);
            return dbContext;
        });

        services.AddScoped<IInvoiceDbContext>(provider => provider.GetRequiredService<InvoiceDbContext>());
        services.AddScoped<InvoiceNumberAllocator>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
