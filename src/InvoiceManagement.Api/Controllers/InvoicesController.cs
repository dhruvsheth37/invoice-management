using InvoiceManagement.Application.Invoices;
using InvoiceManagement.Domain.Invoices;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceManagement.Api.Controllers;

[ApiController]
[Route("api/v1/invoices")]
public sealed class InvoicesController(IInvoiceService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<InvoiceDto>> Create(CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, OperationContext(requireEtag: false), cancellationToken);
        Response.Headers.ETag = result.ETag;
        return CreatedAtAction(nameof(Get), new { invoiceId = result.Id }, result);
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<InvoiceListItemDto>>(StatusCodes.Status200OK)]
    public Task<PagedResult<InvoiceListItemDto>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] InvoiceStatus? status = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] DateOnly? dueFrom = null,
        [FromQuery] DateOnly? dueTo = null,
        [FromQuery] string? invoiceNumber = null,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default) =>
        service.ListAsync(new(page, pageSize, status, customerId, from, to, dueFrom, dueTo, invoiceNumber, sort), cancellationToken);

    [HttpGet("{invoiceId:guid}")]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceDto>> Get(Guid invoiceId, CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(invoiceId, cancellationToken);
        if (result is null) return NotFound();
        Response.Headers.ETag = result.ETag;
        return result;
    }

    [HttpPost("{invoiceId:guid}/issue")]
    public Task<ActionResult<InvoiceDto>> Issue(Guid invoiceId, IssueInvoiceRequest? request, CancellationToken cancellationToken) =>
        Mutate(() => service.IssueAsync(invoiceId, request ?? new(null, null), OperationContext(true), cancellationToken));

    [HttpPost("{invoiceId:guid}/mark-paid")]
    public Task<ActionResult<InvoiceDto>> MarkPaid(Guid invoiceId, MarkInvoicePaidRequest request, CancellationToken cancellationToken) =>
        Mutate(() => service.MarkPaidAsync(invoiceId, request, OperationContext(true), cancellationToken));

    [HttpPost("{invoiceId:guid}/void")]
    public Task<ActionResult<InvoiceDto>> Void(Guid invoiceId, VoidInvoiceRequest request, CancellationToken cancellationToken) =>
        Mutate(() => service.VoidAsync(invoiceId, request, OperationContext(true), cancellationToken));

    private async Task<ActionResult<InvoiceDto>> Mutate(Func<Task<InvoiceDto>> action)
    {
        var result = await action();
        Response.Headers.ETag = result.ETag;
        return result;
    }

    private InvoiceOperationContext OperationContext(bool requireEtag)
    {
        var correlationId = Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? HttpContext.TraceIdentifier;
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault() ?? string.Empty;
        var ifMatch = requireEtag ? Request.Headers["If-Match"].FirstOrDefault() : null;
        var actor = User.Identity?.Name ?? "development-user";
        return new(actor, correlationId, idempotencyKey, ifMatch);
    }
}
