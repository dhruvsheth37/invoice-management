# ADR-004: Use temporal tables selectively

- Status: Proposed
- Date: 2026-07-16

## Context

Invoice financial changes benefit from row-version history, but enabling temporal behavior everywhere increases storage, migration, retention, and operational complexity.

## Decision

Enable SQL Server system-versioned temporal behavior for `Invoices` and `InvoiceLineItems` only. Keep `InvoiceStatusHistory` as an explicit append-only business audit log.

## Consequences

- Previous invoice and line values can be reconstructed.
- Status history retains actor, reason, and correlation semantics absent from temporal rows.
- Temporal history retention requires a production policy.
- Temporal tables for other entities remain a future enhancement, not a default.
