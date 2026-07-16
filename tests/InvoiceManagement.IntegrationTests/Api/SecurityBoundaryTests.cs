using System.Security.Claims;
using InvoiceManagement.Api.Observability;
using InvoiceManagement.Api.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvoiceManagement.IntegrationTests.Api;

public sealed class SecurityBoundaryTests
{
    [Fact]
    public async Task Correlation_middleware_accepts_and_exposes_valid_identifier()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationMiddleware.HeaderName] = "portal-request_123";
        var called = false;
        var middleware = new CorrelationMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, NullLogger<CorrelationMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.True(called);
        Assert.Equal("portal-request_123", context.Items["CorrelationId"]);
    }

    [Fact]
    public async Task Correlation_middleware_rejects_invalid_identifier()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationMiddleware.HeaderName] = "invalid correlation value";
        var middleware = new CorrelationMiddleware(_ => Task.CompletedTask, NullLogger<CorrelationMiddleware>.Instance);

        await Assert.ThrowsAsync<InvalidCorrelationException>(() => middleware.InvokeAsync(context));
        Assert.NotEqual("invalid correlation value", context.Items["CorrelationId"]);
    }

    [Fact]
    public async Task Tenant_middleware_resolves_authenticated_tenant_claim()
    {
        var tenantId = Guid.NewGuid();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "user-1"), new Claim("tenant_id", tenantId.ToString())],
                "test")),
        };
        var tenantContext = new RequestTenantContext();
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask, NullLogger<TenantResolutionMiddleware>.Instance);

        await middleware.InvokeAsync(context, tenantContext);

        Assert.True(tenantContext.IsResolved);
        Assert.Equal(tenantId, tenantContext.TenantId);
    }

    [Fact]
    public async Task Tenant_middleware_rejects_authenticated_identity_without_tenant_claim()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-1")], "test")),
        };
        var middleware = new TenantResolutionMiddleware(_ => Task.CompletedTask, NullLogger<TenantResolutionMiddleware>.Instance);

        await Assert.ThrowsAsync<TenantAccessException>(() => middleware.InvokeAsync(context, new RequestTenantContext()));
    }
}
