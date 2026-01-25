---
phase: 03-ef-core-package
plan: 03
subsystem: database
tags: [entity-framework-core, audit-logging, change-tracking, entities]

# Dependency graph
requires:
  - phase: 03-ef-core-package
    plan: 01
    provides: Interface hierarchy for audit tracking (ITimestampedEntity, IAuditableEntity, IEntityChangeAuditable)
  - phase: 03-ef-core-package
    plan: 02
    provides: Auditable and IgnoreAudit attributes for property-level opt-in
provides:
  - EntityChange header entity for recording who changed what entity and when
  - EntityChangeProperty detail entity for property-level old/new value tracking
  - EntityChangeOperation enum for categorizing Create, Update, Delete operations
  - Two-table design for efficient audit history querying
  - Foundation for audit interceptor persistence layer
affects: [03-ef-core-package, audit-system, change-tracking]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Header-detail pattern for audit change tracking (EntityChange → EntityChangeProperty)"
    - "Generic TUserKey on EntityChange for flexible user identifiers"
    - "String storage for old/new values with AreValuesHidden flag"
    - "TransactionUnique for grouping changes within same SaveChanges call"

key-files:
  created:
    - src/Imagile.Framework.EntityFrameworkCore/Entities/EntityChangeOperation.cs
    - src/Imagile.Framework.EntityFrameworkCore/Entities/EntityChange.cs
    - src/Imagile.Framework.EntityFrameworkCore/Entities/EntityChangeProperty.cs
  modified: []

key-decisions:
  - "Two-table design: EntityChange header + EntityChangeProperty details for efficient querying"
  - "TransactionUnique groups all changes within same SaveChanges call"
  - "CorrelationId links audit records to distributed tracing"
  - "String storage for OriginalValue/NewValue enables universal property type support"
  - "AreValuesHidden flag supports sensitive data tracking without exposing values"
  - "ParentEntityName/ParentItemId enable hierarchical change tracking"

patterns-established:
  - "EntityChange stores who/when/what entity metadata"
  - "EntityChangeProperty stores property name/old value/new value details"
  - "Navigation property from EntityChange to ICollection<EntityChangeProperty>"
  - "Foreign key relationship from EntityChangeProperty to EntityChange"

# Metrics
duration: 2min
completed: 2026-01-25
---

# Phase 03 Plan 03: Audit Change Entities Summary

**Two-table audit history design with EntityChange header (who/when/what) and EntityChangeProperty details (property/old/new values) for property-level change tracking**

## Performance

- **Duration:** 2 min
- **Started:** 2026-01-25T22:07:10Z
- **Completed:** 2026-01-25T22:09:30Z
- **Tasks:** 3
- **Files modified:** 3

## Accomplishments
- Created EntityChangeOperation enum defining Create, Update, Delete operation types
- Built EntityChange header entity with TransactionUnique, CorrelationId, and user/timestamp tracking
- Built EntityChangeProperty detail entity with property name, old/new values, and AreValuesHidden flag
- Established two-table design for efficient audit querying (find all changes to entity, or drill down to property changes)
- Generic TUserKey parameter on EntityChange supports flexible user identifier types

## Task Commits

Each task was committed atomically:

1. **Task 1: Create EntityChangeOperation enum** - `97b85e8` (feat)
2. **Task 2: Create EntityChange entity** - `0439eb5` (feat)
3. **Task 3: Create EntityChangeProperty entity** - `95e6b1f` (feat)

## Files Created/Modified

- `src/Imagile.Framework.EntityFrameworkCore/Entities/EntityChangeOperation.cs` - Enum for Create, Update, Delete operations
- `src/Imagile.Framework.EntityFrameworkCore/Entities/EntityChange.cs` - Header entity recording who changed what entity and when
- `src/Imagile.Framework.EntityFrameworkCore/Entities/EntityChangeProperty.cs` - Detail entity recording old and new values for individual properties

## Decisions Made

- **Two-table design:** Enables efficient querying - find all changes to an entity via EntityChange, or drill down to specific property changes via EntityChangeProperty
- **TransactionUnique grouping:** All changes within same SaveChanges call share the same TransactionUnique Guid for transaction-level queries
- **CorrelationId for distributed tracing:** Links audit records to HTTP request TraceIdentifier, spans multiple SaveChanges within same request
- **String storage for values:** OriginalValue and NewValue stored as strings to support any property type universally
- **AreValuesHidden flag:** Supports sensitive data tracking (records that change occurred without exposing actual values)
- **Hierarchical tracking support:** ParentEntityName and ParentItemId enable grouping child entity changes with parent

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

- **Build warnings for MaxLengthAttribute:** AOT/trimming analysis warnings (IL2026) on MaxLengthAttribute usage are expected based on project configuration that enables AOT analysis by default. These are informational only and don't affect functionality.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

**Ready for next plan (03-04):**
- EntityChange and EntityChangeProperty entities created for audit persistence
- Two-table design enables efficient querying patterns
- TransactionUnique and CorrelationId support transaction grouping and distributed tracing
- AreValuesHidden flag ready for [Auditable(hideValueChanges: true)] integration
- Foundation ready for audit interceptor to populate these entities on SaveChanges

**Notes:**
- Plan 03-04 can now implement the audit interceptor that populates these entities
- DbContext configuration will need to add DbSet<EntityChange<TUserKey>> and DbSet<EntityChangeProperty>
- Entity configuration (indexes, relationships) will be established via Fluent API in future plan

---
*Phase: 03-ef-core-package*
*Completed: 2026-01-25*
