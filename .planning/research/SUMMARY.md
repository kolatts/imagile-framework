# Project Research Summary

**Project:** Imagile.Framework - Multi-Package .NET Framework Library
**Domain:** .NET Framework Library Development (NuGet Distribution)
**Researched:** 2026-01-25
**Confidence:** HIGH

## Executive Summary

This project is a multi-package .NET framework library designed to accelerate B2B SaaS development through three specialized packages: Core (declarative attributes), EntityFrameworkCore (audit logging), and BlazorApplicationInsights (telemetry). The research reveals that successful .NET framework libraries follow a layered abstraction pattern where a zero-dependency Core package provides fundamental abstractions, while specialized packages build upon Core with framework-specific implementations.

The recommended approach uses .NET 10 LTS (3-year support window), GitVersion.MsBuild for automated versioning, and Central Package Management to coordinate dependencies across packages. For EF Core audit logging, implement a custom SaveChangesInterceptor rather than external libraries to maintain zero-dependency architecture. For Blazor telemetry, use BlazorApplicationInsights 3.3.0 which supports both Server and WebAssembly hosting models. The Core package should use System.ComponentModel.DataAnnotations for attributes, requiring zero external dependencies.

Key risks include viral dependency propagation (adding dependencies to Core forces all consumers to inherit them), Native AOT/trimming incompatibility (library must work in 2026 cloud-native environments), and EF Core audit logging transaction scope confusion (orphaned audit records if not handled correctly). Mitigation strategies: establish zero-dependency policy for Core from day one, enable AOT/trim analysis upfront, and buffer audit events within the same transaction rather than writing to separate database contexts.

## Key Findings

### Recommended Stack

The stack centers on .NET 10 LTS (released November 2025, supported until November 2028) with a multi-package architecture managed through Central Package Management. GitVersion.MsBuild 6.5.1 provides automated semantic versioning from Git history, eliminating manual version coordination across packages. SourceLink enables step-through debugging of published NuGet packages.

**Core technologies:**
- **.NET 10 LTS (net10.0):** Target framework with 3-year support window, Native AOT improvements, C# 14 features
- **GitVersion.MsBuild 6.5.1:** Automated versioning with branching strategy support (GitFlow, GitHub Flow)
- **Custom EF Core Interceptor:** Zero-dependency audit logging using ISaveChangesInterceptor pattern (EF Core 5.0+)
- **BlazorApplicationInsights 3.3.0:** Blazor-specific telemetry integration with auto page tracking and custom events
- **System.ComponentModel.DataAnnotations:** Built-in .NET attributes for validation (zero external dependencies)
- **Central Package Management (Directory.Packages.props):** Single source of truth for dependency versions across packages

**Critical version decisions:**
- Minimum version specifications (no upper bounds) for external dependencies to prevent diamond conflicts
- Unified versioning across all three packages (simplifies support and testing)
- AssemblyVersion locked to major version only (1.0.0.0 for all 1.x releases to prevent binding breaks)

### Expected Features

Research identified clear table stakes (must-have for adoption), competitive differentiators, and features to defer post-MVP. The feature landscape balances initial adoption requirements against long-term competitive positioning.

**Must have (table stakes):**
- **Core:** Sealed attribute classes with AttributeUsage, constructor+property pattern for required/optional args, XML documentation
- **EF Core:** Automatic Created/Modified timestamps, user tracking (CreatedBy/ModifiedBy), change tracking (old/new values), soft delete with global query filters, opt-in via `[Auditable]` attribute
- **Blazor:** DI registration (`AddBlazorApplicationInsights()`), page view auto-tracking, custom event tracking, exception tracking, Blazor Server support

**Should have (competitive):**
- **Core:** Validation attribute base classes, source generators for AOT-friendly code generation
- **EF Core:** Multitenant data isolation, audit ignore attribute for PII compliance, queryable audit history with LINQ
- **Blazor:** Render mode agnostic (Server/WASM/Auto), custom telemetry initializers (tenant context enrichment), performance metric tracking

**Defer (v2+):**
- **Core:** Fluent Validation integration, design-time analyzers
- **EF Core:** Asynchronous audit writes (Outbox pattern), temporal table integration, bulk change optimization
- **Blazor:** Blazor WebAssembly support (high complexity), offline queuing for PWA scenarios

### Architecture Approach

Multi-package .NET libraries succeed through strict unidirectional dependency flow where all dependencies point toward a zero-dependency Core package. This "Core-Extensions" model enables minimal dependency footprint for consumers who only need core functionality while specialized packages bring their own framework-specific dependencies. Microsoft's own ecosystem (Microsoft.Extensions.\*, Entity Framework Core) demonstrates this pattern at scale.

**Major components:**

1. **Core Package (Imagile.Framework.Core)** - Zero-dependency foundation providing interfaces, base classes, custom attributes, and reusable patterns. Everything flows from here.

2. **EntityFrameworkCore Package (Imagile.Framework.EntityFrameworkCore)** - References Core + EF Core packages. Implements audit logging via custom SaveChangesInterceptor, entity conventions, and EF-specific repository patterns.

3. **BlazorApplicationInsights Package (Imagile.Framework.BlazorApplicationInsights)** - References Core + Blazor packages. Provides DI registration, page view tracking components, telemetry services with JSInterop for Application Insights.

4. **EntityFrameworkCore.Tests** - References EntityFrameworkCore package. Unit and integration tests for EF implementations. NOT published to NuGet (IsPackable=false).

**Key patterns:**
- Interfaces in Core, implementations in specialized packages (Dependency Inversion at package level)
- Extension methods in specialized packages add fluent APIs to Core types
- Attributes in Core define metadata; specialized packages read and process them
- Central Package Management coordinates external dependency versions

### Critical Pitfalls

From analyzing common .NET library failures and Microsoft guidance, five pitfalls stand out as project-killing:

1. **Viral Dependency Propagation** - Adding dependencies to Core forces all consumers to inherit them, causing diamond conflicts and bloated binaries. A single careless `<PackageReference>` ripples through the ecosystem. Prevention: Core MUST have zero dependencies; use `PrivateAssets="All"` for build-time tools; review dependency graph before each release.

2. **Native AOT/Trimming Incompatibility** - Library fails when consumers publish with Native AOT or trimming (table stakes in 2026 cloud-native apps). Using reflection without trim/AOT annotations causes runtime crashes. Prevention: Enable `<IsAotCompatible>true</IsAotCompatible>` and `<IsTrimmable>true</IsTrimmable>` from day one; use `[RequiresUnreferencedCode]` for reflection-heavy APIs; provide AOT-compatible alternatives via source generators.

3. **EF Core Audit Transaction Scope Confusion** - Writing audit records from interceptor in separate transactions creates orphaned audit records when primary operations fail/rollback. Compliance nightmare. Prevention: Buffer audit events in interceptor, write within same transaction; handle ExecuteUpdate/ExecuteDelete explicitly with manual transactions.

4. **Assembly Identity Changes (Strong Naming)** - Changing assembly name, strong naming key, or incrementing AssemblyVersion for minor releases breaks all compiled code. Prevention: Decide on strong naming before first release (recommend: no strong naming for modern .NET); lock AssemblyVersion to major version only (1.0.0.0 for all 1.x releases).

5. **Central Package Management Misconfiguration** - Directory.Packages.props in wrong location or leaving Version attributes in .csproj causes NU1008 errors and version drift. Prevention: Place Directory.Packages.props at solution root; remove all Version attributes from PackageReference items in .csproj files; clean bin/obj after migration.

## Implications for Roadmap

Based on research, the multi-package architecture dictates a clear phase structure. Core must be built first (zero dependencies), then specialized packages can be developed in parallel. Each phase avoids specific pitfalls identified in research.

### Phase 1: Core Package Foundation

**Rationale:** Core defines abstractions that both specialized packages depend on. Must establish zero-dependency policy and AOT/trim compatibility from the start. No other phases can proceed until Core exists.

**Delivers:**
- Zero-dependency Core package (Imagile.Framework.Core)
- Attribute base classes with proper AttributeUsage (sealed, required vs optional pattern)
- Validation attribute framework using System.ComponentModel.DataAnnotations
- Build infrastructure (Directory.Build.props, Directory.Packages.props, GitVersion configuration)

**Addresses (from FEATURES.md):**
- Sealed attribute classes with AttributeUsage declaration
- Constructor for required args, properties for optional args
- XML documentation comments on all public APIs
- Validation attribute base classes

**Avoids (from PITFALLS.md):**
- Viral dependency propagation (establish zero-dependency policy)
- Over-exposing public API surface (default to internal, use InternalsVisibleTo for tests)
- Native AOT incompatibility (enable IsAotCompatible and IsTrimmable from day one)
- Attribute design pitfalls (seal classes, proper AttributeUsage, AOT annotations)

**Research flags:** Low - attribute design is well-documented with Microsoft guidelines. Standard patterns, skip phase-specific research.

### Phase 2: Build & Versioning Infrastructure

**Rationale:** Multi-package coordination requires versioning strategy and Central Package Management before adding more packages. Prevents version drift and simplifies releases.

**Delivers:**
- GitVersion.MsBuild integration with branch-based versioning
- Directory.Packages.props with centralized dependency management
- Directory.Build.props with shared metadata (authors, license, SourceLink)
- Versioning strategy documentation (AssemblyVersion, FileVersion, PackageVersion)
- CI/CD pipeline skeleton for automated builds

**Uses (from STACK.md):**
- GitVersion.MsBuild 6.5.1 for automated versioning
- Microsoft.SourceLink.GitHub 8.0.0 for symbol packages
- Central Package Management (ManagePackageVersionsCentrally=true)

**Avoids (from PITFALLS.md):**
- CPM misconfiguration (Directory.Packages.props at solution root, no Version in PackageReference)
- Versioning inconsistency (document and configure all four version types)
- Assembly identity changes (lock AssemblyVersion to major version)

**Research flags:** Low - GitVersion and CPM are well-documented Microsoft features. Standard setup, skip research.

### Phase 3: EF Core Package - Audit Logging

**Rationale:** Depends on Core package for `[Auditable]` attribute and base abstractions. Independent of Blazor package, can proceed in parallel with Phase 4.

**Delivers:**
- Imagile.Framework.EntityFrameworkCore package
- Custom SaveChangesInterceptor for audit logging
- Automatic Created/Modified timestamps and user tracking
- Change tracking (old/new values)
- Soft delete with global query filters
- Shadow property support for audit metadata
- Unit and integration tests (EntityFrameworkCore.Tests)

**Implements (from ARCHITECTURE.md):**
- EF Core Interceptor pattern (ISaveChangesInterceptor)
- Repository implementations of Core abstractions
- DbContext extension methods for audit configuration
- Attribute-based opt-in (reads `[Auditable]` from Core)

**Addresses (from FEATURES.md):**
- Automatic timestamps (CreatedDate, ModifiedDate)
- User tracking (CreatedBy, ModifiedBy)
- Change tracking at property level
- Soft delete with IsDeleted flag
- Opt-in per entity via attribute/interface

**Avoids (from PITFALLS.md):**
- Transaction scope confusion (buffer events, write in same transaction)
- DbContext lifetime mismanagement (document Scoped lifetime, provide IDbContextFactory examples)
- Viral dependencies (only reference Core + EF Core packages, no extras)

**Research flags:** MEDIUM - Custom interceptor implementation needs deeper technical research. Patterns are documented but require careful transaction handling and performance testing. Consider `/gsd:research-phase` for:
- Transaction scope strategy (same vs separate DbContext for audit)
- Shadow property implementation details
- Performance implications of change tracking overhead

### Phase 4: Blazor Package - Telemetry (Parallel with Phase 3)

**Rationale:** Depends only on Core package, independent of EF package. Can develop in parallel with Phase 3 once Core is complete.

**Delivers:**
- Imagile.Framework.BlazorApplicationInsights package
- DI registration (`AddBlazorApplicationInsights()` extension)
- Automatic page view tracking on navigation
- Custom event tracking wrapper
- Exception tracking integration
- TelemetryClient access via DI
- Blazor Server support

**Uses (from STACK.md):**
- BlazorApplicationInsights 3.3.0 for Blazor-specific integration
- Microsoft.AspNetCore.Components 10.0.0
- Microsoft.Extensions.Logging.Abstractions 10.0.0

**Implements (from ARCHITECTURE.md):**
- Blazor components for telemetry tracking
- Service registration with DI extensions
- JSInterop for Application Insights in WASM (defer to post-MVP)

**Addresses (from FEATURES.md):**
- Page view tracking (NavigationManager.LocationChanged hook)
- Connection String configuration (appsettings.json)
- DI registration pattern
- Custom event tracking
- Exception tracking
- Blazor Server support only (defer WASM)

**Avoids (from PITFALLS.md):**
- Blazor Server vs WASM assumptions (design for Server first, document WASM as post-MVP)
- Viral dependencies (only Blazor + Core, no extras)
- Hard-coded telemetry keys (configuration-based with Key Vault support)

**Research flags:** MEDIUM - BlazorApplicationInsights package compatibility with .NET 10 needs validation testing. Consider `/gsd:research-phase` for:
- .NET 10 compatibility verification (package shows "computed compatibility")
- Custom telemetry initializer patterns for tenant context
- Performance impact of page view tracking

### Phase 5: NuGet Publishing & Documentation

**Rationale:** All packages implemented, now focus on distribution quality. Metadata is immutable after publish, must get it right.

**Delivers:**
- Complete NuGet package metadata (descriptions, tags, license, README)
- Symbol packages (.snupkg) with SourceLink configuration
- Published packages to NuGet.org
- Comprehensive README with usage examples
- XML documentation files included in packages
- CI/CD automation for publish on tags

**Addresses (from FEATURES.md):**
- XML documentation comments (IntelliSense support)
- Package README files
- SourceLink for debugging support

**Avoids (from PITFALLS.md):**
- Missing/incorrect package metadata (complete before first publish, immutable after)
- Test packages published to NuGet (IsPackable=false on .Tests projects)
- Incomplete XML docs (enable GenerateDocumentationFile, suppress CS1591)

**Research flags:** Low - NuGet publishing is well-documented standard process. Skip research.

### Phase 6: Advanced Features (Post-MVP)

**Rationale:** Core functionality delivered, now add competitive differentiators based on user feedback and adoption metrics.

**Potential features:**
- **EF Core:** Multitenant data isolation, async audit writes (Outbox), queryable audit history
- **Blazor:** WASM support, render mode agnostic, custom telemetry initializers, performance tracking
- **Core:** Source generators for AOT, Fluent Validation integration, design-time analyzers

**Research flags:** HIGH - These features are complex and benefit from real-world usage feedback before design. Conduct phase-specific research when prioritizing post-MVP work.

### Phase Ordering Rationale

**Why this order:**
1. Core must come first (all packages depend on it)
2. Build/versioning infrastructure before adding more packages (prevents coordination issues)
3. EF Core and Blazor packages can proceed in parallel (no dependency between them)
4. Publishing last ensures all packages ready before distribution

**Dependency-driven sequence:**
```
Phase 1 (Core)
    ├─> Phase 2 (Build Infrastructure)
    ├─> Phase 3 (EF Core) ──┐
    └─> Phase 4 (Blazor) ────┤
                             └─> Phase 5 (Publishing)
                                     └─> Phase 6 (Advanced)
```

**How this avoids pitfalls:**
- Zero-dependency policy established in Phase 1 before adding external packages
- CPM configured in Phase 2 before package proliferation creates version drift
- AOT/trim compatibility validated early (Phase 1) before complex implementations
- Transaction scope strategy researched before EF Core implementation (Phase 3)
- Publishing last ensures metadata complete and immutable

### Research Flags

**Phases needing deeper research during planning:**
- **Phase 3 (EF Core Audit):** Custom interceptor transaction strategy, shadow property patterns, performance testing approach - MEDIUM complexity
- **Phase 4 (Blazor Telemetry):** BlazorApplicationInsights .NET 10 compatibility, custom initializer patterns - MEDIUM complexity

**Phases with standard patterns (skip research-phase):**
- **Phase 1 (Core):** Attribute design follows Microsoft guidelines, well-documented
- **Phase 2 (Build Infrastructure):** GitVersion and CPM are standard Microsoft tools
- **Phase 5 (Publishing):** NuGet publishing is standard, documented process

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Official Microsoft docs for .NET 10, verified NuGet package versions, GitVersion widely adopted |
| Features | HIGH | Multiple authoritative sources (Microsoft docs, mature libraries, 2025-2026 articles), clear table stakes vs differentiators |
| Architecture | HIGH | Microsoft.Extensions.\* as proven reference architecture, multi-package patterns documented in Microsoft Learn |
| Pitfalls | HIGH | Official Microsoft library guidance + InfoQ enterprise article + recent community sources (2025-2026) |

**Overall confidence:** HIGH

Research is based on official Microsoft documentation for .NET 10 LTS, verified current NuGet package versions (GitVersion 6.5.1, BlazorApplicationInsights 3.3.0, EF Core 10.0), and recent authoritative sources from 2025-2026. The multi-package architecture pattern is proven at scale by Microsoft's own ecosystem. Custom EF Core interceptor approach has MEDIUM-HIGH confidence due to implementation complexity, but pattern is well-documented with fallback option (Audit.EntityFramework.Core) if complexity grows.

### Gaps to Address

**Areas requiring validation during implementation:**

- **BlazorApplicationInsights .NET 10 compatibility:** Package shows "computed compatibility" for .NET 10 based on .NET 9 support. Needs runtime testing in .NET 10 Blazor Server and WASM projects. LOW risk - likely no issues, but validate early in Phase 4.

- **Custom EF Core interceptor performance at scale:** Transaction overhead of buffering audit events within SaveChanges needs load testing with high-concurrency scenarios. Test with bulk operations (1000+ entities per transaction). Address in Phase 3 integration testing.

- **Multitenant data isolation strategy:** Research identified Finbuckle.MultiTenant as reference architecture, but specific implementation for audit logging filtering needs deeper design. Defer to Phase 6 (post-MVP), conduct research before implementation.

- **Source generator AOT alternatives:** If reflection-heavy attribute processing becomes performance bottleneck, source generators provide compile-time alternative. Pattern is documented but complex. Defer to Phase 6, evaluate based on real-world AOT adoption metrics.

**How to handle:**
- Phase 3/4: Include integration tests validating .NET 10 compatibility
- Phase 3: Include load testing for audit interceptor performance
- Phase 6: Conduct targeted research when prioritizing advanced features

## Sources

### Primary (HIGH confidence)

**Official Microsoft Documentation:**
- [What's new in .NET 10 SDK](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/sdk) - .NET 10 features, release date, support window
- [.NET 10 Announcement](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/) - Official GA announcement
- [Dependencies and .NET libraries](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/dependencies) - Dependency management best practices
- [Breaking changes and .NET libraries](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/breaking-changes) - Versioning and assembly identity
- [Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management) - CPM configuration
- [EF Core Interceptors](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors) - SaveChangesInterceptor pattern
- [Attributes (.NET Framework design guidelines)](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/attributes) - Attribute design patterns
- [How to make libraries compatible with native AOT](https://devblogs.microsoft.com/dotnet/creating-aot-compatible-libraries/) - AOT compatibility guidance

**Verified NuGet Packages:**
- [GitVersion.MsBuild 6.5.1](https://www.nuget.org/packages/GitVersion.MsBuild/) - Nov 2025 release
- [BlazorApplicationInsights 3.3.0](https://www.nuget.org/packages/BlazorApplicationInsights) - Oct 2025 release, .NET 10 computed compatibility
- [Microsoft.SourceLink.GitHub 8.0.0](https://www.nuget.org/packages/Microsoft.SourceLink.GitHub/) - Stable release

### Secondary (MEDIUM confidence)

**Community Best Practices (2025-2026):**
- [Best Practices for Managing Shared Libraries in .NET Applications at Scale - InfoQ](https://www.infoq.com/articles/shared-libraries-dotnet-enterprise/) - Enterprise multi-package patterns
- [Comprehensive Guide to Implementing Audit Logging in .NET with EF Core Interceptors - DEV Community](https://dev.to/hootanht/comprehensive-guide-to-implementing-audit-logging-in-net-with-ef-core-interceptors-1e83) - Custom interceptor implementation
- [Implementing NuGet Central Package Management in Visual Studio 2026 - Medium](https://sachidevop.medium.com/implementing-nuget-central-package-management-cpm-in-a-net-solution-using-visual-studio-2026-c93f207edcb6) - CPM setup
- [Emerging Trends in Blazor Development for 2026 - Medium](https://medium.com/@reenbit/emerging-trends-in-blazor-development-for-2026-70d6a52e3d2a) - Blazor hosting model trends

**Reference Architectures:**
- [Microsoft.Extensions.AI Preview](https://devblogs.microsoft.com/dotnet/introducing-microsoft-extensions-ai-preview/) - Multi-package architecture example
- [Finbuckle.MultiTenant](https://www.finbuckle.com/multitenant) - Multitenant data isolation patterns
- [BlazorApplicationInsights GitHub](https://github.com/IvanJosipovic/BlazorApplicationInsights) - Implementation reference

### Tertiary (Context for validation)

**Anti-Patterns and Common Mistakes:**
- [Common .NET Core Anti-Patterns and How to Avoid Them - Medium](https://medium.com/@robhutton8/common-net-core-anti-patterns-and-how-to-avoid-them-533b9812b6d5)
- [Top 15 Mistakes .NET Developers Make - Anton Dev Tips](https://antondevtips.com/blog/top-15-mistakes-dotnet-developers-make-how-to-avoid-common-pitfalls)

---
*Research completed: 2026-01-25*
*Ready for roadmap: yes*
