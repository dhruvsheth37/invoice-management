using InvoiceManagement.Application.Abstractions.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InvoiceManagement.Infrastructure.Persistence;

public sealed class InvoiceDbContextFactory : IDesignTimeDbContextFactory<InvoiceDbContext>
{
    public InvoiceDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("INVOICE_DATABASE_CONNECTION") ??
            "Server=localhost,1433;Database=InvoiceManagement;User Id=sa;TrustServerCertificate=True;Encrypt=True";

        var options = new DbContextOptionsBuilder<InvoiceDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(InvoiceDbContext).Assembly.FullName))
            .Options;

        return new InvoiceDbContext(options, new DesignTimeTenantContext());
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid TenantId => Guid.Empty;

        public bool IsResolved => false;
    }
}
