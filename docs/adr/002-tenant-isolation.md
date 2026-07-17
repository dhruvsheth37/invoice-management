# ADR-002: Enforce layered tenant isolation

- Status: Accepted
- Date: 2026-07-16

## Context

A query filter alone can be bypassed, and application-only foreign-key validation cannot prevent malformed cross-tenant data.

## Decision

Resolve tenant context from trusted authentication claims in production. Apply independently named EF Core `TenantFilter` and `ActiveFilter` filters through tenant/activation marker interfaces, and reject cross-tenant tracked writes before saving. Use tenant-leading alternate keys and composite foreign keys in SQL Server. Allow `X-Tenant-Id` only in Development and tests.

## Consequences

- Cross-tenant reads and relationships are prevented at several layers.
- Entity configuration is more explicit and composite indexes are wider.
- Administrative filter bypasses must be isolated and audited.
- Inactive-record queries can disable `ActiveFilter` without disabling `TenantFilter`.
- Integration tests must verify both data invisibility and database rejection.
