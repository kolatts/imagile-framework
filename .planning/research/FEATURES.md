# Feature Landscape: .NET Framework Libraries

**Domain:** Multi-package .NET framework for rapid B2B SaaS development
**Researched:** 2026-01-25
**Confidence:** HIGH

## Executive Summary

This research covers feature expectations across three key package domains:
1. **Core Package** - Declarative attribute systems and reusable patterns
2. **EntityFrameworkCore Package** - EF Core audit logging with multitenant support
3. **BlazorApplicationInsights Package** - Application Insights integration for Blazor

The feature landscape is categorized by what developers expect as table stakes (must-have for adoption), what differentiates this framework from building from scratch, and what anti-features to deliberately avoid.

---

## Table Stakes Features

Features users expect in professional .NET framework libraries. Missing these means developers won't adopt the library.

### Core Package (Declarative Attributes)

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| **AttributeUsage Declaration** | Standard .NET pattern; all custom attributes must specify valid targets | Low | Framework Design Guidelines requirement |
| **Sealed Attribute Classes** | Performance best practice; improves attribute lookup | Low | Microsoft recommends sealing for framework libraries |
| **Constructor for Required Args** | Clear API contract distinguishing required vs optional parameters | Low | Constructor params = required, properties = optional |
| **Settable Properties for Optional Args** | Standard .NET idiom for optional attribute configuration | Low | Prevents constructor overload explosion |
| **"Attribute" Suffix Naming** | Universal .NET convention | Low | Users expect `[MyFeature]` via `MyFeatureAttribute` |
| **XML Documentation Comments** | IntelliSense support is non-negotiable for library adoption | Medium | Document attribute purpose, parameters, usage examples |

### EntityFrameworkCore Package (Audit Logging)

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| **Automatic Created/Modified Timestamps** | Universal audit requirement; manual tracking is error-prone | Low | CreatedDate, ModifiedDate on all auditable entities |
| **User Tracking (Created/Modified By)** | Compliance requirement (SOX, GDPR, HIPAA) | Medium | Must integrate with authentication system |
| **Change Tracking (Old/New Values)** | Core audit requirement; "what changed" is the point of audit logs | Medium | Track property-level changes, not just entity-level |
| **Soft Delete Support** | Data recovery and retention compliance requirement | Medium | IsDeleted flag + DeletedDate + DeletedBy + global query filters |
| **EF Core Interceptor Pattern** | Modern best practice (2026); cleaner than SaveChanges override | Medium | ISaveChangesInterceptor for separation of concerns |
| **Global Query Filters** | Automatic exclusion of soft-deleted records | Low | Users expect deleted entities to be invisible by default |
| **Opt-In Per Entity** | Not all entities need auditing; explicit opt-in via attribute/interface | Low | `[Auditable]` or `IAuditable` marker |
| **Shadow Property Support** | Audit metadata shouldn't pollute domain models | Medium | EF Core shadow properties for audit fields |

### BlazorApplicationInsights Package

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| **Page View Tracking** | Most basic telemetry requirement; tracks navigation | Low | Auto-trigger on NavigationManager.LocationChanged |
| **Connection String Configuration** | Standard Application Insights setup | Low | appsettings.json or builder configuration |
| **Dependency Injection Registration** | Modern .NET pattern; `AddBlazorApplicationInsights()` | Low | Follows .NET hosting model conventions |
| **TelemetryClient Access** | Direct access for custom tracking | Low | Injected via DI for custom events/metrics |
| **Exception Tracking** | Critical for production monitoring | Medium | Auto-capture unhandled exceptions in Blazor |
| **Custom Event Tracking** | Users need to track domain-specific events | Low | Wrapper around TelemetryClient.TrackEvent |
| **Blazor Server Support** | Server hosting model is common | Low | Built on ASP.NET Core, standard AI integration works |
| **Blazor WebAssembly Support** | WASM is growing in enterprise (2026 trend) | High | Requires JS interop for client-side tracking |

---

## Differentiating Features

Features that provide competitive advantage over building from scratch. Not expected, but highly valued.

### Core Package (Declarative Attributes)

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| **Validation Attribute Base Classes** | Reusable validation patterns reduce boilerplate | Medium | Base class for common validations (e.g., `ConditionalRequiredAttribute`) |
| **Fluent Validation Integration** | Bridge declarative attributes with FluentValidation | High | Attribute → Validator mapping for best of both worlds |
| **Source Generators for Attribute Processing** | Compile-time code generation from attributes | High | Performance win; eliminates reflection at runtime |
| **Multi-Target Attribute Support** | Single attribute applicable to class, method, property | Medium | `[AttributeUsage(AttributeTargets.Class \| AttributeTargets.Method)]` |
| **Attribute Composition** | Combine multiple attributes into meta-attributes | High | Reduces repetition for common attribute combinations |
| **Design-Time Analyzers** | Roslyn analyzers for attribute misuse detection | High | Catch errors at compile time, not runtime |

### EntityFrameworkCore Package (Audit Logging)

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| **Multitenant Data Isolation** | Automatic tenant filtering in audit logs | High | Audit records filtered by tenant context; critical for B2B SaaS |
| **Audit Ignore Attribute** | Exclude sensitive fields from audit trails | Medium | `[AuditIgnore]` for PII/GDPR compliance |
| **Queryable Audit History** | Audit logs as first-class EF entities | Medium | Query audit trail with LINQ; enables compliance reporting |
| **Asynchronous Audit Writes** | Decouple audit from main transaction for performance | High | Outbox pattern or Channels for bulk audit writes |
| **Customizable Audit Metadata** | Extensible audit context (e.g., IP address, correlation ID) | Medium | Hook for adding custom audit properties |
| **Audit Event Hooks** | Pre/post audit write events for extensibility | Medium | Allow users to intercept and modify audit records |
| **Bulk Change Optimization** | Efficient audit logging for bulk operations | High | Group related changes to reduce audit record volume |
| **Temporal Table Integration** | Native SQL Server temporal table support | Medium | Leverage database-native audit for performance |
| **Audit Log Retention Policies** | Automatic archival/deletion based on age | High | Compliance requirement; prevents audit table bloat |

### BlazorApplicationInsights Package

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| **Render Mode Agnostic** | Works across Server, WASM, Auto render modes | High | Future-proof for .NET 8+ unified hosting model |
| **Custom Telemetry Initializers** | Enrich telemetry with tenant context, user info | Medium | Critical for multitenant SaaS; adds tenant ID to all telemetry |
| **Performance Metric Tracking** | Component render time, interaction latency | High | Blazor-specific performance insights |
| **User Flow Tracking** | Track multi-step processes across pages | Medium | Correlation of page views into user journeys |
| **Client-Side Error Boundaries** | Capture Blazor ErrorBoundary exceptions | Medium | Automatic tracking of component-level errors |
| **Dependency Correlation** | Link Blazor client telemetry with server API calls | High | End-to-end transaction tracking across tiers |
| **Offline Queuing** | Buffer telemetry when offline (WASM) | High | PWA support; sync when connectivity restored |
| **Sampling Configuration** | Control telemetry volume/cost | Medium | Configurable sampling for high-traffic apps |
| **Custom Dimension Defaults** | Auto-add properties to all telemetry | Low | Version, environment, tenant ID without repetition |

---

## Anti-Features

Features to explicitly NOT build. Common mistakes in this domain.

### Core Package (Declarative Attributes)

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| **Magic Attribute Processing** | Hidden behavior breaks principle of least surprise | Provide explicit configuration; require opt-in for automatic behavior |
| **Constructor Overload Explosion** | Multiple constructors make required vs optional ambiguous | Single constructor for required args, properties for optional |
| **Reflection-Heavy Runtime Processing** | Performance penalty in hot paths | Use source generators or compile-time code generation |
| **Tight Coupling to Specific Frameworks** | Limits reusability across projects | Keep core attributes framework-agnostic; separate integration packages |
| **Mutable Attribute Properties** | Attributes should be declarative, not stateful | All properties should be init-only or get-only after construction |
| **Attribute Inheritance Without [Inherited]** | Confusing behavior when derived classes don't inherit attributes | Explicitly mark with `[AttributeUsage(Inherited = true)]` or document clearly |

### EntityFrameworkCore Package (Audit Logging)

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| **Auditing Everything by Default** | Performance impact; audit table bloat; compliance risk (over-logging PII) | Require explicit opt-in per entity via `[Auditable]` or interface |
| **Synchronous Audit Writes in SaveChanges** | Blocks transaction; degrades performance at scale | Async audit writes via Outbox pattern or background channels |
| **Hard-Coded User Context** | Impossible to test; breaks in non-HTTP contexts | Inject IAuditContextProvider; allow custom implementations |
| **Audit Logs in Same DbContext** | Transaction coupling; audit failures block business operations | Separate audit DbContext or use event-driven approach |
| **Storing Full Entity Snapshots** | Massive storage overhead; JSON serialization issues | Store property-level changes only; reconstruct entity state on demand |
| **No Audit Log Versioning** | Schema changes break historical audit queries | Version audit schema; support multiple audit record formats |
| **Exposing EF Internal Details** | Leaks shadow properties, navigation properties in audit logs | Filter to tracked properties only; provide clean audit DTOs |

### BlazorApplicationInsights Package

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| **Auto-Tracking All DOM Interactions** | Massive telemetry volume; cost explosion; noise | Track intentional user actions only; provide explicit tracking helpers |
| **Logging PII by Default** | GDPR/HIPAA violations; security risk | Scrub PII; require explicit opt-in for user data collection |
| **Global TelemetryClient Singleton** | Testability issues; hidden dependencies | Use DI; inject ITelemetryService interface |
| **Hard-Coded Telemetry Keys** | Security risk; inflexible deployment | Configuration-based; support Azure Key Vault integration |
| **Blocking Telemetry Calls** | UI jank; poor user experience | Fire-and-forget; queue telemetry for background transmission |
| **Render Mode Detection via Reflection** | Fragile; breaks with .NET updates | Use official Blazor APIs; support explicit configuration |
| **Custom JavaScript Library Requirement** | Maintenance burden; versioning conflicts | Use official Application Insights JavaScript SDK via interop |

---

## Feature Dependencies

### Dependency Graph

```
Core Package Attributes
├── Validation Attributes → Fluent Validation Integration
├── Source Generators → Design-Time Analyzers
└── Attribute Composition → Multi-Target Support

EntityFrameworkCore Package
├── Soft Delete → Global Query Filters
├── EF Core Interceptor → Change Tracking
├── Multitenant Data Isolation → Queryable Audit History
├── Audit Ignore Attribute → Customizable Audit Metadata
├── Temporal Table Integration → Bulk Change Optimization
└── Asynchronous Audit Writes → Audit Log Retention Policies

BlazorApplicationInsights Package
├── Connection String Configuration → Dependency Injection Registration
├── TelemetryClient Access → Custom Event Tracking
├── Render Mode Agnostic → Blazor WASM Support
├── Custom Telemetry Initializers → Custom Dimension Defaults
├── Performance Metric Tracking → User Flow Tracking
└── Client-Side Error Boundaries → Exception Tracking
```

### Critical Path Dependencies

**Phase 1 Foundation (Must Build First)**
- Core: Attribute base classes with proper AttributeUsage
- EF: EF Core Interceptor + Change Tracking + Audit Entity Model
- Blazor: DI Registration + Connection String Configuration + Page View Tracking

**Phase 2 Core Features (Depends on Phase 1)**
- Core: Validation attributes + XML documentation
- EF: Soft Delete + Global Query Filters + User Tracking
- Blazor: Custom Event Tracking + Exception Tracking + TelemetryClient wrapper

**Phase 3 Differentiators (Depends on Phase 2)**
- Core: Source Generators + Fluent Validation Integration
- EF: Multitenant Data Isolation + Audit Ignore + Queryable History
- Blazor: Render Mode Agnostic + Custom Telemetry Initializers + Performance Metrics

---

## MVP Recommendation

For initial milestone, prioritize table stakes features to achieve basic adoption.

### Must Have (MVP)

**Core Package:**
1. Sealed attribute base classes with proper AttributeUsage
2. Constructor + property pattern for required/optional args
3. XML documentation comments
4. Validation attribute base class

**EntityFrameworkCore Package:**
1. EF Core Interceptor implementation
2. Automatic Created/Modified timestamps
3. User tracking (CreatedBy/ModifiedBy)
4. Change tracking (old/new values)
5. Soft delete with global query filters
6. Opt-in via `[Auditable]` attribute or `IAuditable` interface

**BlazorApplicationInsights Package:**
1. DI registration (`AddBlazorApplicationInsights()`)
2. Connection string configuration
3. Page view tracking (auto on navigation)
4. Custom event tracking wrapper
5. Exception tracking
6. Blazor Server support

### Defer to Post-MVP

**Core Package:**
- Source generators (High complexity; MVP can use reflection)
- Fluent Validation integration (Nice-to-have; not blocking)
- Design-time analyzers (Quality-of-life; not essential)

**EntityFrameworkCore Package:**
- Asynchronous audit writes (Performance optimization; not needed initially)
- Temporal table integration (Advanced feature; low adoption)
- Audit log retention policies (Operational concern; defer to production phase)
- Bulk change optimization (Performance; wait for real usage data)

**BlazorApplicationInsights Package:**
- Blazor WebAssembly support (High complexity; Server covers most use cases)
- Render mode agnostic (Future-proofing; .NET 8+ only)
- Performance metric tracking (Advanced; needs instrumentation design)
- Offline queuing (PWA-specific; niche requirement)

---

## Feature Complexity Matrix

| Feature Category | Low Complexity | Medium Complexity | High Complexity |
|------------------|----------------|-------------------|-----------------|
| **Core Attributes** | AttributeUsage, Naming, Sealing | Validation base classes, XML docs | Source Generators, Analyzers |
| **EF Audit Logging** | Timestamps, Soft delete flag | User tracking, Change tracking, Interceptor | Multitenant isolation, Async writes, Temporal tables |
| **Blazor App Insights** | DI registration, Config, Page views | Custom events, Exceptions, Initializers | WASM support, Render mode agnostic, Performance tracking |

---

## Sources

### Official Microsoft Documentation
- [Framework Design Guidelines - Attributes](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/attributes)
- [Options Pattern for Library Authors](https://learn.microsoft.com/en-us/dotnet/core/extensions/options-library-authors)
- [Application Insights API for Custom Events](https://learn.microsoft.com/en-us/azure/azure-monitor/app/api-custom-events-metrics)
- [EF Core Multi-tenancy](https://learn.microsoft.com/en-us/ef/core/miscellaneous/multitenancy)

### Community Best Practices & Libraries
- [Audit.EntityFramework.Core](https://www.nuget.org/packages/Audit.EntityFramework.Core)
- [BlazorApplicationInsights Package](https://github.com/IvanJosipovic/BlazorApplicationInsights)
- [Comprehensive Guide to EF Core Interceptors](https://dev.to/hootanht/comprehensive-guide-to-implementing-audit-logging-in-net-with-ef-core-interceptors-1e83)
- [Soft Delete in EF Core Patterns](https://medium.com/@kittikawin_ball/soft-delete-in-ef-core-patterns-for-safer-data-management-1d61fec4347b)
- [Implementing Audit Logs Without Polluting Entities](https://blog.elmah.io/implementing-audit-logs-in-ef-core-without-polluting-your-entities/)
- [Finbuckle.MultiTenant](https://www.finbuckle.com/multitenant)
- [Declarative Validation - ServiceStack](https://docs.servicestack.net/declarative-validation)
- [Common .NET Core Anti-Patterns](https://medium.com/@robhutton8/common-net-core-anti-patterns-and-how-to-avoid-them-533b9812b6d5)
- [Blazor Best Practices for Architecture](https://blog.devart.com/asp-net-core-blazor-best-practices-architecture-and-performance-optimization.html)
- [Emerging Trends in Blazor Development for 2026](https://medium.com/@reenbit/emerging-trends-in-blazor-development-for-2026-70d6a52e3d2a)

---

## Research Confidence Assessment

| Domain | Confidence | Reasoning |
|--------|------------|-----------|
| **Declarative Attributes** | HIGH | Official Microsoft Framework Design Guidelines verified via WebFetch; well-established patterns |
| **EF Core Audit Logging** | HIGH | Multiple authoritative sources (Microsoft docs, mature libraries like Audit.NET, recent 2025-2026 articles) |
| **Blazor App Insights** | MEDIUM-HIGH | Official Microsoft docs + existing BlazorApplicationInsights package; WASM integration has some uncertainty |
| **Multitenant Patterns** | HIGH | Well-documented via Finbuckle.MultiTenant, Microsoft EF Core docs |
| **Anti-Patterns** | MEDIUM | Based on Microsoft guidance and community articles; validated against Framework Design Guidelines |

---

## Notes for Roadmap Creation

1. **Package Ordering:** Core package must be built first (attributes used by EF package for `[Auditable]`, etc.)

2. **Integration Points:** EF package and Blazor package are independent; can be developed in parallel after Core

3. **Multitenant Cross-Cutting:** Both EF audit logging and Blazor telemetry need tenant context; consider shared abstraction in Core package

4. **Performance-Critical Features:** Source generators (Core) and async audit writes (EF) are performance optimizations that can be deferred but should be planned for Phase 3+

5. **Compliance Features:** Audit Ignore attribute and PII scrubbing are critical for B2B SaaS but can be added after basic audit logging works

6. **Testing Strategy:** Design-time analyzers (Core) and comprehensive test coverage for audit logging are differentiators worth investing in for framework quality

7. **Documentation Requirements:** XML comments are table stakes, but attribute usage examples in README/wiki are differentiators for adoption
