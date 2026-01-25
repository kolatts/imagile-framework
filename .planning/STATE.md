# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-01-25)

**Core value:** Enable rapid creation of production-ready C# applications by providing battle-tested conventions and patterns that enforce consistency while remaining flexible enough for both personal projects and enterprise work.
**Current focus:** Phase 3 - EF Core Package

## Current Position

Phase: 3 of 5 (EF Core Package)
Plan: 5 of 5 in current phase
Status: Phase complete
Last activity: 2026-01-25 — Completed 03-05-PLAN.md

Progress: [███████░░░] 69%

## Performance Metrics

**Velocity:**
- Total plans completed: 11
- Average duration: 2.7 min
- Total execution time: 0.5 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-foundation-infrastructure | 2 | 9min | 4.5min |
| 02-core-package | 4 | 10min | 2.5min |
| 03-ef-core-package | 5 | 13min | 2.6min |

**Recent Trend:**
- Last 5 plans: 03-01 (3min), 03-02 (2min), 03-03 (2min), 03-04 (3min), 03-05 (3min)
- Trend: Stable - consistent 2-3 minute execution times across phase 3

*Updated after each plan completion*

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- Multi-package structure over monolith (allows consumers to include only needed dependencies)
- Core package has zero dependencies (enables maximum reusability)
- .NET 10 only, no multi-targeting (simplifies maintenance)
- Dependency-based packaging (Core has no deps, others reference Core as needed)
- Central Package Management for all projects (eliminates version conflicts) - 01-01
- AOT/Trimming analysis enabled by default (future compatibility) - 01-01
- Starting version at 0.0.1 (signals early alpha status) - 01-01
- Conditional IsPackable based on project naming patterns (automatic packaging control) - 01-01
- Adopted Imagile.Framework.* naming convention following Microsoft patterns - 01-02
- Organized packages under src/ directory for clean separation - 01-02
- Testing packages use .Testing suffix instead of .Tests - 01-02
- Three separate packages with dependency chain: Core → EntityFrameworkCore, Core → Blazor.ApplicationInsights - 02-01
- GitHub Actions workflow triggered by version tags for NuGet publishing - 02-01
- AssociatedAttribute is NOT sealed - serves as base for RequiresAttribute and IncludesAttribute (architectural pattern) - 02-02
- AllowMultiple=true on AssociatedAttribute enables multi-type associations - 02-02
- Property aliases (Required, Included) provide semantic clarity over base Associated property - 02-02
- Enhanced XML documentation beyond source with detailed remarks and examples - 02-04
- Marker attribute pattern (no properties) for validation exclusion - 02-04
- IAuditContextProvider in Core package for zero-dependency audit context abstraction - 03-01
- Explicit interface properties over shadow properties for testability - 03-01
- Generic TUserKey/TTenantKey parameters for flexible identifier types - 03-01
- ITenantEntity independent of audit hierarchy for composability - 03-01
- DateTimeOffset for proper timezone handling in distributed systems - 03-01
- HideValueChanges parameter on [Auditable] for sensitive data tracking without exposing values - 03-02
- [IgnoreAudit] marker attribute with optional Reason for explicit exclusion documentation - 03-02
- Both attributes sealed with Inherited=true for consistency and subclass support - 03-02
- Two-level opt-in: entity implements IEntityChangeAuditable + properties have [Auditable] - 03-02
- Two-table audit design: EntityChange header + EntityChangeProperty details for efficient querying - 03-03
- TransactionUnique groups changes within same SaveChanges call - 03-03
- CorrelationId links audit records to distributed tracing - 03-03
- String storage for old/new values enables universal property type support - 03-03
- AreValuesHidden flag on EntityChangeProperty supports sensitive data tracking - 03-03
- ParentEntityName/ParentItemId enable hierarchical change tracking - 03-03
- SaveChanges override (not ISaveChangesInterceptor) for simpler state management - 03-04
- Two-phase save pattern: capture changes BEFORE base.SaveChanges, persist AFTER - 03-04
- Reflection-based interface checking for generic type parameter matching - 03-04
- ISO 8601 formatting for DateTimeOffset/DateTime in audit logs - 03-04
- Extension methods provide fluent API for soft delete operations - 03-05
- Query extensions use DbContext constraint to work with any derived context - 03-05
- Configuration helpers called from derived DbContext OnModelCreating for opt-in filtering - 03-05
- Tenant filter requires expression parameter - wiring is opt-in per application needs - 03-05

### Pending Todos

None yet.

### Blockers/Concerns

None yet.

## Session Continuity

Last session: 2026-01-25 22:21 UTC
Stopped at: Completed 03-05-PLAN.md (Phase 3 complete)
Resume file: None
Next: Phase 4 or 5 (see ROADMAP.md)

---
*State initialized: 2026-01-25*
*Last updated: 2026-01-25*
