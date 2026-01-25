# Imagile Framework

## What This Is

A multi-package C# framework library providing reusable, opinionated components for rapid development of modern .NET applications. Organized by dependencies to allow à la carte consumption - use only what you need. Targets EF Core patterns, Azure integrations, and Blazor telemetry.

## Core Value

Enable rapid creation of production-ready C# applications by providing battle-tested conventions and patterns that enforce consistency while remaining flexible enough for both personal projects and enterprise work.

## Requirements

### Validated

- ✓ EF Core convention testing utilities package exists and is functional — existing
- ✓ Fluent exclusion configuration for convention rules — existing
- ✓ Naming convention rules (tables, properties, foreign keys, primary keys) — existing
- ✓ Design convention rules (primary key types, nullable handling, string lengths) — existing
- ✓ In-memory database testing infrastructure — existing
- ✓ NuGet package publishing via GitHub Actions — existing
- ✓ .NET 10 target framework — existing

### Active

- [ ] Create Imagile.Framework.Core package (zero dependencies)
- [ ] Extract declarative attributes from imagile-app (RequiresAttribute, AssociatedAttribute, etc.)
- [ ] Move domain patterns and conventions to Core package
- [ ] Create Imagile.Framework.EntityFrameworkCore package
- [ ] Build audit logging system inspired by Arcoro.One EntityChangeProperty
- [ ] Move existing Tests utilities to EntityFrameworkCore.Tests package
- [ ] Migrate Imagile.BlazorApplicationInsights into framework as separate package
- [ ] Update package naming from Imagile.EntityFrameworkCore.* to Imagile.Framework.*
- [ ] Set up dependency structure (Core has no deps, others reference Core as needed)
- [ ] Update NuGet publishing to handle multiple packages
- [ ] Create comprehensive documentation for each package

### Out of Scope

- Multi-framework targeting (only .NET 10+) — focusing on latest .NET only
- Non-Azure cloud providers in initial release — may add AWS/GCP later as separate packages
- Opinionated UI component library — Blazor patterns only, not full component system
- Database-specific packages beyond EF Core — no Dapper, ADO.NET wrappers, etc.

## Context

**Brownfield project:** Existing Imagile.EntityFrameworkCore.Tests package is functional and published. Repository currently has testing utilities with ~953 lines of codebase documentation.

**Source material:**
- imagile-app repository contains reusable patterns to extract (attributes in Domain project)
- Arcoro.One repository has audit logging patterns to adapt (EntityChangeProperty, EntityChange)
- Imagile.BlazorApplicationInsights standalone repo to migrate into framework

**Target consumers:**
- imagile-app template (will consume framework packages instead of local implementations)
- Personal B2B SaaS projects for rapid prototyping
- Work projects needing specific components (EF Core conventions, audit logging, etc.)
- Open source community wanting opinionated .NET patterns

**Current state:**
- Single solution with Tests package
- GitVersion configured for semantic versioning
- GitHub Actions workflow for NuGet publishing
- Sample projects demonstrating convention testing

## Constraints

- **Tech stack**: .NET 10 — using latest .NET version
- **Architecture**: Core package must have zero dependencies — other packages can reference Core as needed
- **Package organization**: Organize by dependency requirements, not by feature size — prioritize consumer simplicity
- **Versioning**: GitVersion with mainline strategy — automated semantic versioning
- **Distribution**: NuGet.org public feed — all packages publicly available

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Multi-package structure over monolith | Allows consumers to include only needed dependencies (e.g., Blazor project doesn't need EF Core) | — Pending |
| Core package has zero dependencies | Enables maximum reusability without forcing dependency baggage on consumers | — Pending |
| .NET 10 only (no multi-targeting) | Simplifies maintenance, encourages use of latest framework features | — Pending |
| Dependency-based packaging (not feature-based) | Core (no deps), EntityFrameworkCore (EF deps), BlazorApplicationInsights (Blazor deps) matches consumption patterns | — Pending |

---
*Last updated: 2026-01-25 after initialization*
