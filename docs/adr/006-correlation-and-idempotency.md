# ADR-006: Separate tracing, correlation, and idempotency

- Status: Proposed
- Date: 2026-07-16

## Context

Trace IDs, correlation IDs, and idempotency keys are often conflated even though they solve different operational problems.

## Decision

Use W3C trace context through .NET `Activity` for distributed tracing, `X-Correlation-ID` for a validated human-searchable operation identifier, and `Idempotency-Key` for command retry protection. Propagate trace and correlation metadata to future service calls or messages.

## Consequences

- Logs and future services can reconstruct an operation path.
- Repeated commands are protected independently of trace retries.
- Correlation is stored only where it adds audit value.
- Middleware and idempotency storage require separate tests.
