# ADR-007: Use a single active-record flag

- Status: Accepted
- Date: 2026-07-16

## Context

Business data may need to disappear from normal application views without physical deletion. Separate activation and soft-deletion states add ambiguity and require additional deletion-specific audit columns.

## Decision

Use only `IsActive` on Customers, CustomerLocations, Invoices, and InvoiceLineItems. Apply EF Core filters that return active rows. Do not add `IsDeleted`, `DeletedUtc`, or `DeletedBy`. Permit invoice and line deactivation only while the invoice is Draft. Prevent deactivating customers or locations referenced by active Drafts. Use the standard `ModifiedUtc` and `ModifiedBy` fields for the latest mutation audit. Snapshot billing identity during issue so later master-data changes do not alter issued records. Retain issued records and use Void for business cancellation.

## Consequences

- Normal queries hide inactive records.
- Unique indexes for mutable master data account for active rows.
- Every privileged filter bypass must be explicit.
- Reactivation must revalidate uniqueness and aggregate rules if implemented later.
- Temporal history remains independent of current-row visibility.
