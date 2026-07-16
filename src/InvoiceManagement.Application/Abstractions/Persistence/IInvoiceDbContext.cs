using InvoiceManagement.Domain.Customers;
using InvoiceManagement.Domain.Invoices;

namespace InvoiceManagement.Application.Abstractions.Persistence;

public interface IInvoiceDbContext
{
    IQueryable<Customer> Customers { get; }

    IQueryable<CustomerLocation> CustomerLocations { get; }

    IQueryable<Invoice> Invoices { get; }

    void Add(Invoice invoice);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
