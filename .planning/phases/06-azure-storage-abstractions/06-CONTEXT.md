# Phase 6: Azure Storage Abstractions - Context

**Gathered:** 2026-01-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Abstract Azure Storage services (Queue Storage and Blob Storage) into reusable framework patterns. Provide interfaces for testability and contracts while allowing consumers to interact directly with Azure SDK clients. Include helpers for local development setup.

This phase covers Queue and Blob storage only. Table Storage is out of scope.

</domain>

<decisions>
## Implementation Decisions

### Service Coverage
- **Queue Storage and Blob Storage only** — Table Storage deferred to future phase
- **Interface-based abstractions** — IQueueMessage, IBlobContainer for contracts
- **Consumers use Azure SDK directly** — Framework provides interfaces, not wrappers
- **Research Arcoro.One patterns** — Use existing patterns from Arcoro.One or imagile-app as reference

### API Design
- **IQueueMessage interface** — Message contract with Id, Body, PopReceipt
- **IBlobContainer interface** — Container contract with CreateIfNotExists, GetBlob, etc.
- **Convention-based scanning** — Framework scans for IQueueMessage/IBlobContainer implementations and auto-creates queues/containers
- **Centralized initialization method** — Takes StorageAccount connection string, creates all discovered queues and blob containers
- **Additional interfaces** — Claude to research and propose based on Arcoro.One patterns (e.g., IQueueClient, IBlobService)

### Authentication Integration
- **Support both credential and connection string** — AppTokenCredential (from Phase 5) or connection string
- **Credential takes precedence** — If both provided, use AppTokenCredential (identity-based auth preferred)
- **Named client support** — Multiple storage accounts supported via named clients
- **Attribute-driven registration** — `[StorageAccount("account1")]` attribute decorates interfaces to associate with connection string names
- **Framework auto-wires** — Based on attributes, framework resolves correct storage account for each interface

### Error Handling and Resilience
- **Built-in exponential backoff** — Framework provides automatic retry for transient failures
- **Azure SDK exceptions bubble** — No exception wrapping, consumers work with RequestFailedException directly
- **Simple retry for queues** — Don't overcomplicate queue processing, just retry on failure
- **No progress reporting** — Blob upload/download operations complete without progress tracking

### Claude's Discretion
- Exact retry policy configuration (max attempts, backoff timing)
- Interface method signatures and parameters
- How convention-based scanning discovers queue/container names
- DI registration API pattern (fluent vs separate extensions)
- Storage account attribute implementation details

</decisions>

<specifics>
## Specific Ideas

- **Reference:** Use patterns from Arcoro.One repository or imagile-app repository for IQueueMessage and IBlobContainer
- **Local development focus:** Initialization method should make local setup easy (one call creates all queues/containers)
- **Multi-account pattern:** `[StorageAccount("account1")]` attribute similar to `[DbContext]` or similar patterns in EF Core

</specifics>

<deferred>
## Deferred Ideas

- **Table Storage abstractions** — Future phase or backlog
- **Poison message handling / dead-letter queues** — Advanced queue patterns deferred
- **Visibility timeout extension** — Complex queue processing deferred
- **Progress reporting for large blobs** — Keep initial version simple

</deferred>

---

*Phase: 06-azure-storage-abstractions*
*Context gathered: 2026-01-26*
