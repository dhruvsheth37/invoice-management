# Rollback Guidance

The default production strategy is application rollback plus database roll-forward:

1. Stop rollout and keep the last healthy application image available.
2. Determine whether the applied migration is backward compatible.
3. Roll the application back only when the previous version can safely use the current schema.
4. Prefer a reviewed forward-fix migration for production database corrections.
5. Restore from a tested point-in-time backup only for destructive or unrecoverable incidents.

Automatic destructive down scripts are intentionally not supplied. Any exceptional rollback script must be reviewed, backed up, rehearsed, and tied to a specific migration and runbook.
