---
phase: 03-ef-core-package
plan: 01
subsystem: database
tags: [entity-framework-core, audit-logging, interfaces, multi-tenancy]

# Dependency graph
requires:
  - phase: 02-core-package
    provides: Core package with zero dependencies for abstractions
provides:
  - IAuditContextProvider interface in Core for audit context abstraction
  - Interface hierarchy for audit tracking (ITimestampedEntity -> IAuditableEntity -> IEntityChangeAuditable)
  - ITenantEntity interface for multi-tenant data isolation
  - Foundation for EF Core audit interceptor implementation
affects: [03-ef-core-package, multi-tenant, audit-system]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Interface-based audit opt-in pattern"
    - "Generic type parameters for flexible user/tenant identifiers"
    - "Explicit interface properties over shadow properties for testability"
    - "Separate tenant and audit concerns via independent interfaces"

key-files:
  created:
    - src/Imagile.Framework.Core/Interfaces/IAuditContextProvider.cs
    - src/Imagile.Framework.EntityFrameworkCore/Interfaces/ITimestampedEntity.cs
    - src/Imagile.Framework.EntityFrameworkCore/Interfaces/IAuditableEntity.cs
    - src/Imagile.Framework.EntityFrameworkCore/Interfaces/IEntityChangeAuditable.cs
    - src/Imagile.Framework.EntityFrameworkCore/Interfaces/ITenantEntity.cs
  modified: []

key-decisions:
  - "IAuditContextProvider in Core package for zero-dependency abstraction"
  - "Three-level interface hierarchy for progressive audit complexity"
  - "Generic TUserKey/TTenantKey parameters for flexibility"
  - "Explicit interface properties for better testability and discoverability"
  - "ITenantEntity independent of audit hierarchy for composability"
  - "DateTimeOffset for proper timezone handling"

patterns-established:
  - "Entity opt-in via interface implementation (ITimestampedEntity, IAuditableEntity, IEntityChangeAuditable)"
  - "Property opt-in via [Auditable] attribute for change tracking (to be implemented)"
  - "Audit context abstraction through IAuditContextProvider for DI integration"

# Metrics
duration: 3min
completed: 2026-01-25
---

# Phase 03 Plan 01: EF Core Audit Interfaces Summary

**Five audit interfaces establishing progressive complexity (timestamp → full audit → property tracking) with multi-tenant support**

## Performance

- **Duration:** 3 min
- **Started:** 2026-01-25T21:56:28Z
- **Completed:** 2026-01-25T21:59:42Z
- **Tasks:** 5
- **Files modified:** 5

## Accomplishments
- Created IAuditContextProvider abstraction in Core package for injecting user/tenant context
- Established three-level interface hierarchy: ITimestampedEntity → IAuditableEntity → IEntityChangeAuditable
- Added ITenantEntity as separate concern for multi-tenant data isolation
- All interfaces use generic type parameters (TUserKey, TTenantKey) for maximum flexibility
- Comprehensive XML documentation on all interfaces with usage examples

## Task Commits

Each task was committed atomically:

1. **Task 1: Create IAuditContextProvider in Core package** - `c12e11c` (feat)
2. **Task 2: Create ITimestampedEntity interface** - `c63b3d8` (feat)
3. **Task 3: Create IAuditableEntity interface** - `ab03845` (feat)
4. **Task 4: Create IEntityChangeAuditable interface** - `1addbbb` (feat)
5. **Task 5: Create ITenantEntity interface** - `02fe3da` (feat)

## Files Created/Modified

- `src/Imagile.Framework.Core/Interfaces/IAuditContextProvider.cs` - Abstraction for audit context (user ID, tenant ID, correlation ID)
- `src/Imagile.Framework.EntityFrameworkCore/Interfaces/ITimestampedEntity.cs` - Base audit interface with CreatedOn/ModifiedOn
- `src/Imagile.Framework.EntityFrameworkCore/Interfaces/IAuditableEntity.cs` - Full audit with user tracking and soft delete
- `src/Imagile.Framework.EntityFrameworkCore/Interfaces/IEntityChangeAuditable.cs` - Property-level change tracking
- `src/Imagile.Framework.EntityFrameworkCore/Interfaces/ITenantEntity.cs` - Multi-tenant isolation interface

## Decisions Made

- **IAuditContextProvider placed in Core package:** Maintains zero-dependency principle while allowing both EF Core and Blazor packages to reference the abstraction
- **Explicit interface properties over shadow properties:** Improves testability, discoverability, and aligns with Arcoro.One production patterns
- **Generic type parameters:** TUserKey and TTenantKey provide flexibility for different identifier types (int, Guid, string)
- **ITenantEntity as separate interface:** Allows entities to be tenant-scoped without audit tracking, or vice versa, via composition
- **DateTimeOffset for timestamps:** Proper timezone handling for distributed systems

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None - all interfaces created successfully and solution builds without errors.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

**Ready for next plan (03-02):**
- Interface contracts established for all audit levels
- IAuditContextProvider abstraction available for dependency injection
- Generic design supports multiple user/tenant identifier types
- Foundation ready for audit interceptor implementation

**Notes:**
- Plan 03-02 can now implement the audit interceptor using these interfaces
- Property-level change tracking requires [Auditable] attribute (deferred to Plan 03-02)
- Global query filters for soft delete and tenant isolation will be implemented in DbContext setup

---
*Phase: 03-ef-core-package*
*Completed: 2026-01-25*
