# Security and Observability

## Authentication and tenant resolution

The HTTP boundary uses environment-specific authentication while keeping one tenant-resolution path:

- **Development:** the `Development` authentication scheme requires `X-Tenant-Id` and creates a local identity. `X-Development-User` optionally supplies the actor name.
- **Non-Development:** the JWT bearer handler validates tokens against `Authentication:Authority` and `Authentication:Audience` over HTTPS.
- After authentication, `TenantResolutionMiddleware` reads the trusted `tenant_id` claim and populates the scoped tenant context.
- Request bodies never supply the authoritative tenant identifier.
- Controllers require authorization; health endpoints remain anonymous for platform probes.

Production configuration must be provided through environment-specific configuration or deployment secrets:

```text
Authentication__Authority=https://login.microsoftonline.com/{directory-id}/v2.0
Authentication__Audience=api://invoice-management
```

An unauthenticated request returns `401`. An authenticated identity without a valid `tenant_id` claim returns `403`. Development headers are not registered as an authentication scheme outside Development.

## Request pipeline

The middleware order is intentional:

1. Central exception handling.
2. Correlation validation and W3C activity enrichment.
3. Authentication.
4. Trusted tenant-claim resolution.
5. Structured request-completion logging.
6. Tenant-partitioned rate limiting.
7. Request timeouts.
8. Authorization and controller execution.

This ensures failures retain trace/correlation context and tenant-owned handlers never execute before the tenant boundary is resolved.

## Correlation and tracing

- ASP.NET Core's `Activity` supplies the W3C `TraceId` and honors incoming `traceparent` headers.
- `X-Correlation-ID` is optional and limited to 64 characters from `[A-Za-z0-9._:-]`.
- If absent, the current W3C trace identifier becomes the correlation identifier.
- `X-Correlation-ID` is returned on responses, including centrally handled failures.
- Structured JSON log scopes contain `TraceId`, `CorrelationId`, `TenantId`, and `UserId` when available.
- Lifecycle history and idempotency records keep the correlation identifier for audit navigation.

Correlation identifies an execution chain; it is not an idempotency key and does not prevent duplicate work.

## ProblemDetails

The centralized exception handler maps expected exceptions to stable HTTP status codes and error codes. Unexpected exceptions are logged but internal details are not returned. Problem responses include:

- `status`, `title`, `detail`, and request `instance` where applicable.
- Stable `errorCode` for client behavior.
- `traceId` for distributed tracing.
- `correlationId` for operational search.

Status-code pages provide consistent bodies for authentication, authorization, and not-found responses. Rate-limit rejections use the same ProblemDetails service.

## Rate limits and timeouts

API controller requests use a fixed-window limiter partitioned by the authenticated `tenant_id` claim. Defaults are 100 requests per 60 seconds with no queue. Limits are configuration-driven:

```json
{
  "Api": {
    "RequestTimeoutSeconds": 30,
    "RateLimit": {
      "PermitLimit": 100,
      "WindowSeconds": 60
    }
  }
}
```

Rate limiting is an application guardrail, not a replacement for gateway or platform protection. Timeout cancellation is propagated through controller and EF Core cancellation tokens.

## Health endpoints

- `GET /health/live` proves the process can serve requests and does not depend on SQL Server.
- `GET /health/ready` verifies SQL Server connectivity for deployment readiness.
- Both endpoints return compact JSON and remain outside tenant authentication, rate limiting, and API request-timeout policies.

Health payloads expose component state and duration only; they do not return connection strings or exception internals.
