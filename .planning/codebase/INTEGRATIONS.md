# External Integrations

**Analysis Date:** 2026-01-25

## APIs & External Services

**No external APIs:** This is a library package focused on convention testing for Entity Framework Core. It does not integrate with remote APIs or external services.

## Data Storage

**Databases:**
- SQLite (in-memory) - Used exclusively for testing via `Microsoft.Data.Sqlite 10.0.0`
  - Connection: Test-created in-memory connections (no persistent storage)
  - Client: Entity Framework Core DbContext with SQLite provider
  - Used in: `Imagile.EntityFrameworkCore.Tests.Infrastructure.InMemoryDatabaseTest<TContext>` base class

**File Storage:**
- Local filesystem only - No remote file storage integration

**Caching:**
- None - No caching layers or services configured

## Authentication & Identity

**Auth Provider:**
- Custom/None - This is a testing library; no authentication mechanism is implemented
  - Library consumers are responsible for authentication in their own DbContexts

## Monitoring & Observability

**Error Tracking:**
- None - No error tracking service integration (Sentry, ApplicationInsights, etc.)

**Logs:**
- Console output only - Test failures are reported via xUnit test framework output
- FluentAssertions provides detailed assertion failure messages

## CI/CD & Deployment

**Hosting:**
- GitHub - Repository and action runners
- NuGet.org - Package distribution platform

**CI Pipeline:**
- GitHub Actions (`.github/workflows/`)
  - **CI Workflow** (`ci.yml`): Runs on push to main/develop and all PRs
    - Tests across .NET 9.0.x and 10.0.x
    - Runs on ubuntu-latest runner
    - Executes sample test suite
  - **Publish NuGet Workflow** (`publish-nuget.yml`): Triggers on version tags or manual dispatch
    - Uses GitVersion for semantic versioning
    - Packs and publishes to NuGet API
    - Requires `NUGET_API_KEY` secret

## Environment Configuration

**Required env vars:**
- `NUGET_API_KEY` - GitHub Actions secret for publishing to NuGet.org (configured in repository secrets)

**Secrets location:**
- GitHub repository secrets - Used for NuGet API key in publish workflow

**Build environment:**
- Uses multi-version .NET setup with `actions/setup-dotnet@v4`
- Supports .NET 9.0.x and 10.0.x side-by-side

## Webhooks & Callbacks

**Incoming:**
- GitHub push/PR webhooks - Standard GitHub workflow triggers
  - `push` events on `main` and `develop` branches
  - `pull_request` events targeting `main` and `develop`
  - Workflow dispatch for manual trigger

**Outgoing:**
- NuGet.org publish API - Package upload endpoint via `dotnet nuget push`

## NuGet Package Metadata

**Package Details:**
- Package ID: `Imagile.EntityFrameworkCore.Tests`
- License: MIT (via `PackageLicenseExpression`)
- Repository: https://github.com/imagile/imagile-entityframeworkcore
- Project URL: https://github.com/imagile/imagile-entityframeworkcore
- Tags: ef-core, entity-framework, testing, conventions, xunit
- README: Included in package from root `README.md`

---

*Integration audit: 2026-01-25*
