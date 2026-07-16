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

        var suppliedUserId = Request.Headers["X-Development-User-Id"].FirstOrDefault() ?? "1";
        if (!int.TryParse(suppliedUserId, out var userId) || userId <= 0)
        {
            return Task.FromResult(AuthenticateResult.Fail("X-Development-User-Id must be a positive integer."));
        }
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, $"development-user-{userId}"),
            new Claim("user_id", userId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim("tenant_id", tenantId.ToString()),
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
