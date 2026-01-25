---
phase: 02-core-package
plan: 01
subsystem: infrastructure
tags: [nuget, packaging, project-structure, github-actions]
requires:
  - 01-02 # Project restructure with multi-package layout
provides:
  - Core package structure (zero dependencies)
  - EntityFrameworkCore package structure (references Core)
  - Blazor.ApplicationInsights package structure (references Core)
  - Automated NuGet publishing workflow
affects:
  - 02-02 # Future Core package implementation
  - 02-03 # Future EntityFrameworkCore implementation
  - 02-04 # Future Blazor.ApplicationInsights implementation
tech-stack:
  added:
    - Microsoft.ApplicationInsights@2.22.0
    - Microsoft.AspNetCore.Components.WebAssembly@10.0.0
  patterns:
    - Multi-package NuGet structure
    - Zero-dependency core package pattern
    - GitHub Actions continuous delivery
decisions:
  - key: multi-package-structure
    choice: "Three separate packages: Core (zero deps), EntityFrameworkCore (Core + EF deps), Blazor.ApplicationInsights (Core + Blazor/AppInsights deps)"
    rationale: "Allows consumers to include only needed dependencies"
  - key: nuget-publishing-automation
    choice: "GitHub Actions workflow triggered by version tags"
    rationale: "Enables continuous delivery to NuGet.org with manual override capability"
key-files:
  created:
    - src/Imagile.Framework.Core/Imagile.Framework.Core.csproj
    - src/Imagile.Framework.EntityFrameworkCore/Imagile.Framework.EntityFrameworkCore.csproj
    - src/Imagile.Framework.Blazor.ApplicationInsights/Imagile.Framework.Blazor.ApplicationInsights.csproj
    - .github/workflows/publish-nuget.yml
  modified:
    - Directory.Packages.props
    - Imagile.Framework.sln
metrics:
  duration: 5min
  completed: 2026-01-25
---

# Phase 2 Plan 01: Package Structure Setup Summary

**One-liner:** Multi-package NuGet structure with Core (zero deps), EntityFrameworkCore, and Blazor.ApplicationInsights packages plus GitHub Actions automated publishing.

## What Was Built

Created the complete multi-package framework structure with three NuGet packages and automated publishing:

### 1. Core Package (Zero Dependencies)
- Created `src/Imagile.Framework.Core/` with project file
- Configured as zero-dependency package for maximum reusability
- Package metadata: "Zero-dependency declarative attributes for building opinionated .NET applications"
- Tags: attributes, validation, metadata, declarative, enum, framework

### 2. EntityFrameworkCore Package
- Created `src/Imagile.Framework.EntityFrameworkCore/` with project file
- References Core package via ProjectReference
- References Microsoft.EntityFrameworkCore via PackageReference
- Package metadata: "EF Core audit logging with automatic timestamps, user tracking, and property-level change tracking"
- Tags: entityframeworkcore, efcore, audit, changetracking, softdelete, framework

### 3. Blazor.ApplicationInsights Package
- Created `src/Imagile.Framework.Blazor.ApplicationInsights/` with project file
- Uses Microsoft.NET.Sdk.Razor for Blazor support
- References Core package via ProjectReference
- References Microsoft.ApplicationInsights and Microsoft.AspNetCore.Components.WebAssembly
- Package metadata: "Application Insights telemetry integration for Blazor WebAssembly with automatic page tracking and custom event support"
- Tags: blazor, webassembly, applicationinsights, telemetry, monitoring, framework

### 4. Central Package Management
- Added Microsoft.ApplicationInsights@2.22.0 to Directory.Packages.props
- Added Microsoft.AspNetCore.Components.WebAssembly@10.0.0 to Directory.Packages.props
- All package versions centrally managed for consistency

### 5. Solution Integration
- Added all three framework projects to Imagile.Framework.sln
- Solution now contains 4 projects total (including EntityFrameworkCore.Testing)
- Full solution builds successfully in both Debug and Release configurations

### 6. Automated NuGet Publishing
- Created GitHub Actions workflow at `.github/workflows/publish-nuget.yml`
- Triggers on version tags (v*.*.*)
- Manual trigger via workflow_dispatch
- Builds solution in Release mode
- Packs all three framework packages
- Publishes to NuGet.org using NUGET_API_KEY secret
- Skips duplicates for safe re-runs

## Technical Decisions

### Zero-Dependency Core Pattern
**Decision:** Core package has absolutely zero PackageReference elements.

**Rationale:** Enables maximum reusability without forcing dependency baggage on consumers. Any project can reference Core without worrying about dependency conflicts or bloat.

**Implementation:** Core.csproj contains no PackageReference ItemGroup, only ProjectReference for README.md packaging.

### Dependency-Based Package Organization
**Decision:** Organize packages by dependency requirements rather than feature size or domain.

**Rationale:** Matches consumption patterns - consumers can include only the packages whose dependencies they already have. A Blazor project doesn't need EF Core packages, an API project doesn't need Blazor packages.

**Implementation:**
- Core: No dependencies (attributes, markers, interfaces)
- EntityFrameworkCore: Core + EF Core dependencies (audit, change tracking)
- Blazor.ApplicationInsights: Core + Blazor + App Insights dependencies (telemetry)

### GitHub Actions for Publishing
**Decision:** Use GitHub Actions workflow triggered by version tags for automated publishing.

**Rationale:** Provides continuous delivery capability while maintaining control through tag-based releases. Manual workflow_dispatch allows emergency publishes if needed.

**Implementation:** Single workflow that builds, packs, and publishes all three packages to NuGet.org. Requires NUGET_API_KEY secret to be configured in GitHub repository.

## Verification Results

All verification criteria passed:

✅ **Full solution builds:** `dotnet build Imagile.Framework.sln` succeeds with all 4 projects
✅ **Core has zero dependencies:** No PackageReference elements in Core.csproj
✅ **EntityFrameworkCore references Core:** ProjectReference to Core exists
✅ **Blazor.ApplicationInsights references Core:** ProjectReference to Core exists
✅ **All packages can be created:** Successfully created .nupkg for all three framework packages
✅ **AOT analysis enabled:** Build shows trim/AOT warnings only in Testing project (expected)
✅ **GitHub Actions workflow exists:** `.github/workflows/publish-nuget.yml` created
✅ **Workflow includes all packages:** Contains dotnet pack commands for all three framework packages

### Build Output
- Debug build: 0 errors, 2 warnings (AOT warnings in Testing project only)
- Release build: 0 errors, 2 warnings (AOT warnings in Testing project only)
- Package creation: All three packages successfully created as .nupkg files

### Package Verification
- Core: Imagile.Framework.Core.1.0.0.nupkg
- EntityFrameworkCore: Imagile.Framework.EntityFrameworkCore.1.0.0.nupkg
- Blazor.ApplicationInsights: Imagile.Framework.Blazor.ApplicationInsights.1.0.0.nupkg

## File Inventory

### Created Files
- `src/Imagile.Framework.Core/Imagile.Framework.Core.csproj` (28 lines)
- `src/Imagile.Framework.EntityFrameworkCore/Imagile.Framework.EntityFrameworkCore.csproj` (35 lines)
- `src/Imagile.Framework.Blazor.ApplicationInsights/Imagile.Framework.Blazor.ApplicationInsights.csproj` (38 lines)
- `.github/workflows/publish-nuget.yml` (41 lines)

### Modified Files
- `Directory.Packages.props` - Added 2 package versions
- `Imagile.Framework.sln` - Added 3 project references

### Directory Structure
```
src/
├── Imagile.Framework.Core/
│   ├── Attributes/        # Created for future attribute implementations
│   └── Imagile.Framework.Core.csproj
├── Imagile.Framework.EntityFrameworkCore/
│   └── Imagile.Framework.EntityFrameworkCore.csproj
├── Imagile.Framework.Blazor.ApplicationInsights/
│   └── Imagile.Framework.Blazor.ApplicationInsights.csproj
└── Imagile.Framework.EntityFrameworkCore.Testing/
    └── (existing)

.github/
└── workflows/
    ├── ci.yml                    # Existing
    └── publish-nuget.yml        # New
```

## Deviations from Plan

None - plan executed exactly as written.

## Next Phase Readiness

### Blockers
None identified.

### Concerns
None at this time.

### Prerequisites Met
- ✅ All project files created and buildable
- ✅ Zero-dependency Core package structure established
- ✅ Package dependency graph validated (EntityFrameworkCore → Core, Blazor.ApplicationInsights → Core)
- ✅ Central package management configured
- ✅ Automated publishing infrastructure ready

### Ready For
- **02-02:** Core package implementation (attributes, markers, interfaces)
- **02-03:** EntityFrameworkCore package implementation (audit logging, change tracking)
- **02-04:** Blazor.ApplicationInsights package implementation (telemetry integration)

### Notes for Future Plans
- User must add `NUGET_API_KEY` to GitHub repository secrets before first publish
- Version numbering currently defaults to 1.0.0 - consider integrating GitVersion in future plan
- README.md is currently repo-level - may want package-specific READMEs in future
- AOT warnings in Testing project are expected (EF Core EnsureCreated/EnsureDeleted not AOT-compatible)

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| 1 | ca35a42 | feat(02-01): create all framework project structures |
| 2 | 14de7a6 | chore(02-01): add Blazor and Application Insights package versions |
| 3 | db52f36 | feat(02-01): add all framework projects to solution |
| 4 | e93373c | feat(02-01): add GitHub Actions workflow for NuGet publishing |

---
*Executed: 2026-01-25*
*Duration: ~5 minutes*
*Status: Complete*
