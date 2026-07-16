using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using AppException = InvoiceManagement.Application.Common.ApplicationException;
using InvoiceManagement.Application.Abstractions.Tenancy;
using InvoiceManagement.Application.Invoices;
using InvoiceManagement.Domain.Customers;
using InvoiceManagement.Domain.Invoices;
using InvoiceManagement.Domain.Platform;
using InvoiceManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvoiceManagement.Infrastructure.Invoices;

internal sealed class InvoiceService(
    InvoiceDbContext dbContext,
    ITenantContext tenantContext,
    TimeProvider timeProvider,
    Microsoft.Extensions.Logging.ILogger<InvoiceService> logger) : IInvoiceService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<InvoiceDto> CreateAsync(
        CreateInvoiceRequest request,
        InvoiceOperationContext context,
        CancellationToken cancellationToken)
    {
        ValidateContext(context, requireEtag: false);
        ValidateCreate(request);

        var result = await ExecuteIdempotentAsync(
            "invoice.create",
            request,
            context,
            201,
            async now =>
            {
                var customer = await dbContext.Customers.SingleOrDefaultAsync(
                    item => item.Id == request.CustomerId,
                    cancellationToken) ?? throw AppException.Validation("invoice.customer_invalid", "Customer is not active or does not exist.");

                var locationExists = await dbContext.CustomerLocations.AnyAsync(
                    item => item.Id == request.CustomerLocationId && item.CustomerId == customer.Id,
                    cancellationToken);
                if (!locationExists)
                {
                    throw AppException.Validation("invoice.location_invalid", "Customer location is not active or does not belong to the customer.");
                }

                var invoice = Invoice.CreateDraft(
                    Guid.CreateVersion7(), tenantContext.TenantId, customer.Id, request.CustomerLocationId,
                    request.CurrencyCode, request.DueDate, request.Notes, now, context.Actor, context.CorrelationId);

                short lineNumber = 1;
                foreach (var line in request.LineItems)
                {
                    invoice.AddLine(Guid.CreateVersion7(), lineNumber++, line.Description, line.Quantity,
                        line.UnitPrice, line.TaxRate, now, context.Actor);
                }

                dbContext.Invoices.Add(invoice);
                await dbContext.SaveChangesAsync(cancellationToken);
                return Map(invoice);
            },
            cancellationToken);
        logger.LogInformation("Draft invoice {InvoiceId} created", result.Id);
        return result;
    }

    public async Task<PagedResult<InvoiceListItemDto>> ListAsync(InvoiceListQuery query, CancellationToken cancellationToken)
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 100)
        {
            throw AppException.Validation("invoice.pagination_invalid", "Page must be positive and pageSize must be between 1 and 100.");
        }

        var invoices = dbContext.Invoices.AsNoTracking().AsQueryable();
        if (query.Status is not null) invoices = invoices.Where(x => x.Status == query.Status);
        if (query.CustomerId is not null) invoices = invoices.Where(x => x.CustomerId == query.CustomerId);
        if (query.From is not null) invoices = invoices.Where(x => x.CreatedUtc >= query.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        if (query.To is not null) invoices = invoices.Where(x => x.CreatedUtc < query.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        if (query.DueFrom is not null) invoices = invoices.Where(x => x.DueDate >= query.DueFrom);
        if (query.DueTo is not null) invoices = invoices.Where(x => x.DueDate <= query.DueTo);
        if (!string.IsNullOrWhiteSpace(query.InvoiceNumber)) invoices = invoices.Where(x => x.InvoiceNumber == query.InvoiceNumber.Trim());

        invoices = query.Sort?.ToLowerInvariant() switch
        {
            "createdutc" => invoices.OrderBy(x => x.CreatedUtc).ThenBy(x => x.Id),
            "duedate" => invoices.OrderBy(x => x.DueDate).ThenBy(x => x.Id),
            "-duedate" => invoices.OrderByDescending(x => x.DueDate).ThenByDescending(x => x.Id),
            "total" => invoices.OrderBy(x => x.Total).ThenBy(x => x.Id),
            "-total" => invoices.OrderByDescending(x => x.Total).ThenByDescending(x => x.Id),
            null or "" or "-createdutc" => invoices.OrderByDescending(x => x.CreatedUtc).ThenByDescending(x => x.Id),
            _ => throw AppException.Validation("invoice.sort_invalid", "The requested sort is not supported."),
        };

        var totalCount = await invoices.CountAsync(cancellationToken);
        var items = await invoices.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new InvoiceListItemDto(x.Id, x.InvoiceNumber, x.Status, x.CustomerId, x.CurrencyCode,
                x.Total, x.IssueDate, x.DueDate, x.CreatedUtc))
            .ToListAsync(cancellationToken);

        return new(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<InvoiceDto?> GetAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        var invoice = await InvoiceQuery(asTracking: false).SingleOrDefaultAsync(x => x.Id == invoiceId, cancellationToken);
        return invoice is null ? null : Map(invoice);
    }

    public Task<InvoiceDto> IssueAsync(Guid invoiceId, IssueInvoiceRequest request, InvoiceOperationContext context, CancellationToken cancellationToken) =>
        ExecuteMutationAsync(invoiceId, "invoice.issue", request, context, async (invoice, now) =>
        {
            var customer = await dbContext.Customers.SingleOrDefaultAsync(x => x.Id == invoice.CustomerId, cancellationToken)
                ?? throw AppException.Validation("invoice.customer_invalid", "Customer is not active.");
            var location = await dbContext.CustomerLocations.SingleOrDefaultAsync(
                x => x.Id == invoice.CustomerLocationId && x.CustomerId == invoice.CustomerId, cancellationToken)
                ?? throw AppException.Validation("invoice.location_invalid", "Customer location is not active.");

            var issueDate = request.IssueDate ?? DateOnly.FromDateTime(now);
            var dueDate = request.DueDate ?? invoice.DueDate ?? issueDate;
            var sequence = await dbContext.InvoiceNumberSequences.SingleOrDefaultAsync(
                x => x.TenantId == tenantContext.TenantId && x.FiscalYear == issueDate.Year, cancellationToken);
            if (sequence is null)
            {
                sequence = InvoiceNumberSequence.Create(tenantContext.TenantId, checked((short)issueDate.Year), now);
                dbContext.InvoiceNumberSequences.Add(sequence);
            }

            var number = sequence.Allocate(now);
            var snapshot = new BillToSnapshot(customer.Code, customer.LegalName, location.TaxNumber ?? customer.TaxNumber,
                location.AddressLine1, location.AddressLine2, location.City, location.StateProvince,
                location.PostalCode, location.CountryCode);
            invoice.Issue($"INV-{issueDate.Year}-{number:D6}", snapshot, issueDate, dueDate, now, context.Actor, context.CorrelationId);
        }, cancellationToken, IsolationLevel.Serializable);

    public Task<InvoiceDto> MarkPaidAsync(Guid invoiceId, MarkInvoicePaidRequest request, InvoiceOperationContext context, CancellationToken cancellationToken) =>
        ExecuteMutationAsync(invoiceId, "invoice.mark-paid", request, context, (invoice, now) =>
        {
            if (string.IsNullOrWhiteSpace(request.Reference) || request.Reference.Length > 100)
                throw AppException.Validation("invoice.payment_reference_invalid", "Payment reference is required and cannot exceed 100 characters.");
            invoice.MarkPaid(request.PaidDate, request.Reference, now, context.Actor, context.CorrelationId);
            return Task.CompletedTask;
        }, cancellationToken);

    public Task<InvoiceDto> VoidAsync(Guid invoiceId, VoidInvoiceRequest request, InvoiceOperationContext context, CancellationToken cancellationToken) =>
        ExecuteMutationAsync(invoiceId, "invoice.void", request, context, (invoice, now) =>
        {
            if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 500)
                throw AppException.Validation("invoice.void_reason_invalid", "Void reason is required and cannot exceed 500 characters.");
            invoice.Void(request.Reason, now, context.Actor, context.CorrelationId);
            return Task.CompletedTask;
        }, cancellationToken);

    public async Task<InvoiceDashboardDto> GetDashboardAsync(DateOnly asOf, CancellationToken cancellationToken)
    {
        var rows = await dbContext.Invoices.AsNoTracking()
            .Select(x => new { x.CurrencyCode, x.Status, x.Total, x.DueDate })
            .ToListAsync(cancellationToken);

        var summaries = rows.GroupBy(x => x.CurrencyCode).OrderBy(x => x.Key).Select(group =>
        {
            InvoiceAmountSummary For(InvoiceStatus status) => new(group.Count(x => x.Status == status), group.Where(x => x.Status == status).Sum(x => x.Total));
            var overdue = group.Where(x => x.Status == InvoiceStatus.Issued && x.DueDate < asOf);
            return new InvoiceCurrencySummary(group.Key, For(InvoiceStatus.Draft), For(InvoiceStatus.Issued),
                For(InvoiceStatus.Paid), For(InvoiceStatus.Void), new(overdue.Count(), overdue.Sum(x => x.Total)));
        }).ToList();

        return new(asOf, summaries);
    }

    private async Task<InvoiceDto> ExecuteMutationAsync<TRequest>(
        Guid invoiceId,
        string operation,
        TRequest request,
        InvoiceOperationContext context,
        Func<Invoice, DateTime, Task> mutate,
        CancellationToken cancellationToken,
        IsolationLevel isolation = IsolationLevel.ReadCommitted)
    {
        ValidateContext(context, requireEtag: true);
        var result = await ExecuteIdempotentAsync(operation, new { invoiceId, request }, context, 200, async now =>
        {
            var invoice = await InvoiceQuery(asTracking: true).SingleOrDefaultAsync(x => x.Id == invoiceId, cancellationToken)
                ?? throw AppException.NotFound();
            EnsureEtag(invoice, context.IfMatch!);
            await mutate(invoice, now);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(invoice);
        }, cancellationToken, isolation);
        logger.LogInformation("Invoice command {Operation} completed for {InvoiceId}", operation, invoiceId);
        return result;
    }

    private async Task<InvoiceDto> ExecuteIdempotentAsync<TRequest>(
        string operation,
        TRequest request,
        InvoiceOperationContext context,
        short status,
        Func<DateTime, Task<InvoiceDto>> action,
        CancellationToken cancellationToken,
        IsolationLevel isolation = IsolationLevel.ReadCommitted)
    {
        var hash = SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(request, JsonOptions));
        await using var transaction = await dbContext.Database.BeginTransactionAsync(isolation, cancellationToken);
        var existing = await dbContext.IdempotencyRequests.SingleOrDefaultAsync(
            x => x.TenantId == tenantContext.TenantId && x.Operation == operation && x.IdempotencyKey == context.IdempotencyKey,
            cancellationToken);

        if (existing is not null)
        {
            if (!existing.RequestHash.SequenceEqual(hash))
                throw AppException.Conflict("idempotency.payload_mismatch", "The idempotency key was already used with a different request.");
            if (existing.State != IdempotencyState.Completed || existing.ResponseBody is null)
                throw AppException.Conflict("idempotency.in_progress", "A request with this idempotency key is already processing.");
            return JsonSerializer.Deserialize<InvoiceDto>(existing.ResponseBody, JsonOptions)
                ?? throw new InvalidOperationException("Stored idempotency response is invalid.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var record = IdempotencyRequest.Start(Guid.CreateVersion7(), tenantContext.TenantId, operation,
            context.IdempotencyKey, hash, context.CorrelationId, now);
        dbContext.IdempotencyRequests.Add(record);

        try
        {
            var result = await action(now);
            record.Complete(result.Id, status, JsonSerializer.Serialize(result, JsonOptions), now);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw AppException.Conflict("invoice.concurrency_conflict", "The invoice changed since it was retrieved.");
        }
        catch (DbUpdateException)
        {
            throw AppException.Conflict("invoice.persistence_conflict", "The operation conflicted with another request or a uniqueness constraint.");
        }
    }

    private IQueryable<Invoice> InvoiceQuery(bool asTracking)
    {
        var query = dbContext.Invoices.Include(x => x.LineItems).Include(x => x.StatusHistory).AsQueryable();
        return asTracking ? query : query.AsNoTracking();
    }

    private static void ValidateCreate(CreateInvoiceRequest request)
    {
        if (request.LineItems is null || request.LineItems.Count is < 1 or > 100)
            throw AppException.Validation("invoice.lines_invalid", "Between 1 and 100 line items are required.");
        if (string.IsNullOrWhiteSpace(request.CurrencyCode) || request.CurrencyCode.Trim().Length != 3)
            throw AppException.Validation("invoice.currency_invalid", "Currency code must contain three characters.");
        if (request.Notes?.Length > 1000) throw AppException.Validation("invoice.notes_invalid", "Notes cannot exceed 1000 characters.");
        if (request.LineItems.Any(x => string.IsNullOrWhiteSpace(x.Description) || x.Description.Length > 500 || x.Quantity <= 0 || x.UnitPrice < 0 || x.TaxRate is < 0 or > 1))
            throw AppException.Validation("invoice.line_invalid", "One or more invoice lines are invalid.");
    }

    private static void ValidateContext(InvoiceOperationContext context, bool requireEtag)
    {
        if (string.IsNullOrWhiteSpace(context.IdempotencyKey) || context.IdempotencyKey.Length > 100)
            throw AppException.Validation("idempotency.key_invalid", "Idempotency-Key is required and cannot exceed 100 characters.");
        if (string.IsNullOrWhiteSpace(context.CorrelationId) || context.CorrelationId.Length > 64)
            throw AppException.Validation("correlation.invalid", "Correlation identifier is required and cannot exceed 64 characters.");
        if (requireEtag && string.IsNullOrWhiteSpace(context.IfMatch))
            throw AppException.Precondition("If-Match is required for lifecycle operations.");
    }

    private static void EnsureEtag(Invoice invoice, string ifMatch)
    {
        var supplied = ifMatch.Trim().Trim('"');
        if (!string.Equals(supplied, Convert.ToBase64String(invoice.RowVersion), StringComparison.Ordinal))
            throw AppException.Conflict("invoice.concurrency_conflict", "The invoice changed since it was retrieved.");
    }

    private static InvoiceDto Map(Invoice invoice) => new(
        invoice.Id, invoice.CustomerId, invoice.CustomerLocationId, invoice.InvoiceNumber, invoice.Status,
        invoice.CurrencyCode, invoice.IssueDate, invoice.DueDate, invoice.PaidDate, invoice.PaymentReference,
        invoice.Subtotal, invoice.TaxTotal, invoice.Total, invoice.Notes, invoice.VoidReason,
        invoice.BillToCustomerCode, invoice.BillToLegalName, invoice.BillToTaxNumber,
        invoice.BillToAddressLine1, invoice.BillToAddressLine2, invoice.BillToCity,
        invoice.BillToStateProvince, invoice.BillToPostalCode, invoice.BillToCountryCode,
        invoice.CreatedUtc, invoice.ModifiedUtc, $"\"{Convert.ToBase64String(invoice.RowVersion)}\"",
        invoice.LineItems.Where(x => x.IsActive).OrderBy(x => x.LineNumber)
            .Select(x => new InvoiceLineDto(x.Id, x.LineNumber, x.Description, x.Quantity, x.UnitPrice,
                x.TaxRate, x.NetAmount, x.TaxAmount, x.TotalAmount)).ToList(),
        invoice.StatusHistory.OrderBy(x => x.ChangedUtc)
            .Select(x => new InvoiceStatusHistoryDto(x.FromStatus, x.ToStatus, x.Reason,
                x.ChangedUtc, x.ChangedBy, x.CorrelationId)).ToList());
}
