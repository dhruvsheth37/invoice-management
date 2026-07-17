# AI Usage

This project was developed with AI assistance. I used AI as an engineering accelerator and review partner, while retaining responsibility for architecture decisions, security, correctness, testing, and the final submitted code.

## 1. AI tools used

- **ChatGPT** was used during early architecture discussions and to explore possible solution boundaries, data modelling choices, API design, and delivery phases.
- **OpenAI Codex** was used to inspect and modify the repository, draft implementation and documentation, generate tests and database scripts, diagnose local-development issues, and review performance and architecture concerns.
- A **GitHub integration** was considered for pull-request operations. Where the integration was unavailable or unsuitable, I reviewed and ran the Git commands manually.

## 2. What I used AI for

I used AI to assist with:

- translating the assessment into phased architecture and implementation work;
- drafting the modular-monolith solution structure and dependency rules;
- generating initial domain entities, application contracts, API endpoints, EF Core mappings, migrations, database scripts, and tests;
- documenting the API, database, architecture, security, observability, performance, and local execution workflow;
- creating Docker and Podman local SQL Server workflows and comprehensive sample data;
- diagnosing EF migration, SQL Server session-option, container, and connection-string issues;
- evaluating API decisions, including body-based search, separate invoice lifecycle endpoints, optimistic concurrency, idempotency, and correlation identifiers;
- identifying performance improvements such as SQL-side dashboard aggregation, split queries, keyset pagination, optional total counts, pooled EF contexts, atomic invoice-number allocation, source-generated logging, and supporting indexes;
- creating architecture tests that protect project dependencies and prevent Domain entities or Infrastructure types from leaking into controllers.

AI-generated output was treated as a draft or proposed change. It was not accepted solely because it compiled or appeared plausible.

## 3. What I personally reviewed

I personally reviewed:

- the technical-assessment requirements and the scope of each delivery phase;
- the four-project architecture and all project-reference directions;
- the tenant-isolation model, tenant-leading keys, query filters, and cross-tenant relationship constraints;
- invoice calculations, lifecycle transitions, numbering, idempotency, and concurrency behavior;
- controller contracts, request and response DTOs, status codes, routes, and error handling;
- every database table, relationship, constraint, index, temporal-table decision, migration, and seed-data script;
- the decision to use `IsActive` without a second soft-delete mechanism;
- the decision to store audit user identifiers as required integers with development default user ID `1`;
- authentication, claim handling, rate limiting, request timeouts, structured logging, health checks, and secret-management expectations;
- build output, analyzer warnings, automated tests, architecture-test rules, and local SQL Server behavior;
- Git diffs and pull-request boundaries before changes were merged.

I also verified local database connectivity independently with a database client and used the generated HTTP examples to inspect the intended API workflow.

## 4. What AI got wrong or initially missed

AI assistance was useful, but several initial suggestions required correction:

- It initially proposed both `IsActive` and `IsDeleted`, together with `DeletedUtc` and `DeletedBy`. This duplicated lifecycle concepts, so I simplified the design to `IsActive` only.
- Initial audit-user fields were represented as strings. I changed `CreatedBy`, `ModifiedBy`, and status-history `ChangedBy` to integer user IDs; created and modified user IDs are required and default to `1` for local/demo use.
- The first temporal-table migration attempted a history-table update in a batch that SQL Server compiled while system versioning was still enabled. This produced SQL error 13561 and was corrected by executing the history update dynamically after disabling versioning.
- The EF CLI setup initially omitted `Microsoft.EntityFrameworkCore.Design` from the startup project, which prevented `dotnet ef database update` from running. The required design-time dependency was added.
- Early Podman instructions assumed the Compose service was already running and did not sufficiently distinguish a Docker-backed Compose provider from the Podman runtime. Dedicated Podman scripts and configurable ports were added.
- The initial seed execution did not account for the SQL session settings required by filtered indexes. The seed script was corrected to set options such as `QUOTED_IDENTIFIER` appropriately.
- An early architecture-test check did not correctly recognize generic `ProducesResponseType<T>` metadata. The reflection rule was corrected and revalidated.
- Some performance suggestions were initially generic. They were only implemented after being checked against actual query shapes and supported with matching indexes.

The execution environment used by AI could not exercise every container or socket-dependent test path. I therefore did not treat tool-environment limitations as proof that the application worked locally; those paths required separate local verification.

## 5. What I wrote, corrected, or significantly changed myself

I made or finalized the significant engineering decisions, including:

- choosing a four-project modular monolith instead of prematurely creating many services and projects;
- defining the invoice lifecycle as `Draft -> Issued -> Paid` with valid void transitions and terminal Paid/Void states;
- retaining separate issue, mark-paid, and void endpoints because they have different commands, authorization opportunities, validations, audit meanings, and side effects;
- moving invoice search to a POST body and adopting cursor pagination for scalable filtering;
- requiring tenant context, correlation propagation, idempotency keys, ETags, and `If-Match` for protected mutations;
- limiting temporal history to invoices and invoice line items rather than enabling temporal tables everywhere;
- simplifying soft deletion to the single `IsActive` field;
- changing audit identity fields to integer user IDs;
- selecting and prioritizing the implemented performance changes and the indexes that support them;
- defining which scale features belong in future enhancements rather than the assessment implementation;
- reviewing and correcting generated code, SQL, tests, scripts, and documentation until they matched the agreed design.

AI drafted a substantial portion of the implementation and documentation under my direction. My contribution was not limited to accepting generated text: I set the constraints, challenged design choices, corrected errors, reviewed the resulting diffs and behavior, and retained accountability for the final solution.

