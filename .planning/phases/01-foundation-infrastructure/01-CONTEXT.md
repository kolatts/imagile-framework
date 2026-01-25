# Phase 1: Foundation & Infrastructure - Context

**Gathered:** 2026-01-25
**Status:** Ready for planning

<domain>
## Phase Boundary

Establish multi-package project structure with proper naming conventions, build infrastructure (Central Package Management, Directory.Build.props), versioning automation (GitVersion), and enable XML documentation + Native AOT analysis for all framework packages. This is the foundation layer that all subsequent packages will build upon.

</domain>

<decisions>
## Implementation Decisions

### Package Naming Structure
- Core package: `Imagile.Framework.Core`
- EF Core package: `Imagile.Framework.EntityFrameworkCore`
- Testing package: `Imagile.Framework.EntityFrameworkCore.Testing`
- Blazor package: `Imagile.Framework.Blazor.ApplicationInsights`
- All packages use full descriptive names (no abbreviations)
- Blazor package gets namespace segment (`.Blazor.`) to group related packages

### Version Strategy
- Unified versioning: All packages share the same version number
- Initial version: `0.0.1` (signals early alpha, increment to 1.0.0 when production-ready)
- GitVersion strategy: Mainline (trunk-based) - every commit to main increments version
- Simple continuous delivery approach

### Build Configuration
- Package metadata in `Directory.Build.props` (shared: authors, license, repository URL)
- Central Package Management via `Directory.Packages.props` (all external dependency versions centralized)
- XML documentation generated for ALL packages (Core, EF Core, Blazor, Testing)
- Native AOT/trimming analysis enabled for all framework packages

### Migration Strategy
- Create new `Imagile.Framework.sln` solution file (fresh start)
- Remove existing `Samples/` projects (focus on core packages first)

### Claude's Discretion
- How to handle existing Imagile.EntityFrameworkCore.Tests package migration (rename in place vs create fresh)
- Exact structure of Directory.Build.props and Directory.Packages.props
- GitVersion.yml configuration details

</decisions>

<specifics>
## Specific Ideas

- Package naming follows Microsoft patterns (full names like `Microsoft.EntityFrameworkCore`, not abbreviations)
- Blazor package uses namespace segment to allow future growth (e.g., could add `Imagile.Framework.Blazor.Components` later)
- Start at 0.0.1 to signal experimental/alpha status, gives flexibility to iterate before 1.0.0

</specifics>

<deferred>
## Deferred Ideas

None - discussion stayed within phase scope

</deferred>

---

*Phase: 01-foundation-infrastructure*
*Context gathered: 2026-01-25*
