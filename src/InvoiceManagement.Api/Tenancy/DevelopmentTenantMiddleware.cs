using InvoiceManagement.Application.Abstractions.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceManagement.Api.Tenancy;

public sealed class DevelopmentTenantMiddleware(RequestDelegate next, IWebHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context, IMutableTenantContext tenantContext)
    {
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await next(context);
            return;
        }

        if (!environment.IsDevelopment())
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Production tenant identity requires authentication.",
            });
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Tenant-Id", out var value) || !Guid.TryParse(value, out var tenantId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "A valid X-Tenant-Id header is required.",
            });
            return;
        }

        tenantContext.SetTenant(tenantId);
        await next(context);
    }
}
