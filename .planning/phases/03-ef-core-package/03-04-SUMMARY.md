---
phase: 03-ef-core-package
plan: 04
subsystem: database
tags: [entityframeworkcore, efcore, audit, changetracking, dbcontext, savechanges, reflection]

# Dependency graph
requires:
  - phase: 03-01
    provides: IAuditableEntity, IEntityChangeAuditable, ITenantEntity interfaces
  - phase: 03-02
    provides: AuditableAttribute for property-level tracking
  - phase: 03-03
    provides: EntityChange and EntityChangeProperty entities
  - phase: 01-02
    provides: IAuditContextProvider in Core package
provides:
  - ImagileDbContext abstract base class with SaveChanges override
  - Automatic timestamp population (CreatedOn, ModifiedOn)
  - Automatic user tracking (CreatedBy, ModifiedBy, DeletedBy)
  - Soft delete detection and field population
  - Property-level change tracking for auditable entities
  - Temporary value handling for auto-generated keys
affects: [04-testing-conventions, 05-samples, future-ef-core-consumers]

# Tech tracking
tech-stack:
  added:
    - Microsoft.EntityFrameworkCore.Relational 10.0.0
  patterns:
    - SaveChanges override pattern (Arcoro.One proven approach)
    - Two-phase save (capture before, persist after) prevents recursive deadlock
    - Reflection-based generic interface handling for IAuditableEntity/ITenantEntity
    - Attribute-based property tracking with [Auditable]
    - Temporary property handling for auto-generated primary keys

key-files:
  created:
    - src/Imagile.Framework.EntityFrameworkCore/DbContext/ImagileDbContext.cs
  modified:
    - Directory.Packages.props
    - src/Imagile.Framework.EntityFrameworkCore/Imagile.Framework.EntityFrameworkCore.csproj

key-decisions:
  - "SaveChanges override (not ISaveChangesInterceptor) for simpler state management"
  - "Two-phase save pattern: capture changes BEFORE base.SaveChanges, persist AFTER"
  - "Reflection-based interface checking for generic type parameter matching"
  - "WithOne() relationship (no navigation property) for EntityChangeProperty to EntityChange"
  - "ISO 8601 formatting for DateTimeOffset/DateTime in audit logs"

patterns-established:
  - "Two-phase SaveChanges: CaptureEntityChanges → PopulateAuditFields → base.SaveChanges → SaveEntityChangesAsync"
  - "Soft delete detection via IsDeleted property change tracking"
  - "HideValueChanges masking sensitive data as [HIDDEN] constant"
  - "TransactionUnique per SaveChanges + CorrelationId from audit context"

# Metrics
duration: 3min
completed: 2026-01-25
---

# Phase 3 Plan 4: EF Core Package - Base DbContext Summary

**Abstract DbContext base class with SaveChanges override implementing Arcoro.One audit pattern for automatic timestamps, user tracking, soft delete, and property-level change tracking**

## Performance

- **Duration:** 3 min
- **Started:** 2026-01-25T22:12:29Z
- **Completed:** 2026-01-25T22:15:39Z
- **Tasks:** 2
- **Files modified:** 3

## Accomplishments
- ImagileDbContext abstract base class with TUserKey and TTenantKey generic parameters
- SaveChanges override implementing proven Arcoro.One two-phase save pattern
- Automatic population of all audit fields (timestamps, users, tenant, soft delete)
- Property-level change tracking respecting [Auditable] attribute with value masking
- Temporary value handling for auto-generated primary keys
- EntityChange table configuration with performance indexes

## Task Commits

Each task was committed atomically:

1. **Task 1: Add Microsoft.EntityFrameworkCore.Relational package reference** - `5dcee0b` (chore)
2. **Task 2: Create ImagileDbContext base class** - `cbb5987` (feat)

## Files Created/Modified
- `src/Imagile.Framework.EntityFrameworkCore/DbContext/ImagileDbContext.cs` - Abstract base DbContext with SaveChanges override (598 lines)
- `Directory.Packages.props` - Added Microsoft.EntityFrameworkCore.Relational 10.0.0
- `src/Imagile.Framework.EntityFrameworkCore/Imagile.Framework.EntityFrameworkCore.csproj` - Added Relational package reference

## Decisions Made

**1. SaveChanges override vs ISaveChangesInterceptor**
- Chose SaveChanges override following Arcoro.One proven pattern
- Rationale: Simpler state management, easier testing, no interceptor lifecycle complexity
- Aligns with RESEARCH.md recommendation and user CONTEXT.md "hybrid approach" decision

**2. Two-phase save pattern for audit records**
- Capture EntityChange records BEFORE base.SaveChanges (original values available)
- Persist EntityChange records AFTER base.SaveChanges (ItemIds from auto-generated keys available)
- Prevents recursive SaveChanges deadlock (Pitfall 1 from RESEARCH.md)

**3. Reflection-based generic interface handling**
- Use reflection to check if entity implements IAuditableEntity<TUserKey> or ITenantEntity<TTenantKey>
- Required because generic type parameters must match DbContext's TUserKey/TTenantKey
- Enables flexible type support (int, Guid, string) without code duplication

**4. EntityChangeProperty navigation simplification**
- Used `.WithOne()` without navigation property instead of `.WithOne(p => p.EntityChange!)`
- Rationale: EntityChangeProperty.EntityChange is typed as EntityChange<object> (non-generic), causing type mismatch
- Foreign key relationship still enforced, just no strongly-typed navigation from property back to change

**5. Value formatting for audit logs**
- ISO 8601 format for DateTimeOffset/DateTime (portable, unambiguous)
- InvariantCulture for decimal/double/float (consistent parsing)
- DisplayFormatAttribute support for custom formatting
- [HIDDEN] constant for sensitive data when HideValueChanges=true

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Added EF Core Relational package version to Directory.Packages.props**
- **Found during:** Task 1 (package reference verification)
- **Issue:** Central Package Management requires PackageVersion in Directory.Packages.props
- **Fix:** Added `<PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.0" />`
- **Files modified:** Directory.Packages.props
- **Verification:** `dotnet restore` succeeded
- **Committed in:** 5dcee0b (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Auto-fix necessary due to Central Package Management convention. No scope creep.

## Issues Encountered
None - implementation followed RESEARCH.md patterns closely, Arcoro.One code provided proven structure.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness

**Ready for Wave 4 (Extensions and Utilities):**
- ImagileDbContext provides complete base class for consumer applications
- All automatic audit functionality working (timestamps, users, soft delete, change tracking)
- Ready for extension methods (Restore, GetChangeHistory) and utilities

**Ready for Testing package:**
- Base class ready for convention testing (DbContextConventionTests can verify audit fields)
- InMemoryDatabaseTest can use ImagileDbContext derivatives

**Technical notes:**
- AOT/Trimming warnings expected (EF Core uses reflection, documented in Microsoft docs)
- Reflection-based interface handling required for generic type parameters (cannot use `is IAuditableEntity<TUserKey>` directly)
- Two-phase save pattern critical - DO NOT modify without understanding recursive deadlock risk
- EntityChange/EntityChangeProperty entities NOT audited (would cause infinite recursion)

---
*Phase: 03-ef-core-package*
*Completed: 2026-01-25*
