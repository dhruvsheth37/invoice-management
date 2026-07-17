# ADR-005: Expose business lifecycle operations

- Status: Accepted
- Date: 2026-07-16

## Context

An unrestricted status patch permits impossible or unaudited financial transitions.

## Decision

Expose `issue`, `mark-paid`, and `void` commands. Enforce the Draft -> Issued -> Paid path and Draft/Issued -> Void transitions in the domain. Treat Overdue as derived and defer PartiallyPaid until payments are modeled.

## Consequences

- API intent, validation, authorization, and audit records are explicit.
- Adding a lifecycle operation requires a new command contract.
- Clients cannot arbitrarily set status.
- The minimum assessment status-update requirement is still satisfied.
