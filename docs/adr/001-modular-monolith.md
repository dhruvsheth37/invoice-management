# ADR-001: Use a modular monolith

- Status: Accepted
- Date: 2026-07-16

## Context

The assessment requires a small invoice API within approximately 5-6 hours. Microservices would introduce deployment, messaging, distributed tracing, failure-handling, and transaction complexity unrelated to the core evaluation.

## Decision

Build one deployable ASP.NET Core API with logical Customer, Invoice, Dashboard, and Platform modules across four source projects.

## Consequences

- Local development, transactions, tests, and deployment remain simple.
- Module boundaries and GUID identifiers preserve an extraction path.
- Independent scaling and deployment are unavailable until a module is extracted.
- Redis, Service Bus, Azure Functions, and AKS are not assessment dependencies.
