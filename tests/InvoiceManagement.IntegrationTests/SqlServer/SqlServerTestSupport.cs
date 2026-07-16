using InvoiceManagement.Domain.Customers;
using InvoiceManagement.Domain.Tenants;
using InvoiceManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace InvoiceManagement.IntegrationTests.SqlServer;

[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(SqlServerFixture.ConnectionVariable)))
        {
            Skip = $"Set {SqlServerFixture.ConnectionVariable} to run SQL Server integration tests.";
        }
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SqlServerTestGroup : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SQL Server integration";
}

public sealed class SqlServerFixture : IAsyncLifetime
{
    public const string ConnectionVariable = "INVOICE_TEST_SQLSERVER";

    private readonly string? _serverConnectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
    private readonly string _databaseName = $"InvoiceManagementTests_{Guid.NewGuid():N}";

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(_serverConnectionString))
        {
            return;
        }

        var serverBuilder = new SqlConnectionStringBuilder(_serverConnectionString)
        {
            InitialCatalog = "master",
            TrustServerCertificate = true,
        };
        var databaseBuilder = new SqlConnectionStringBuilder(serverBuilder.ConnectionString)
        {
            InitialCatalog = _databaseName,
        };
        ConnectionString = databaseBuilder.ConnectionString;

        await using (var connection = new SqlConnection(serverBuilder.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE [{_databaseName}]";
            await command.ExecuteNonQueryAsync();
        }

        await using var context = CreateContext(Guid.Empty);
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (string.IsNullOrWhiteSpace(_serverConnectionString) || string.IsNullOrWhiteSpace(ConnectionString))
        {
            return;
        }

        SqlConnection.ClearAllPools();
        var serverBuilder = new SqlConnectionStringBuilder(_serverConnectionString)
        {
            InitialCatalog = "master",
            TrustServerCertificate = true,
        };
        await using var connection = new SqlConnection(serverBuilder.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]";
        await command.ExecuteNonQueryAsync();
    }

    public InvoiceDbContext CreateContext(Guid tenantId)
    {
        var options = new DbContextOptionsBuilder<InvoiceDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new InvoiceDbContext(options, new TestTenantContext(tenantId));
    }

    public WebApplicationFactory<Program> CreateApi() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:InvoiceDatabase"] = ConnectionString,
                    ["Api:RateLimit:PermitLimit"] = "1000",
                }));
        });

    public async Task<TenantData> SeedTenantAsync(string discriminator)
    {
        var tenantId = Guid.CreateVersion7();
        var customerId = Guid.CreateVersion7();
        var locationId = Guid.CreateVersion7();
        var now = DateTime.UtcNow;
        await using var context = CreateContext(tenantId);
        context.Tenants.Add(Tenant.Create(tenantId, $"tenant-{discriminator}", $"Tenant {discriminator}", now, 1));
        context.Customers.Add(Customer.Create(customerId, tenantId, $"C-{discriminator}", $"Customer {discriminator}",
            $"TAX-{discriminator}", $"{discriminator}@example.test", now, 1));
        context.CustomerLocations.Add(CustomerLocation.Create(
            locationId,
            tenantId,
            customerId,
            "Head office",
            new PostalAddress("1 Main Street", null, "Mumbai", "Maharashtra", "400001", "IN"),
            null,
            now,
            1));
        await context.SaveChangesAsync();
        return new TenantData(tenantId, customerId, locationId);
    }

    private sealed class TestTenantContext(Guid tenantId) : InvoiceManagement.Application.Abstractions.Tenancy.ITenantContext
    {
        public Guid TenantId { get; } = tenantId;

        public bool IsResolved => true;
    }
}

public sealed record TenantData(Guid TenantId, Guid CustomerId, Guid LocationId);
