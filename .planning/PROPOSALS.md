# Framework Improvement Proposals

*Created 2026-09-01 from a review of imagile-app patterns, the current framework state, and the enum-driven vision. Ordered by leverage.*

## Vision alignment

The framework's loop: **enum + attributes → seeded schema → audited entities → convention tests → UI/verification**. Every proposal below strengthens a link in that loop. The strongest finding from imagile-app: the enum and EF layers are already partially extracted and consumed as NuGet packages, but the entire Playwright end-to-end layer and the reference-data sync engine remain app-local and are proven, generalizable code.

## P1 — New package: Imagile.Framework.Testing.EndToEnd

The highest-value extraction. imagile-app has a complete, battle-tested Playwright + Reqnroll e2e stack that is ~90% app-agnostic. Extract into a new package (Playwright + Reqnroll optional via a companion `*.Reqnroll` package so plain-xUnit users don't take the dependency):

- **`PlaywrightFixture`** — one browser per run, double-checked `SemaphoreSlim` lazy init, config-driven browser choice, viewport/video/mobile emulation (`Fixtures/PlaywrightFixture.cs`).
- **`BrowserContext`** — per-scenario isolated context + page, optional tracing, console collection, network diagnostics, `CaptureScreenshotOnFailureAsync`, `SaveTraceAsync` (`Fixtures/BrowserContext.cs`).
- **`PageObjectBase`** (from `AppPageBase`) — abstract `RelativePath`, explicit `GoToAsync()` (no constructor navigation: deadlocks under xUnit's sync context), `NetworkIdle` wait. Include the Blazor-specific wait pattern: pushState navigation means "wait for the login form to detach", not "wait for navigation".
- **`TestContextBag`** (from `AppTestContext`) — scenario-scoped typed bag (`Set<T>/Get<T>`), current user/tenant/viewport state.
- **Viewport profiles** — desktop/mobile profile switch with `IsMobile`/`HasTouch`/`UserAgent`, overridable via `Playwright__Profile` env var; run both into `TestResults/desktop` and `TestResults/mobile`.
- **Hooks** — failure capture (screenshot + HTML + console + trace), tag-driven skips (`@DesktopOnly`, `@MobileOnly`, `@LocalOnly` as the mutation-safety gate), report post-processing on `ProcessExit`.
- **DI spine** — the `[ScenarioDependencies]` composition (singleton fixture, scoped context, lazy `IPage` factory) as a ready-made `AddEndToEndTesting(configuration)` extension.
- **CI readiness gate** — the polling loop (`/health/ready` + real auth round-trip) as a reusable workflow or a documented pattern.

Known fixes to make while extracting: `MobileAppSmokeTests` uses `SkippableFact` only via a transitive Reqnroll dependency (add the explicit package), and the postdeployment README is stale (write fresh docs).

## P2 — Generalize the reference-data sync engine into EntityFrameworkCore

imagile-app's `ReferenceDataExtensions.SyncReferenceDataAsync<TEntity, TId>` is the "enum → table" materialization engine: diff seed data vs DB, add/update/delete-orphan, honor `[DoNotUpdate]`, `deleteFilter` to preserve user-owned rows in mixed tables, `SyncResult(Added, Updated, Deleted)` reporting. The only blocker is that it's typed to `TenantDbContext` — retype to `DbContext` and ship it beside the existing `SyncEnumsAsync`/upsert work. Bring with it:

- **`IReferenceDataEntity<TSelf, TId>`** with `static abstract IEnumerable<TSelf> GetSeedData()` (C# 11 static abstracts) — the entity declares its own seed set, typically projected from an enum.
- **Dependency-ordered seeding** (from `ReferenceDataSeeder`) plus attribute-derived join tables (`[DefaultSecurityRoles]` + recursive `GetIncluded()` → role-permission rows). The join-derivation pattern generalizes to any "enum A grants enum B" relationship.
- **JSON loader escape hatch** for 500+ row reference sets (`JsonReferenceDataLoader` pattern).

## P3 — Pull the remaining convention rules into EntityFrameworkCore.Testing

imagile-app still hand-rolls rules the package should own: `Properties_ShouldMatchSqlColumns`, `Id`-not-`ID` casing on keys, no `Entity` suffix on entity names, non-nullable strings default to `string.Empty`, pluralized `DbSet` names, FK-matches-navigation-name. Each is a straightforward `IConventionRule`. This makes the package the single source of database readability rules and lets imagile-app delete its local copies.

Also extract `InMemoryDatabaseTest`'s multi-context sweep (expose `Contexts` as a list so convention LINQ can check all contexts at once) — the framework's version currently exists but should absorb the app's refinements.

## P4 — Enum layer hardening in Core

- **Cache attribute lookups.** Every `GetAttributeFirstOrDefault` call does reflection; a `static ConcurrentDictionary<(Type, TEnum), TAttribute?>` per attribute type makes enum metadata effectively free and AOT-friendly. Hot-path concern for permission checks that call `GetIncluded()` per request.
- **`GetDescriptionOrThrow`** — imagile-app added it; adopt it (fail fast when a seeded column would silently fall back to the member name).
- **Banded-value convention support** — imagile-app uses value bands (negative = system, 1000+ = tenant, 5000+ = edit permissions). Add an optional `[ValueBand]`/analyzer-style convention test so bands are checkable, not tribal knowledge.
- **Source generator (later)** — replace runtime reflection with a generator emitting switch-based metadata lookups; zero reflection, fully AOT/trim safe.

## P5 — In-process API integration tier (gap in both repos)

imagile-app references `Microsoft.AspNetCore.Mvc.Testing` but never uses it — there is no test tier between unit tests and browser-vs-deployed-URL. A thin `Imagile.Framework.Testing.Api` (WebApplicationFactory base with the framework's audit context provider faked, SQLite-in-memory wired to `InMemoryDatabaseTest`) fills the pyramid's middle and would be immediately useful to imagile-app.

## P6 — Housekeeping

- **Drop the unused `Microsoft.ApplicationInsights` PackageReference** from the Blazor package: no source file uses the SDK (all telemetry is JS interop). Removing it shrinks the consumer dependency graph. (Held pending confirmation since it changes the published package's dependency surface.)
- **Package versioning holds** (now encoded in the maintenance workflow): FluentAssertions stays 7.x (8.x is commercially licensed), ApplicationInsights stays 2.x, xunit stays 2.x until a deliberate v3 migration.
- **Finish Phase 7** (READMEs per package, repository README) — the public site now exists and should link to/from them.

## Suggested sequence

1. P3 (small, pure wins, unblocks imagile-app deleting local rules)
2. P2 (core of the enum→database story)
3. P1 (biggest package, do it as its own phase with plans)
4. P4 caching + `GetDescriptionOrThrow` (quick), source generator later
5. P5 after P1 (shares fixtures)
