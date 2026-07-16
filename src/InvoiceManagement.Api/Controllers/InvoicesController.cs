using InvoiceManagement.Application.Invoices;
using InvoiceManagement.Api.Tenancy;
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

    [HttpPost("search")]
    [ProducesResponseType<CursorResult<InvoiceListItemDto>>(StatusCodes.Status200OK)]
    public Task<CursorResult<InvoiceListItemDto>> Search(
        [FromBody] InvoiceListQuery request,
        CancellationToken cancellationToken = default) =>
        service.ListAsync(request, cancellationToken);

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
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? HttpContext.TraceIdentifier;
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault() ?? string.Empty;
        var ifMatch = requireEtag ? Request.Headers["If-Match"].FirstOrDefault() : null;
        var userClaim = User.FindFirst("user_id")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userClaim, out var userId) || userId <= 0)
            throw new TenantAccessException("The authenticated identity does not contain a valid integer user_id claim.");
        return new(userId, correlationId, idempotencyKey, ifMatch);
    }
}
