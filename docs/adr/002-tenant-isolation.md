# ADR-002: Enforce layered tenant isolation

- Status: Proposed
- Date: 2026-07-16

## Context

A query filter alone can be bypassed, and application-only foreign-key validation cannot prevent malformed cross-tenant data.

## Decision

Resolve tenant context from trusted authentication claims in production, apply EF Core tenant/active-record filters, and use tenant-leading alternate keys and composite foreign keys in SQL Server. Allow `X-Tenant-Id` only in Development and tests.

## Consequences

- Cross-tenant reads and relationships are prevented at several layers.
- Entity configuration is more explicit and composite indexes are wider.
- Administrative filter bypasses must be isolated and audited.
- Integration tests must verify both data invisibility and database rejection.
