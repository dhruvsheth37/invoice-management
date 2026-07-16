# Performance Design

Performance changes are driven by query shape and contention before micro-optimizations.

## Implemented

- Invoice search uses keyset pagination with opaque continuation tokens for every supported sort. Tokens are bound to the active filters and sort.
- Exact total counts are opt-in through `includeTotalCount`; the normal search path performs only the page query.
- Dashboard counts and amounts are grouped and aggregated by SQL Server. The API materializes one row per currency rather than every invoice.
- Invoice details use EF Core split queries for line items and status history, avoiding collection cartesian expansion.
- Invoice numbers use an atomic row/range-locked allocation command inside the existing transaction. The full issue workflow remains at `ReadCommitted` isolation.
- EF contexts are pooled. Tenant identity is assigned to every scoped pooled lease rather than captured by the pooled context constructor.
- Request log events use source generation, routine completions are `Debug`, health probes are excluded, and structured scopes use cached delegates.
- Covering indexes support created-date, due-date, and total keyset sorts plus currency/status dashboard aggregation.

## Deliberately deferred

Compiled EF queries are not enabled without profiling evidence. They add rigid query variants and maintenance cost while providing less benefit than reducing transferred rows, avoiding offset scans, and improving indexes. Production traces and database query statistics should identify stable, frequently executed query shapes before introducing `EF.CompileAsyncQuery`.

## Operational verification

Before changing or removing indexes in production, inspect actual execution plans and Query Store data for representative tenant sizes. Indexes improve reads at the cost of storage and additional work on invoice writes, so unused indexes should not be retained speculatively.
