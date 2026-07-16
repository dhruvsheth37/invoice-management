# C4 Context

```mermaid
flowchart LR
    User["SaaS customer user"]
    IdP["Identity provider<br/>JWT issuer"]
    System["Invoice Management API<br/>Multi-tenant modular monolith"]
    ExternalLedger["External accounting or payment system<br/>Future integration"]

    User -->|"Manage invoices over HTTPS"| System
    IdP -->|"Identity and trusted tenant claims"| System
    System -.->|"Future paid confirmation or events"| ExternalLedger
```

The assessment implements only the API system boundary. The external accounting/payment integration explains the source of a future paid confirmation; no distributed integration is built now.
