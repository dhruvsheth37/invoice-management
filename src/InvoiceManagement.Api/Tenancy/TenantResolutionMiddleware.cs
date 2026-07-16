using InvoiceManagement.Application.Abstractions.Tenancy;

namespace InvoiceManagement.Api.Tenancy;

public sealed class TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
{
    private static readonly Func<ILogger, Guid, string?, IDisposable?> TenantScope =
        LoggerMessage.DefineScope<Guid, string?>("TenantId:{TenantId} UserId:{UserId}");

    public async Task InvokeAsync(HttpContext context, IMutableTenantContext tenantContext)
    {
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var tenantClaim = context.User.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantClaim, out var tenantId) || tenantId == Guid.Empty)
        {
            throw new TenantAccessException("The authenticated identity does not contain a valid tenant_id claim.");
        }

        tenantContext.SetTenant(tenantId);
        context.Items["TenantId"] = tenantId;
        using var scope = TenantScope(
            logger,
            tenantId,
            context.User.FindFirst("user_id")?.Value
                ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
        await next(context);
    }
}

public sealed class TenantAccessException(string message) : Exception(message);
