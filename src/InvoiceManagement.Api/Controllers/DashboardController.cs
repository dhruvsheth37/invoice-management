using InvoiceManagement.Application.Invoices;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceManagement.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
public sealed class DashboardController(IInvoiceService service, TimeProvider timeProvider) : ControllerBase
{
    [HttpGet("invoice-summary")]
    [ProducesResponseType<InvoiceDashboardDto>(StatusCodes.Status200OK)]
    public Task<InvoiceDashboardDto> Summary([FromQuery] DateOnly? asOf, CancellationToken cancellationToken) =>
        service.GetDashboardAsync(asOf ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime), cancellationToken);
}
