# ADR-003: Use EF migrations as schema authority

- Status: Accepted
- Date: 2026-07-16

## Context

The solution requires EF Core and a migration or SQL script. Treating independent hand-written DDL and EF migrations as equal authorities invites schema drift.

## Decision

Commit EF Core migrations as the schema authority. Store versioned and idempotent deployment scripts generated from those migrations, plus independent verification queries, under `database/`.

## Consequences

- Entity mappings, migrations, and deployment artifacts remain traceable.
- SQL can be reviewed without running the application.
- Generated scripts must be refreshed and verified when migrations change.
- A future DBA-controlled SSDT, Flyway, or DbUp workflow would require a new decision and transition plan.
