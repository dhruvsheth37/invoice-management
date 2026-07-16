using InvoiceManagement.Application.Abstractions.Persistence;
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

        services.AddDbContext<InvoiceDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(typeof(InvoiceDbContext).Assembly.FullName)));

        services.AddScoped<IInvoiceDbContext>(provider => provider.GetRequiredService<InvoiceDbContext>());
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
