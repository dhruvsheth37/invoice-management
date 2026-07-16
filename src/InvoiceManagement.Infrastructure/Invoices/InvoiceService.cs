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

internal sealed partial class InvoiceService(
    InvoiceDbContext dbContext,
    ITenantContext tenantContext,
    InvoiceNumberAllocator invoiceNumberAllocator,
    TimeProvider timeProvider,
    ILogger<InvoiceService> logger) : IInvoiceService
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
                    request.CurrencyCode, request.DueDate, request.Notes, now, context.UserId, context.CorrelationId);

                short lineNumber = 1;
                foreach (var line in request.LineItems)
                {
                    invoice.AddLine(Guid.CreateVersion7(), lineNumber++, line.Description, line.Quantity,
                        line.UnitPrice, line.TaxRate, now, context.UserId);
                }

                dbContext.Invoices.Add(invoice);
                await dbContext.SaveChangesAsync(cancellationToken);
                return Map(invoice);
            },
            cancellationToken);
        LogDraftCreated(logger, result.Id);
        return result;
    }

    public async Task<CursorResult<InvoiceListItemDto>> ListAsync(InvoiceListQuery query, CancellationToken cancellationToken)
    {
        if (query.PageSize is < 1 or > 100)
        {
            throw AppException.Validation("invoice.pagination_invalid", "PageSize must be between 1 and 100.");
        }

        var invoices = dbContext.Invoices.AsNoTracking().AsQueryable();
        if (query.Status is not null) invoices = invoices.Where(x => x.Status == query.Status);
        if (query.CustomerId is not null) invoices = invoices.Where(x => x.CustomerId == query.CustomerId);
        if (query.From is not null) invoices = invoices.Where(x => x.CreatedUtc >= query.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        if (query.To is not null) invoices = invoices.Where(x => x.CreatedUtc < query.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        if (query.DueFrom is not null) invoices = invoices.Where(x => x.DueDate >= query.DueFrom);
        if (query.DueTo is not null) invoices = invoices.Where(x => x.DueDate <= query.DueTo);
        if (!string.IsNullOrWhiteSpace(query.InvoiceNumber)) invoices = invoices.Where(x => x.InvoiceNumber == query.InvoiceNumber.Trim());

        var sort = NormalizeSort(query.Sort);
        int? totalCount = query.IncludeTotalCount
            ? await invoices.CountAsync(cancellationToken)
            : null;
        var cursor = DecodeCursor(query, sort);
        invoices = ApplyCursor(invoices, cursor, sort);
        invoices = ApplyOrdering(invoices, sort);

        var items = await invoices.Take(query.PageSize + 1)
            .Select(x => new InvoiceListItemDto(x.Id, x.InvoiceNumber, x.Status, x.CustomerId, x.CurrencyCode,
                x.Total, x.IssueDate, x.DueDate, x.CreatedUtc))
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > query.PageSize;
        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        var continuationToken = hasMore && items.Count > 0
            ? EncodeCursor(query, sort, items[^1])
            : null;
        return new(items, query.PageSize, totalCount, continuationToken);
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
            var number = await invoiceNumberAllocator.AllocateAsync(checked((short)issueDate.Year), now, cancellationToken);
            var snapshot = new BillToSnapshot(customer.Code, customer.LegalName, location.TaxNumber ?? customer.TaxNumber,
                location.AddressLine1, location.AddressLine2, location.City, location.StateProvince,
                location.PostalCode, location.CountryCode);
            invoice.Issue($"INV-{issueDate.Year}-{number:D6}", snapshot, issueDate, dueDate, now, context.UserId, context.CorrelationId);
        }, cancellationToken);

    public Task<InvoiceDto> MarkPaidAsync(Guid invoiceId, MarkInvoicePaidRequest request, InvoiceOperationContext context, CancellationToken cancellationToken) =>
        ExecuteMutationAsync(invoiceId, "invoice.mark-paid", request, context, (invoice, now) =>
        {
            if (string.IsNullOrWhiteSpace(request.Reference) || request.Reference.Length > 100)
                throw AppException.Validation("invoice.payment_reference_invalid", "Payment reference is required and cannot exceed 100 characters.");
            invoice.MarkPaid(request.PaidDate, request.Reference, now, context.UserId, context.CorrelationId);
            return Task.CompletedTask;
        }, cancellationToken);

    public Task<InvoiceDto> VoidAsync(Guid invoiceId, VoidInvoiceRequest request, InvoiceOperationContext context, CancellationToken cancellationToken) =>
        ExecuteMutationAsync(invoiceId, "invoice.void", request, context, (invoice, now) =>
        {
            if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 500)
                throw AppException.Validation("invoice.void_reason_invalid", "Void reason is required and cannot exceed 500 characters.");
            invoice.Void(request.Reason, now, context.UserId, context.CorrelationId);
            return Task.CompletedTask;
        }, cancellationToken);

    public async Task<InvoiceDashboardDto> GetDashboardAsync(DateOnly asOf, CancellationToken cancellationToken)
    {
        var rows = await dbContext.Invoices
            .AsNoTracking()
            .GroupBy(x => x.CurrencyCode)
            .Select(group => new
            {
                CurrencyCode = group.Key,
                DraftCount = group.Count(x => x.Status == InvoiceStatus.Draft),
                DraftAmount = group.Where(x => x.Status == InvoiceStatus.Draft).Sum(x => (decimal?)x.Total) ?? 0m,
                IssuedCount = group.Count(x => x.Status == InvoiceStatus.Issued),
                IssuedAmount = group.Where(x => x.Status == InvoiceStatus.Issued).Sum(x => (decimal?)x.Total) ?? 0m,
                PaidCount = group.Count(x => x.Status == InvoiceStatus.Paid),
                PaidAmount = group.Where(x => x.Status == InvoiceStatus.Paid).Sum(x => (decimal?)x.Total) ?? 0m,
                VoidCount = group.Count(x => x.Status == InvoiceStatus.Void),
                VoidAmount = group.Where(x => x.Status == InvoiceStatus.Void).Sum(x => (decimal?)x.Total) ?? 0m,
                OverdueCount = group.Count(x => x.Status == InvoiceStatus.Issued && x.DueDate < asOf),
                OverdueAmount = group
                    .Where(x => x.Status == InvoiceStatus.Issued && x.DueDate < asOf)
                    .Sum(x => (decimal?)x.Total) ?? 0m,
            })
            .OrderBy(x => x.CurrencyCode)
            .ToListAsync(cancellationToken);

        var summaries = rows.Select(row => new InvoiceCurrencySummary(
            row.CurrencyCode,
            new(row.DraftCount, row.DraftAmount),
            new(row.IssuedCount, row.IssuedAmount),
            new(row.PaidCount, row.PaidAmount),
            new(row.VoidCount, row.VoidAmount),
            new(row.OverdueCount, row.OverdueAmount))).ToList();

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
        LogCommandCompleted(logger, operation, invoiceId);
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
        var query = dbContext.Invoices
            .Include(x => x.LineItems)
            .Include(x => x.StatusHistory)
            .AsSplitQuery();
        return asTracking ? query : query.AsNoTracking();
    }

    private static string NormalizeSort(string? sort) => sort?.Trim().ToLowerInvariant() switch
    {
        null or "" or "-createdutc" => "-createdutc",
        "createdutc" => "createdutc",
        "duedate" => "duedate",
        "-duedate" => "-duedate",
        "total" => "total",
        "-total" => "-total",
        _ => throw AppException.Validation("invoice.sort_invalid", "The requested sort is not supported."),
    };

    private static IQueryable<Invoice> ApplyOrdering(IQueryable<Invoice> invoices, string sort) => sort switch
    {
        "createdutc" => invoices.OrderBy(x => x.CreatedUtc).ThenBy(x => x.Id),
        "duedate" => invoices.OrderBy(x => x.DueDate).ThenBy(x => x.Id),
        "-duedate" => invoices.OrderByDescending(x => x.DueDate).ThenByDescending(x => x.Id),
        "total" => invoices.OrderBy(x => x.Total).ThenBy(x => x.Id),
        "-total" => invoices.OrderByDescending(x => x.Total).ThenByDescending(x => x.Id),
        _ => invoices.OrderByDescending(x => x.CreatedUtc).ThenByDescending(x => x.Id),
    };

    private static IQueryable<Invoice> ApplyCursor(
        IQueryable<Invoice> invoices,
        InvoiceSearchCursor? cursor,
        string sort)
    {
        if (cursor is null)
        {
            return invoices;
        }

        return sort switch
        {
            "createdutc" => invoices.Where(x =>
                x.CreatedUtc > cursor.CreatedUtc || x.CreatedUtc == cursor.CreatedUtc && x.Id > cursor.Id),
            "-createdutc" => invoices.Where(x =>
                x.CreatedUtc < cursor.CreatedUtc || x.CreatedUtc == cursor.CreatedUtc && x.Id < cursor.Id),
            "duedate" when cursor.DueDate is null => invoices.Where(x =>
                x.DueDate == null && x.Id > cursor.Id || x.DueDate != null),
            "duedate" => invoices.Where(x =>
                x.DueDate > cursor.DueDate || x.DueDate == cursor.DueDate && x.Id > cursor.Id),
            "-duedate" when cursor.DueDate is null => invoices.Where(x =>
                x.DueDate == null && x.Id < cursor.Id),
            "-duedate" => invoices.Where(x =>
                x.DueDate < cursor.DueDate || x.DueDate == cursor.DueDate && x.Id < cursor.Id || x.DueDate == null),
            "total" => invoices.Where(x =>
                x.Total > cursor.Total || x.Total == cursor.Total && x.Id > cursor.Id),
            "-total" => invoices.Where(x =>
                x.Total < cursor.Total || x.Total == cursor.Total && x.Id < cursor.Id),
            _ => invoices,
        };
    }

    private static InvoiceSearchCursor? DecodeCursor(InvoiceListQuery query, string sort)
    {
        if (string.IsNullOrWhiteSpace(query.ContinuationToken))
        {
            return null;
        }

        try
        {
            var token = query.ContinuationToken.Replace('-', '+').Replace('_', '/');
            token = token.PadRight(token.Length + (4 - token.Length % 4) % 4, '=');
            var cursor = JsonSerializer.Deserialize<InvoiceSearchCursor>(Convert.FromBase64String(token), JsonOptions)
                ?? throw new JsonException("Cursor payload is empty.");
            if (!string.Equals(cursor.Sort, sort, StringComparison.Ordinal) ||
                !string.Equals(cursor.Scope, CreateCursorScope(query, sort), StringComparison.Ordinal))
            {
                throw new JsonException("Cursor does not match the current search.");
            }

            return cursor;
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            throw AppException.Validation("invoice.cursor_invalid", "The continuation token is invalid for this search.");
        }
    }

    private static string EncodeCursor(InvoiceListQuery query, string sort, InvoiceListItemDto item)
    {
        var cursor = new InvoiceSearchCursor(
            sort,
            CreateCursorScope(query, sort),
            item.Id,
            item.CreatedUtc,
            item.DueDate,
            item.Total);
        return Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(cursor, JsonOptions))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string CreateCursorScope(InvoiceListQuery query, string sort)
    {
        var scope = JsonSerializer.SerializeToUtf8Bytes(new
        {
            query.Status,
            query.CustomerId,
            query.From,
            query.To,
            query.DueFrom,
            query.DueTo,
            InvoiceNumber = query.InvoiceNumber?.Trim(),
            Sort = sort,
        }, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(scope).AsSpan(0, 12));
    }

    private sealed record InvoiceSearchCursor(
        string Sort,
        string Scope,
        Guid Id,
        DateTime CreatedUtc,
        DateOnly? DueDate,
        decimal Total);

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

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Draft invoice {InvoiceId} created")]
    private static partial void LogDraftCreated(ILogger logger, Guid invoiceId);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Invoice command {Operation} completed for {InvoiceId}")]
    private static partial void LogCommandCompleted(ILogger logger, string operation, Guid invoiceId);

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
