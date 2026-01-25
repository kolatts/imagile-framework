---
phase: 03-ef-core-package
plan: 02
subsystem: audit-attributes
tags: [efcore, attributes, change-tracking, audit]
requires: [03-01]
provides: [property-level-opt-in, sensitive-data-handling]
affects: []
tech-stack:
  added: []
  patterns: [property-level-attributes, opt-in-tracking, marker-attributes]
key-files:
  created:
    - src/Imagile.Framework.EntityFrameworkCore/Attributes/AuditableAttribute.cs
    - src/Imagile.Framework.EntityFrameworkCore/Attributes/IgnoreAuditAttribute.cs
  modified: []
decisions:
  - decision: HideValueChanges parameter for sensitive data
    rationale: Allows tracking that sensitive properties changed without exposing actual values
    alternatives: Could have used separate attribute, but single attribute with parameter is cleaner
  - decision: IgnoreAudit as marker attribute with optional Reason
    rationale: Documents intent explicitly, Reason property aids code review and maintenance
    alternatives: Could omit entirely (just not using [Auditable] is sufficient), but explicit is better
  - decision: Both attributes sealed and Inherited=true
    rationale: Prevents modification, allows subclass properties to inherit audit behavior
    alternatives: Non-sealed would allow extension but adds complexity
metrics:
  duration: ~2 minutes
  completed: 2026-01-25
---

# Phase 03 Plan 02: Audit Attributes Summary

**One-liner:** Property-level opt-in attributes with [Auditable] for tracking and [IgnoreAudit] for explicit exclusion

## What Was Built

Created the attribute system for selective property-level change tracking in EF Core entities:

1. **AuditableAttribute** - Marks properties for inclusion in EntityChangeProperty records
   - `HideValueChanges` parameter for sensitive data (passwords, tokens)
   - Sealed attribute with `AttributeTargets.Property`
   - Comprehensive XML documentation with Employee example
   - Opt-in approach prevents tracking noise

2. **IgnoreAuditAttribute** - Explicitly marks properties excluded from tracking
   - Optional `Reason` property for documentation
   - Marker attribute for code clarity
   - Sealed with `AttributeTargets.Property`
   - Comprehensive XML documentation with Session example

## How It Works

**Property-level opt-in pattern:**
```csharp
public class Employee : IEntityChangeAuditable<int>
{
    [Auditable]                        // Tracked
    public string FirstName { get; set; }

    [Auditable(hideValueChanges: true)] // Tracked, values hidden
    public string PasswordHash { get; set; }

    [IgnoreAudit]                      // Explicitly not tracked
    public string InternalNotes { get; set; }

    public string TempField { get; set; } // Not tracked (no attribute)
}
```

**Two-level opt-in system:**
- Entity must implement `IEntityChangeAuditable<TKey>` (Plan 01)
- Properties must have `[Auditable]` attribute
- Both required for change tracking to occur

## Decisions Made

### 1. HideValueChanges Parameter Design
**Decision:** Single `[Auditable(hideValueChanges: true)]` attribute instead of separate `[AuditableHidden]`

**Rationale:**
- Cleaner API with single attribute
- Parameter name is self-documenting
- Follows established patterns from Core package attributes

**Alternatives Considered:**
- Separate `[AuditableHidden]` attribute - rejected (more attributes to remember)
- `[Auditable(TrackingMode.Hidden)]` enum - rejected (overkill for boolean choice)

### 2. IgnoreAudit as Marker Attribute
**Decision:** Create `[IgnoreAudit]` despite properties without `[Auditable]` already being ignored

**Rationale:**
- Explicit exclusion documents developer intent
- `Reason` property provides context for code reviewers
- Distinguishes "forgot to add [Auditable]" from "intentionally not tracked"

**Alternatives Considered:**
- Omit entirely - rejected (implicit exclusion is ambiguous)
- Required Reason parameter - rejected (too verbose for obvious cases)

### 3. Sealed Attributes with Inherited=true
**Decision:** Both attributes are sealed but allow inheritance

**Rationale:**
- `sealed` prevents developers from extending attributes incorrectly
- `Inherited=true` allows subclass properties to inherit audit behavior
- Follows .NET Framework attribute patterns

**Alternatives Considered:**
- Non-sealed - rejected (adds complexity, no clear use case for extension)
- `Inherited=false` - rejected (would require re-applying on overridden properties)

## Technical Implementation

**Attribute Patterns:**
- Both use `AttributeTargets.Property`
- Both have `AllowMultiple = false`
- Both have `Inherited = true`
- Both are sealed classes
- Both include comprehensive XML documentation with examples

**File Structure:**
```
src/Imagile.Framework.EntityFrameworkCore/
└── Attributes/
    ├── AuditableAttribute.cs      (70 lines)
    └── IgnoreAuditAttribute.cs    (68 lines)
```

## Testing & Validation

- ✅ Solution builds with no errors
- ✅ Attributes follow established patterns from Core package
- ✅ XML documentation includes comprehensive examples
- ✅ Both attributes are sealed with correct AttributeUsage
- ✅ Property constraints validated (HideValueChanges, Reason)

## Integration Points

**Consumed By (Future Plans):**
- Plan 03: Property change tracking implementation (will read these attributes via reflection)
- Plan 04: SaveChangesAsync interceptor (will use HideValueChanges flag)

**Dependencies:**
- Plan 01: IEntityChangeAuditable interface (entity-level opt-in)

## Next Phase Readiness

**Blockers:** None

**Risks:** None

**Validation Needed:**
- Property-level tracking implementation (Plan 03) will validate attribute usage
- Integration tests needed to verify HideValueChanges behavior

**Dependencies for Next Plan:**
- These attributes are foundational and ready for consumption
- Plan 03 can proceed to implement reflection-based property tracking

## Deviations from Plan

None - plan executed exactly as written.

## Key Learnings

1. **Two-level opt-in clarity:** Interface for entity + attribute for property makes tracking intent explicit
2. **Sensitive data pattern:** `hideValueChanges` parameter provides clean solution for PII/password tracking
3. **Marker attributes value:** `[IgnoreAudit]` documents intent even though technically optional
4. **XML documentation completeness:** Comprehensive examples in attributes reduce need for separate docs

## Files Changed

### Created
1. `src/Imagile.Framework.EntityFrameworkCore/Attributes/AuditableAttribute.cs`
   - Property-level opt-in for change tracking
   - HideValueChanges parameter for sensitive data
   - 70 lines with comprehensive documentation

2. `src/Imagile.Framework.EntityFrameworkCore/Attributes/IgnoreAuditAttribute.cs`
   - Explicit exclusion marker attribute
   - Optional Reason property for documentation
   - 68 lines with use case examples

### Modified
None

## Commit History

- `6a621dd` - feat(03-02): add AuditableAttribute for property-level change tracking
- `e9b7438` - feat(03-02): add IgnoreAuditAttribute for explicit exclusion from tracking
