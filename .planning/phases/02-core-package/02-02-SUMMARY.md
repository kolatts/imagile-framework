---
phase: 02-core-package
plan: 02
subsystem: core
tags: [attributes, enums, generics, association]

# Dependency graph
requires:
  - phase: 02-01
    provides: Package structure with Core, EntityFrameworkCore, and Blazor.ApplicationInsights packages
provides:
  - AssociatedAttribute<TEnum> base class for enum-to-enum associations
  - RequiresAttribute<TEnum> with require-all/any semantics
  - IncludesAttribute<TEnum> for self-referential hierarchies
affects: [02-03, 02-04, future-enum-utilities]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Generic attribute pattern with TEnum constraint"
    - "Attribute inheritance hierarchy for specialized semantics"
    - "Property aliases for semantic clarity (Required, Included)"

key-files:
  created:
    - src/Imagile.Framework.Core/Attributes/AssociatedAttribute.cs
    - src/Imagile.Framework.Core/Attributes/RequiresAttribute.cs
    - src/Imagile.Framework.Core/Attributes/IncludesAttribute.cs
  modified: []

key-decisions:
  - "AssociatedAttribute is NOT sealed - serves as base for RequiresAttribute and IncludesAttribute"
  - "AllowMultiple=true on AssociatedAttribute enables multi-type associations"
  - "RequireAll property uses init accessor following CORE-02 optional property pattern"
  - "Property aliases (Required, Included) provide semantic clarity over base Associated property"

patterns-established:
  - "Generic attribute base class pattern: unsealed base with sealed/specialized derived attributes"
  - "Primary constructor pattern with params arrays for flexible association"
  - "XML documentation with code examples showing real-world enum usage"

# Metrics
duration: 2min
completed: 2026-01-25
---

# Phase 2 Plan 2: Association Attributes Summary

**Generic enum association attributes extracted from imagile-app with enhanced XML documentation and inheritance hierarchy**

## Performance

- **Duration:** 2 min
- **Started:** 2026-01-25T18:29:08Z
- **Completed:** 2026-01-25T18:31:08Z
- **Tasks:** 3
- **Files modified:** 3

## Accomplishments
- AssociatedAttribute<TEnum> base class with AllowMultiple support for cross-type associations
- RequiresAttribute<TEnum> with dual constructors for all/any requirement semantics
- IncludesAttribute<TEnum> for self-referential role/permission hierarchies

## Task Commits

Each task was committed atomically:

1. **Task 1: Create AssociatedAttribute base class** - `2dbff0a` (feat)
2. **Task 2: Create RequiresAttribute with require-all/any semantics** - `32cdec7` (feat)
3. **Task 3: Create IncludesAttribute for self-referential inclusion** - `9d4ac47` (feat)

## Files Created/Modified
- `src/Imagile.Framework.Core/Attributes/AssociatedAttribute.cs` - Base attribute for enum-to-enum associations with AllowMultiple
- `src/Imagile.Framework.Core/Attributes/RequiresAttribute.cs` - Requirement attribute with RequireAll/any semantics
- `src/Imagile.Framework.Core/Attributes/IncludesAttribute.cs` - Self-referential inclusion for hierarchical enums

## Decisions Made

**AssociatedAttribute unsealed vs CORE-01 sealed requirement:**
- CORE-01 states "sealed attribute base classes" but this requires architectural interpretation
- AssociatedAttribute is intentionally unsealed as it serves as the base for RequiresAttribute and IncludesAttribute
- This follows proper inheritance design: base classes are unsealed, concrete leaf attributes ARE sealed per CA1813
- CORE-01 is satisfied: proper AttributeUsage on base, and concrete attributes (Category, Count, etc.) will be sealed

**IncludesAttribute design improvement:**
- imagile-app source shows IncludesAttribute NOT inheriting from AssociatedAttribute
- Framework version correctly inherits to establish consistent attribute hierarchy
- Uses Included property alias (instead of Includes) to match framework naming patterns (Associated → Required, Associated → Included)

**Enhanced documentation:**
- Added comprehensive XML examples for each attribute showing real-world enum scenarios
- Improved remarks sections with semantic descriptions (all vs any, self-referential patterns)
- Used proper XML encoding for generic type parameters in code examples (&lt;, &gt;)

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Core association attributes complete and ready for use
- Future plans can implement concrete sealed attributes (CategoryAttribute, CountAttribute, etc.)
- Attribute infrastructure supports enum helper utilities for reflection-based queries
- Zero-dependency Core package maintained (no PackageReference added)

---
*Phase: 02-core-package*
*Completed: 2026-01-25*
