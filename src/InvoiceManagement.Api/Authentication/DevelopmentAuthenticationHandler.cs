using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace InvoiceManagement.Api.Authentication;

public sealed class DevelopmentAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Development";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Tenant-Id", out var value) || !Guid.TryParse(value, out var tenantId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var actor = Request.Headers["X-Development-User"].FirstOrDefault() ?? "development-user";
        if (actor.Length is < 1 or > 200)
        {
            return Task.FromResult(AuthenticateResult.Fail("X-Development-User must contain 1-200 characters."));
        }
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, actor),
            new Claim(ClaimTypes.Name, actor),
            new Claim("tenant_id", tenantId.ToString()),
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
