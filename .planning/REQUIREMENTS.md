# Requirements: Imagile Framework

**Defined:** 2026-01-25
**Core Value:** Enable rapid creation of production-ready C# applications by providing battle-tested conventions and patterns

## v1 Requirements

Requirements for initial release. Each maps to roadmap phases.

### Infrastructure

- [ ] **INFRA-01**: All packages target .NET 10
- [ ] **INFRA-02**: Central Package Management configured (Directory.Packages.props)
- [ ] **INFRA-03**: GitVersion configured for semantic versioning
- [ ] **INFRA-04**: NuGet package metadata complete for all packages
- [ ] **INFRA-05**: GitHub Actions workflow publishes all packages to NuGet.org
- [ ] **INFRA-06**: XML documentation generated for all public APIs
- [ ] **INFRA-07**: Native AOT/trimming analysis enabled

### Core Package

- [ ] **CORE-01**: Sealed attribute base classes with proper AttributeUsage declarations
- [ ] **CORE-02**: Constructor accepts required arguments, properties accept optional arguments
- [ ] **CORE-03**: All attributes follow "Attribute" suffix naming convention
- [ ] **CORE-04**: XML documentation comments on all public APIs for IntelliSense
- [ ] **CORE-05**: Validation attribute base classes for reusable validation patterns
- [ ] **CORE-06**: Extract RequiresAttribute from imagile-app
- [ ] **CORE-07**: Extract AssociatedAttribute from imagile-app
- [ ] **CORE-08**: Extract related declarative attributes from imagile-app
- [ ] **CORE-09**: Package has zero external dependencies
- [ ] **CORE-10**: Package targets net10.0 framework

### EntityFrameworkCore Package

- [ ] **EF-01**: Automatic CreatedDate and ModifiedDate timestamps on auditable entities
- [ ] **EF-02**: User tracking with CreatedBy and ModifiedBy fields
- [ ] **EF-03**: Property-level change tracking (old value, new value) for auditable properties
- [ ] **EF-04**: Soft delete support with IsDeleted flag, DeletedDate, DeletedBy
- [ ] **EF-05**: Global query filters automatically exclude soft-deleted entities
- [ ] **EF-06**: SaveChangesInterceptor pattern for audit logging
- [ ] **EF-07**: Opt-in per entity via [Auditable] attribute or IAuditable interface
- [ ] **EF-08**: Shadow properties for audit metadata (don't pollute domain models)
- [ ] **EF-09**: Audit logging inspired by Arcoro.One EntityChangeProperty patterns
- [ ] **EF-10**: Package references Core package and EF Core 10.0
- [ ] **EF-11**: Audit context provider interface (IAuditContextProvider) for testability

### BlazorApplicationInsights Package

- [ ] **BLAZOR-01**: Page view tracking on NavigationManager.LocationChanged
- [ ] **BLAZOR-02**: Connection string configuration from appsettings.json
- [ ] **BLAZOR-03**: Dependency injection registration via AddBlazorApplicationInsights()
- [ ] **BLAZOR-04**: TelemetryClient accessible via DI for custom tracking
- [ ] **BLAZOR-05**: Automatic exception tracking for unhandled Blazor exceptions
- [ ] **BLAZOR-06**: Custom event tracking wrapper around TelemetryClient
- [ ] **BLAZOR-07**: Blazor WebAssembly support via JS interop
- [ ] **BLAZOR-08**: Custom telemetry initializer support with factory pattern for context injection
- [ ] **BLAZOR-09**: Migrate code from Imagile.BlazorApplicationInsights repository
- [ ] **BLAZOR-10**: Package references Core package and Application Insights SDK
- [ ] **BLAZOR-11**: Focus on WASM use cases (Server support secondary)

### Package Renaming

- [ ] **RENAME-01**: Rename Imagile.EntityFrameworkCore.Tests to Imagile.Framework.EntityFrameworkCore.Tests
- [ ] **RENAME-02**: Update solution name to Imagile.Framework.sln
- [ ] **RENAME-03**: Update NuGet package IDs to Imagile.Framework.* namespace
- [ ] **RENAME-04**: Update GitHub repository references and documentation

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Core Package

- **CORE-V2-01**: Source generators for compile-time attribute processing
- **CORE-V2-02**: Roslyn design-time analyzers for attribute misuse detection
- **CORE-V2-03**: Fluent Validation integration for bridging attributes with FluentValidation
- **CORE-V2-04**: Attribute composition for meta-attributes

### EntityFrameworkCore Package

- **EF-V2-01**: Multitenant data isolation (auto-filter audit by tenant context)
- **EF-V2-02**: [AuditIgnore] attribute for PII/GDPR compliance
- **EF-V2-03**: Queryable audit history as first-class EF entities
- **EF-V2-04**: Asynchronous audit writes via outbox pattern
- **EF-V2-05**: Customizable audit metadata (IP address, correlation ID)
- **EF-V2-06**: Audit event hooks for pre/post write extensibility
- **EF-V2-07**: Temporal table integration for SQL Server
- **EF-V2-08**: Audit log retention policies and archival

### BlazorApplicationInsights Package

- **BLAZOR-V2-01**: Blazor Server support (currently focused on WASM)
- **BLAZOR-V2-02**: Render mode agnostic support (works across Server/WASM/Auto)
- **BLAZOR-V2-03**: Performance metric tracking (component render time)
- **BLAZOR-V2-04**: User flow tracking across multiple pages
- **BLAZOR-V2-05**: Client-side ErrorBoundary integration
- **BLAZOR-V2-06**: Dependency correlation (link client telemetry with server API calls)
- **BLAZOR-V2-07**: Offline queuing for PWA scenarios
- **BLAZOR-V2-08**: Sampling configuration for high-traffic apps

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Multi-framework targeting (net8.0, net9.0) | Focusing on .NET 10 only to use latest features and simplify maintenance |
| Non-Azure cloud providers (AWS, GCP) | May add as separate packages later, Azure is primary target |
| Opinionated UI component library | Only providing Blazor patterns, not full component system |
| Database-specific packages beyond EF Core | No Dapper, ADO.NET wrappers - EF Core only |
| Auditing everything by default | Requires explicit opt-in per entity to prevent performance issues and PII over-logging |
| Synchronous audit writes in SaveChanges | Async pattern deferred to v2 for performance |
| Magic attribute processing | All behavior must be explicit and opt-in |
| Global TelemetryClient singleton | Must use DI for testability |
| Auto-tracking all DOM interactions | Only intentional user actions tracked to control telemetry volume/cost |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| INFRA-01 | Phase 1 | Complete |
| INFRA-02 | Phase 1 | Complete |
| INFRA-03 | Phase 1 | Complete |
| INFRA-04 | Phase 5 | Pending |
| INFRA-05 | Phase 5 | Pending |
| INFRA-06 | Phase 1 | Complete |
| INFRA-07 | Phase 1 | Complete |
| CORE-01 | Phase 2 | Pending |
| CORE-02 | Phase 2 | Pending |
| CORE-03 | Phase 2 | Pending |
| CORE-04 | Phase 2 | Pending |
| CORE-05 | Phase 2 | Pending |
| CORE-06 | Phase 2 | Pending |
| CORE-07 | Phase 2 | Pending |
| CORE-08 | Phase 2 | Pending |
| CORE-09 | Phase 2 | Pending |
| CORE-10 | Phase 2 | Pending |
| EF-01 | Phase 3 | Pending |
| EF-02 | Phase 3 | Pending |
| EF-03 | Phase 3 | Pending |
| EF-04 | Phase 3 | Pending |
| EF-05 | Phase 3 | Pending |
| EF-06 | Phase 3 | Pending |
| EF-07 | Phase 3 | Pending |
| EF-08 | Phase 3 | Pending |
| EF-09 | Phase 3 | Pending |
| EF-10 | Phase 3 | Pending |
| EF-11 | Phase 3 | Pending |
| BLAZOR-01 | Phase 4 | Pending |
| BLAZOR-02 | Phase 4 | Pending |
| BLAZOR-03 | Phase 4 | Pending |
| BLAZOR-04 | Phase 4 | Pending |
| BLAZOR-05 | Phase 4 | Pending |
| BLAZOR-06 | Phase 4 | Pending |
| BLAZOR-07 | Phase 4 | Pending |
| BLAZOR-08 | Phase 4 | Pending |
| BLAZOR-09 | Phase 4 | Pending |
| BLAZOR-10 | Phase 4 | Pending |
| BLAZOR-11 | Phase 4 | Pending |
| RENAME-01 | Phase 1 | Complete |
| RENAME-02 | Phase 1 | Complete |
| RENAME-03 | Phase 1 | Complete |
| RENAME-04 | Phase 1 | Complete |

**Coverage:**
- v1 requirements: 43 total
- Mapped to phases: 43/43 (100%)
- Unmapped: 0

**Phase Distribution:**
- Phase 1 (Foundation & Infrastructure): 9 requirements
- Phase 2 (Core Package): 10 requirements
- Phase 3 (EF Core Package): 11 requirements
- Phase 4 (Blazor Package): 11 requirements
- Phase 5 (Publishing & Documentation): 2 requirements

---
*Requirements defined: 2026-01-25*
*Last updated: 2026-01-25 after roadmap traceability mapping*
