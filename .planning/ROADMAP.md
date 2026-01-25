# Roadmap: Imagile Framework

## Overview

Transform a single-package testing library into a multi-package .NET framework by establishing a Core package with zero dependencies, extracting EF Core audit logging patterns, and migrating Blazor telemetry capabilities. The roadmap follows natural dependency boundaries: foundation first (infrastructure + Core), then specialized packages (EF Core and Blazor in parallel), concluding with NuGet publishing and documentation.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: Foundation & Infrastructure** - Project structure, renaming, and build configuration
- [ ] **Phase 2: Core Package** - Zero-dependency attribute and pattern library
- [ ] **Phase 3: EF Core Package** - Audit logging system with change tracking
- [ ] **Phase 4: Blazor Package** - Application Insights telemetry integration
- [ ] **Phase 5: Publishing & Documentation** - NuGet metadata and release automation

## Phase Details

### Phase 1: Foundation & Infrastructure
**Goal**: Establish multi-package project structure with proper naming, versioning infrastructure, and build tooling.
**Depends on**: Nothing (first phase)
**Requirements**: INFRA-01, INFRA-02, INFRA-03, INFRA-06, INFRA-07, RENAME-01, RENAME-02, RENAME-03, RENAME-04
**Success Criteria** (what must be TRUE):
  1. Solution renamed to Imagile.Framework.sln with all projects using Imagile.Framework.* namespace
  2. Central Package Management configured with Directory.Packages.props at solution root
  3. GitVersion generates semantic versions automatically from Git history
  4. XML documentation files generate for all projects without warnings
  5. Native AOT and trimming analysis enabled for all framework packages
**Plans**: 2 plans

Plans:
- [x] 01-01-PLAN.md - Build infrastructure setup (Directory.Build.props, Directory.Packages.props, GitVersion)
- [x] 01-02-PLAN.md - Project restructure (rename solution, move project, update namespaces)

### Phase 2: Core Package
**Goal**: Create zero-dependency Core package with declarative attributes extracted from imagile-app.
**Depends on**: Phase 1
**Requirements**: CORE-01, CORE-02, CORE-03, CORE-04, CORE-05, CORE-06, CORE-07, CORE-08, CORE-09, CORE-10
**Success Criteria** (what must be TRUE):
  1. Developer can reference Imagile.Framework.Core package without any transitive dependencies
  2. RequiresAttribute, AssociatedAttribute, and related attributes accessible via NuGet
  3. All public APIs documented with XML comments that appear in IntelliSense
  4. Validation attribute base classes available for custom validation patterns
  5. Package compiles without warnings when Native AOT analysis is enabled
**Plans**: TBD

Plans:
- [ ] (To be created during phase planning)

### Phase 3: EF Core Package
**Goal**: Implement audit logging system inspired by Arcoro.One with automatic timestamps, user tracking, and change tracking.
**Depends on**: Phase 2
**Requirements**: EF-01, EF-02, EF-03, EF-04, EF-05, EF-06, EF-07, EF-08, EF-09, EF-10, EF-11
**Success Criteria** (what must be TRUE):
  1. Developer can mark entity with [Auditable] attribute and automatically get CreatedDate, ModifiedDate, CreatedBy, ModifiedBy tracking
  2. SaveChangesInterceptor captures property-level changes (old value, new value) for auditable entities
  3. Soft delete support works with IsDeleted flag and global query filters automatically exclude soft-deleted entities
  4. Audit metadata stored as shadow properties without polluting domain models
  5. IAuditContextProvider interface allows testability by injecting user context
**Plans**: TBD

Plans:
- [ ] (To be created during phase planning)

### Phase 4: Blazor Package
**Goal**: Migrate BlazorApplicationInsights into framework as standalone package with automatic page tracking and custom event support.
**Depends on**: Phase 2
**Requirements**: BLAZOR-01, BLAZOR-02, BLAZOR-03, BLAZOR-04, BLAZOR-05, BLAZOR-06, BLAZOR-07, BLAZOR-08, BLAZOR-09, BLAZOR-10, BLAZOR-11
**Success Criteria** (what must be TRUE):
  1. Developer can call AddBlazorApplicationInsights() in DI container and get automatic page view tracking
  2. Connection string configurable via appsettings.json without hard-coded values
  3. TelemetryClient accessible via DI for custom tracking throughout Blazor app
  4. Unhandled Blazor exceptions automatically tracked to Application Insights
  5. Custom telemetry initializer support with factory pattern enables context injection (e.g., tenant ID)
**Plans**: TBD

Plans:
- [ ] (To be created during phase planning)

### Phase 5: Publishing & Documentation
**Goal**: Complete NuGet package metadata, publish all packages to NuGet.org, and create comprehensive documentation.
**Depends on**: Phase 3, Phase 4
**Requirements**: INFRA-04, INFRA-05
**Success Criteria** (what must be TRUE):
  1. All three packages (Core, EntityFrameworkCore, BlazorApplicationInsights) published to NuGet.org with correct metadata
  2. GitHub Actions workflow automatically publishes packages when version tags are pushed
  3. Symbol packages (.snupkg) published with SourceLink enabling step-through debugging
  4. Each package has README with usage examples visible on NuGet.org package page
  5. Comprehensive repository README documents package structure, dependencies, and consumption patterns
**Plans**: TBD

Plans:
- [ ] (To be created during phase planning)

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5

**Note:** Phase 3 and Phase 4 can execute in parallel after Phase 2 completes (no dependency between them).

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundation & Infrastructure | 2/2 | ✓ Complete | 2026-01-25 |
| 2. Core Package | 0/? | Not started | - |
| 3. EF Core Package | 0/? | Not started | - |
| 4. Blazor Package | 0/? | Not started | - |
| 5. Publishing & Documentation | 0/? | Not started | - |

---
*Roadmap created: 2026-01-25*
*Last updated: 2026-01-25 after Phase 1 completion*
