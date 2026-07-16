using InvoiceManagement.Api.Tenancy;
using InvoiceManagement.Api.Health;
using InvoiceManagement.Application.Abstractions.Tenancy;
using InvoiceManagement.Infrastructure;
using InvoiceManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<RequestTenantContext>();
builder.Services.AddScoped<ITenantContext>(provider => provider.GetRequiredService<RequestTenantContext>());
builder.Services.AddScoped<IMutableTenantContext>(provider => provider.GetRequiredService<RequestTenantContext>());
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
});

app.Run();

public partial class Program;
