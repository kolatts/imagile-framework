---
phase: 04-blazor-package
plan: 01
subsystem: telemetry
tags: [blazor, wasm, application-insights, telemetry, js-interop, azure-monitor]

# Dependency graph
requires:
  - phase: 01-foundation-infrastructure
    provides: Build system, package structure, central package management
provides:
  - Complete Application Insights SDK interface definitions (IApplicationInsights, IAppInsights, IDependenciesPlugin, IPropertiesPlugin)
  - All telemetry model classes for event, exception, trace, metric, pageview, and dependency tracking
  - Configuration models for SDK setup (Config, Configuration)
  - Context models for telemetry metadata (Application, Device, Location, Session, User, etc.)
  - CookieManager for browser cookie management
  - Full type definitions for JS interop with Application Insights JavaScript SDK
affects: [04-02, 04-03, 04-04, blazor-telemetry-consumers]

# Tech tracking
tech-stack:
  added:
    - Application Insights model types
    - System.Text.Json serialization attributes
  patterns:
    - JsonPropertyName attributes for JS SDK compatibility
    - Abstract base classes (PartC) for shared telemetry properties
    - Custom JSON converters (TimeSpanJsonConverter, DateTimeJsonConverter)
    - Context object pattern for telemetry metadata

key-files:
  created:
    - src/Imagile.Framework.Blazor.ApplicationInsights/Interfaces/IApplicationInsights.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Interfaces/IAppInsights.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Interfaces/IDependenciesPlugin.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Interfaces/IPropertiesPlugin.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/Config.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/Configuration.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/CookieManager.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/EventTelemetry.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/ExceptionTelemetry.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/TraceTelemetry.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/MetricTelemetry.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/PageViewTelemetry.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/PageViewPerformanceTelemetry.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/DependencyTelemetry.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/TelemetryItem.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/TelemetryContext.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/SeverityLevel.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/Error.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/PartC.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/Context/Application.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/Context/Device.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/Context/Internal.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/Context/Location.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/Context/OperatingSystem.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/Context/Session.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/Context/SessionManager.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/Context/TelemetryTrace.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/Context/UserContext.cs
    - src/Imagile.Framework.Blazor.ApplicationInsights/Models/Context/Web.cs
  modified: []

key-decisions:
  - "Renamed CookieMgr to CookieManager following framework naming standards (no abbreviations)"
  - "Preserved all JsonPropertyName attributes for JavaScript SDK compatibility"
  - "Enhanced XML documentation beyond source repository"
  - "Used System.Text.Json with custom converters for Application Insights data format compatibility"

patterns-established:
  - "PartC abstract base class pattern for telemetry with custom properties"
  - "Custom JSON converters for TimeSpan (general format) and DateTime (Unix milliseconds)"
  - "Context models in separate namespace (Models.Context) for organization"
  - "Interface composition pattern (IApplicationInsights inherits IAppInsights, IDependenciesPlugin, IPropertiesPlugin)"

# Metrics
duration: 8min
completed: 2026-01-25
---

# Phase 04 Plan 01: Interfaces and Models Migration Summary

**Complete Application Insights type system migrated with 4 interfaces, 15 core models, and 10 context models for Blazor WASM telemetry**

## Performance

- **Duration:** 8 min
- **Started:** 2026-01-25T23:34:49Z
- **Completed:** 2026-01-25T23:43:05Z
- **Tasks:** 3
- **Files created:** 29

## Accomplishments

- Migrated complete Application Insights SDK interface definitions from source repository
- Established full telemetry type system (events, exceptions, traces, metrics, pageviews, dependencies)
- Created comprehensive context models for telemetry metadata (application, device, location, session, user, web)
- Package compiles successfully with zero errors, ready for implementation in Plan 02

## Task Commits

Each task was committed atomically:

1. **Task 1: Migrate Interfaces** - `30438f7` (feat)
   - IApplicationInsights, IAppInsights, IDependenciesPlugin, IPropertiesPlugin
   - Updated namespaces and renamed CookieMgr → CookieManager

2. **Task 2: Migrate Core Models** - `85cb637` (feat)
   - 15 core model files: Config, Configuration, telemetry types, base types
   - Preserved JsonPropertyName attributes for JS interop

3. **Task 3: Migrate Context Models and Verify Build** - `a755c64` (feat)
   - 10 context models in Models/Context subdirectory
   - Build verification successful

## Files Created/Modified

### Interfaces (4 files)
- `Interfaces/IApplicationInsights.cs` - Main telemetry API with Flush, authentication, and configuration methods
- `Interfaces/IAppInsights.cs` - Core tracking methods (TrackEvent, TrackPageView, TrackException, TrackTrace, TrackMetric, TrackDependencyData)
- `Interfaces/IDependenciesPlugin.cs` - Dependency tracking interface
- `Interfaces/IPropertiesPlugin.cs` - Context property access interface

### Core Models (15 files)
- `Models/Config.cs` - Comprehensive SDK configuration with 50+ settings
- `Models/Configuration.cs` - Base configuration class
- `Models/CookieManager.cs` - Cookie management wrapper for JS interop (renamed from CookieMgr)
- `Models/EventTelemetry.cs` - Custom event tracking
- `Models/ExceptionTelemetry.cs` - Exception tracking with severity
- `Models/TraceTelemetry.cs` - Diagnostic trace logging
- `Models/MetricTelemetry.cs` - Performance metric tracking
- `Models/PageViewTelemetry.cs` - Page view tracking
- `Models/PageViewPerformanceTelemetry.cs` - Page load performance with custom TimeSpan converter
- `Models/DependencyTelemetry.cs` - External dependency tracking with custom DateTime converter
- `Models/TelemetryItem.cs` - Telemetry envelope
- `Models/TelemetryContext.cs` - Context container
- `Models/SeverityLevel.cs` - Severity enum (Verbose, Information, Warning, Error, Critical)
- `Models/Error.cs` - Exception details
- `Models/PartC.cs` - Abstract base for custom properties

### Context Models (10 files)
- `Models/Context/Application.cs` - Application version info
- `Models/Context/Device.cs` - Device and screen info
- `Models/Context/Internal.cs` - SDK version metadata
- `Models/Context/Location.cs` - IP address info
- `Models/Context/OperatingSystem.cs` - OS info
- `Models/Context/Session.cs` - Session tracking
- `Models/Context/SessionManager.cs` - Automatic session management
- `Models/Context/TelemetryTrace.cs` - Distributed tracing (W3C TraceContext)
- `Models/Context/UserContext.cs` - User and authentication info
- `Models/Context/Web.cs` - Browser and domain info

## Decisions Made

**1. Renamed CookieMgr to CookieManager**
- Rationale: Framework convention prohibits abbreviations in class names for clarity
- Impact: Improved readability, consistent with project standards

**2. Preserved all JsonPropertyName attributes**
- Rationale: Required for JavaScript SDK compatibility and correct serialization
- Impact: Ensures data sent to Application Insights matches expected schema

**3. Enhanced XML documentation beyond source**
- Rationale: Framework standard requires comprehensive documentation
- Impact: Better IntelliSense experience for consumers, clearer API usage

**4. Custom JSON converters for TimeSpan and DateTime**
- Rationale: Application Insights expects specific formats (TimeSpan as general format string, DateTime as Unix milliseconds)
- Impact: Correct serialization for JS SDK consumption

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None - migration completed successfully with no blocking issues. Build warnings about commented-out "todo" code are expected and documented in source.

## User Setup Required

None - no external service configuration required. This plan only establishes type definitions.

## Next Phase Readiness

**Ready for Plan 02 (Implementation)**
- All interfaces defined and ready for implementation
- All model types available for data transfer
- Package structure established
- Zero compilation errors

**Blockers:** None

**Notes:**
- Implementation (Plan 02) will add ApplicationInsights service class implementing IApplicationInsights
- JavaScript interop files will be added in Plan 03
- Service registration extensions will be added in Plan 04

---
*Phase: 04-blazor-package*
*Completed: 2026-01-25*
