# Correlation and Trace Propagation

```mermaid
sequenceDiagram
    autonumber
    actor Client
    participant API as Invoice API
    participant Activity as .NET Activity / OpenTelemetry
    participant Logs as Structured logs
    participant DB as SQL Server
    participant Future as Future service or message broker

    Client->>API: Request with optional traceparent<br/>and X-Correlation-ID
    API->>API: Validate or generate correlation ID
    API->>Activity: Continue or create W3C trace
    Activity-->>API: TraceId and SpanId
    API->>Logs: Begin scope with trace, correlation,<br/>tenant, user, and resource IDs
    API->>DB: Execute tenant-scoped operation
    DB-->>API: Result and command duration
    API->>Logs: Development EF command event<br/>SQL, duration, masked parameters
    API->>DB: Persist correlation on status/idempotency audit
    API-->>Client: Response with X-Correlation-ID
    API-.->Future: Future propagation of traceparent,<br/>correlation, tenant, event, and causation metadata
```

`CorrelationId` is a searchable operation label. W3C `TraceId` represents distributed tracing. `IdempotencyKey` protects command execution. All three have different responsibilities.
