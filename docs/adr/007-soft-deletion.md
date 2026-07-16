# ADR-007: Restrict soft deletion

- Status: Proposed
- Date: 2026-07-16

## Context

Business data may need to disappear from normal application views without physical deletion. Financial records, however, require retention and must not be silently removed after issue.

## Decision

Add `IsDeleted`, `DeletedUtc`, and `DeletedBy` to Customers, CustomerLocations, Invoices, and InvoiceLineItems. Apply EF Core filters. Permit invoice and line deletion only while the invoice is Draft. Prevent deleting customers or locations referenced by active Drafts. Snapshot billing identity during issue so later master-data changes do not alter issued records. Do not expose deletion in the assessment API. Retain issued records and use Void for business cancellation.

## Consequences

- Normal queries remain simple and hide retired records.
- Unique indexes for mutable master data must account for active rows.
- Every privileged filter bypass must be explicit.
- Restoration must revalidate uniqueness and aggregate rules if implemented later.
- Temporal history remains independent of current-row visibility.
