namespace InvoiceManagement.Application.Abstractions.Tenancy;

public sealed class TenantIsolationException(string message) : Exception(message);
