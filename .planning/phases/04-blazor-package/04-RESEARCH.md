# Phase 4: Blazor Package - Research

**Researched:** 2026-01-25
**Domain:** Blazor WebAssembly Application Insights Integration via JavaScript Interop
**Confidence:** HIGH

## Summary

This phase migrates the existing Imagile.BlazorApplicationInsights repository into the framework as a standalone package. The migration involves adapting a proven WASM-focused Application Insights integration that uses JS interop to communicate with the Application Insights JavaScript SDK.

The standard approach for Blazor WASM is JS interop with the Application Insights JavaScript SDK rather than the .NET SDK, because Blazor WASM runs in the browser where the JavaScript SDK is designed to operate. The existing codebase provides a complete, battle-tested implementation including automatic page tracking, custom event support, telemetry initializers, and logger integration.

**Primary recommendation:** Migrate the existing Imagile.BlazorApplicationInsights codebase wholesale, adapting only namespaces, package structure, and documentation to match framework conventions. The architecture is sound and proven in production.

## Standard Stack

The established libraries/tools for Blazor WASM Application Insights integration:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Application Insights JS SDK | 3.x (via CDN) | Client-side telemetry | Official Microsoft SDK for browser-based telemetry, loaded via snippet |
| Microsoft.AspNetCore.Components.WebAssembly | 10.0.0 | Blazor WASM runtime | Required for Blazor components and DI |
| Microsoft.JSInterop | 10.0.0 (via ASP.NET) | JS interop bridge | Standard mechanism for C# to JS communication in Blazor |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Microsoft.ApplicationInsights | 2.22.0 | .NET telemetry types | OPTIONAL - Only if using server-side telemetry alongside WASM |
| Microsoft.Extensions.Logging | 10.0.0 (via ASP.NET) | Logging abstraction | For ILoggerProvider integration to send logs to App Insights |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| JS SDK via interop | .NET Application Insights SDK | .NET SDK doesn't work in browser context - WASM requires JS SDK |
| CDN snippet injection | NPM package with bundling | Snippet is lighter and recommended by Microsoft; NPM adds build complexity |
| Community package | Build from scratch | Community package (BlazorApplicationInsights) is proven and maintained |

**Installation:**
```bash
# Package will reference these via Central Package Management
dotnet add package Microsoft.AspNetCore.Components.WebAssembly
# Microsoft.ApplicationInsights is optional - only for shared model types
```

## Architecture Patterns

### Recommended Project Structure
```
src/Imagile.Framework.Blazor.ApplicationInsights/
├── Components/
│   └── ApplicationInsightsInit.razor(.cs)  # Initialization component
├── Interfaces/
│   ├── IApplicationInsights.cs             # Main API interface
│   ├── IAppInsights.cs                     # Core tracking methods
│   ├── IDependenciesPlugin.cs              # Dependency tracking
│   └── IPropertiesPlugin.cs                # Property plugin
├── Logging/
│   ├── ApplicationInsightsLogger.cs        # ILogger implementation
│   ├── ApplicationInsightsLoggerProvider.cs # ILoggerProvider
│   └── ApplicationInsightsLoggerOptions.cs  # Logger configuration
├── Models/
│   ├── Config.cs                           # JS SDK configuration
│   ├── Configuration.cs                    # Base config
│   ├── TelemetryItem.cs                    # Telemetry envelope
│   ├── EventTelemetry.cs                   # Event model
│   ├── ExceptionTelemetry.cs               # Exception model
│   └── [Other telemetry models]
├── wwwroot/
│   └── BlazorApplicationInsights.lib.module.js  # JS interop helpers
├── ApplicationInsights.cs                  # Main implementation
├── ApplicationInsightsInitConfig.cs        # DI configuration
└── IServiceCollectionExtensions.cs         # AddBlazorApplicationInsights()
```

### Pattern 1: JS Interop via Razor Component for SDK Initialization
**What:** Inject the Application Insights JavaScript SDK snippet during component initialization, then wire up IJSRuntime for API calls
**When to use:** Always - required for WASM scenarios
**Why:** The JS SDK must be loaded before any telemetry can be sent; component lifecycle ensures proper initialization timing
**Example:**
```csharp
// Source: Imagile.BlazorApplicationInsights ApplicationInsightsInit.razor.cs
protected override void OnInitialized()
{
    // Generate JS snippet with connection string from config
    script = $"""<script>/* AI SDK snippet */cfg: {JsonSerializer.Serialize(Config.Config)}</script>""";
}

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        ApplicationInsights.InitJSRuntime(JSRuntime);
        await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/PackageName/module.js");
        await ApplicationInsights.TrackPageView();
    }
}
```

### Pattern 2: Wrapper Service with IJSRuntime for Telemetry API
**What:** C# service that wraps all JavaScript SDK methods via IJSRuntime.InvokeVoidAsync/InvokeAsync
**When to use:** For all telemetry tracking operations
**Why:** Provides strongly-typed C# API over JavaScript SDK, enables DI injection
**Example:**
```csharp
// Source: Imagile.BlazorApplicationInsights ApplicationInsights.cs
public class ApplicationInsights : IApplicationInsights
{
    private IJSRuntime _jsRuntime;

    public async Task TrackEvent(EventTelemetry @event)
        => await _jsRuntime.InvokeVoidAsync("appInsights.trackEvent", @event);

    public async Task TrackException(ExceptionTelemetry exception)
        => await _jsRuntime.InvokeVoidAsync("appInsights.trackException", exception);
}
```

### Pattern 3: Telemetry Initializer via Factory Function
**What:** Pass TelemetryItem envelope to JS, which creates a closure that augments all telemetry with custom properties
**When to use:** For adding context like cloud role, tenant ID, custom dimensions to all telemetry
**Why:** Centralized property injection without manually adding to every tracking call
**Example:**
```csharp
// Source: BlazorApplicationInsights sample Program.cs
var telemetryItem = new TelemetryItem()
{
    Tags = new Dictionary<string, object?>()
    {
        { "ai.cloud.role", "SPA" },
        { "ai.cloud.roleInstance", "Blazor Wasm" },
    }
};
await applicationInsights.AddTelemetryInitializer(telemetryItem);

// JS side (BlazorApplicationInsights.lib.module.js)
addTelemetryInitializer: function (telemetryItem) {
    var telemetryInitializer = (envelope) => {
        if (telemetryItem.tags !== null) {
            Object.assign(envelope.tags, telemetryItem.tags);
        }
    };
    appInsights.addTelemetryInitializer(telemetryInitializer);
}
```

### Pattern 4: NavigationManager.LocationChanged for Page View Tracking
**What:** Subscribe to NavigationManager.LocationChanged event to automatically track page views on route changes
**When to use:** For automatic page view tracking in Blazor WASM apps
**Why:** Blazor routing doesn't reload the page, so browser navigation events don't fire; NavigationManager event detects Blazor route changes
**Example:**
```csharp
// Pattern from research (not in current implementation, but standard approach)
@inject NavigationManager NavigationManager
@inject IApplicationInsights AppInsights
@implements IDisposable

protected override void OnInitialized()
{
    NavigationManager.LocationChanged += OnLocationChanged;
}

private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
{
    await AppInsights.TrackPageView(new PageViewTelemetry
    {
        Name = e.Location,
        Uri = e.Location
    });
}

public void Dispose()
{
    NavigationManager.LocationChanged -= OnLocationChanged;
}
```

### Pattern 5: Configuration from appsettings.json in WASM
**What:** Load connection string from wwwroot/appsettings.json via IConfiguration
**When to use:** For non-sensitive configuration values in WASM apps
**Why:** Standard configuration pattern in Blazor WASM; allows environment-specific config
**Security note:** appsettings.json in wwwroot is visible to clients - connection strings are safe here (not secrets)
**Example:**
```csharp
// Standard Blazor WASM configuration loading
builder.Services.AddBlazorApplicationInsights(config =>
{
    var connectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    config.ConnectionString = connectionString;
});
```

### Pattern 6: ILoggerProvider Integration for Automatic Log Shipping
**What:** Custom ILoggerProvider that sends all ILogger calls to Application Insights via TrackTrace
**When to use:** WASM-only (disabled for Server) to centralize logging
**Why:** Allows existing logging code to automatically ship to App Insights without refactoring
**Example:**
```csharp
// Source: Imagile.BlazorApplicationInsights ApplicationInsightsLoggerProvider.cs
public class ApplicationInsightsLoggerProvider : ILoggerProvider
{
    private readonly IApplicationInsights _appInsights;

    public ILogger CreateLogger(string categoryName)
    {
        return new ApplicationInsightsLogger(_appInsights, categoryName);
    }
}

// In logger implementation
public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
    Exception? exception, Func<TState, Exception?, string> formatter)
{
    var message = formatter(state, exception);
    await _appInsights.TrackTrace(new TraceTelemetry
    {
        Message = message,
        SeverityLevel = MapLogLevel(logLevel)
    });
}
```

### Anti-Patterns to Avoid
- **Using .NET Application Insights SDK directly in WASM:** The .NET SDK is designed for server-side scenarios and doesn't work in browser context. Use JS SDK via interop.
- **Hard-coding connection strings in components:** Use configuration abstraction (IConfiguration or DI) for environment-specific values
- **Forgetting to dispose NavigationManager.LocationChanged subscriptions:** Causes memory leaks as components won't be GC'd
- **Enabling ILoggerProvider on Blazor Server:** Redundant - Server apps should use standard .NET Application Insights integration
- **Storing secrets in wwwroot/appsettings.json:** These files are publicly accessible; connection strings are safe but API keys/tokens are not

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| JS SDK snippet injection | Custom script loading logic | Microsoft's official snippet from docs | Snippet includes fallback CDNs, error handling, version management |
| Telemetry envelope serialization | Manual JSON construction | Pass C# objects to IJSRuntime | JSRuntime handles serialization correctly, including null handling and JSON naming |
| Page view duration tracking | Manual stopwatch/timer | StartTrackPage/StopTrackPage methods | JS SDK calculates timing accurately with browser performance API |
| Automatic exception tracking | Manual try/catch everywhere | JS SDK's autoExceptionTracking + ErrorBoundary | JS SDK hooks window.onerror globally; ErrorBoundary catches component errors |
| Correlation headers for distributed tracing | Manual header injection | JS SDK's distributedTracingMode config | SDK automatically manages W3C trace context and AI correlation headers |
| Telemetry buffering and batching | Custom queue/timer logic | JS SDK's built-in batching | SDK optimizes network calls with configurable batch size and interval |
| Browser storage for offline scenarios | Custom localStorage implementation | JS SDK's enableSessionStorageBuffer | SDK manages storage quota, serialization, and recovery on reconnection |

**Key insight:** The Application Insights JavaScript SDK is a sophisticated client-side telemetry system with years of production hardening. It handles browser quirks, network failures, performance optimization, and privacy compliance. Wrapping it via interop leverages this maturity while providing type-safe C# APIs.

## Common Pitfalls

### Pitfall 1: Not Handling JSRuntime Initialization Timing
**What goes wrong:** Calling telemetry methods before IJSRuntime is initialized throws exceptions
**Why it happens:** IJSRuntime isn't available until after component renders; attempting to track events in OnInitialized() fails
**How to avoid:** Initialize IJSRuntime in OnAfterRenderAsync(firstRender: true), not OnInitialized()
**Warning signs:** "Cannot call InvokeAsync before the JavaScript runtime is initialized" exception

### Pitfall 2: Memory Leaks from NavigationManager Event Subscriptions
**What goes wrong:** Components subscribing to LocationChanged never get garbage collected
**Why it happens:** NavigationManager is a singleton that holds strong references to event subscribers; components must explicitly unsubscribe
**How to avoid:** Implement IDisposable and unsubscribe in Dispose() method
**Warning signs:** Increasing memory usage over time, components remaining in memory after navigation

### Pitfall 3: Connection String Visibility Concerns
**What goes wrong:** Developers worry about exposing connection strings in client-side code
**Why it happens:** Misunderstanding of Application Insights security model
**How to avoid:** Understand that connection strings are not secrets - they're designed for client-side use. The ingestion endpoint is rate-limited and doesn't expose data.
**Warning signs:** Attempting to store connection string server-side and fetch it via API (unnecessary complexity)
**Reference:** [Connection strings in Application Insights - Microsoft Learn](https://learn.microsoft.com/en-us/azure/azure-monitor/app/connection-strings) - "The connection string isn't considered a security token or key."

### Pitfall 4: Enabling Telemetry Before SDK Loads
**What goes wrong:** TrackPageView() called before JS SDK finishes loading results in lost events
**Why it happens:** The SDK snippet loads asynchronously; C# code may execute before SDK is ready
**How to avoid:** The snippet provides a queue mechanism - early calls are queued and replayed. However, for initial page view, call TrackPageView() in OnAfterRenderAsync after import completes.
**Warning signs:** First page view not appearing in Application Insights portal

### Pitfall 5: Incorrect Render Mode for ApplicationInsightsInit Component
**What goes wrong:** Component doesn't initialize in certain Blazor hosting models
**Why it happens:** Component requires interactive rendering to execute C# code; static rendering doesn't run OnAfterRenderAsync
**How to avoid:** Use @rendermode="@InteractiveAuto" or @InteractiveWebAssembly on ApplicationInsightsInit component
**Warning signs:** No telemetry appearing, component's C# code not executing

### Pitfall 6: Not Flushing Telemetry Before Page Unload
**What goes wrong:** Telemetry sent just before navigation may be lost
**Why it happens:** Batched telemetry waits in buffer; browser may terminate JavaScript before batch sends
**How to avoid:** JS SDK's onunloadDisableBeacon configuration (default false) uses Beacon API for reliable shutdown telemetry
**Warning signs:** Missing telemetry for navigation events, form submissions before redirect

### Pitfall 7: Using EnableAutoRouteTracking Without Understanding WASM Limitations
**What goes wrong:** Expecting automatic page tracking without manual implementation
**Why it happens:** The JS SDK's enableAutoRouteTracking watches browser history API, but in WASM the navigation is client-side only
**How to avoid:** For Blazor WASM, use NavigationManager.LocationChanged event to explicitly call TrackPageView()
**Warning signs:** No page view events appearing for route changes

### Pitfall 8: Conflicting Telemetry Initializers
**What goes wrong:** Multiple initializers overwriting each other's tags or properties
**Why it happens:** Telemetry initializers run in order of addition; later ones using assignment instead of merge overwrite earlier values
**How to avoid:** Use Object.assign() in JS initializers to merge properties rather than replace; order matters
**Warning signs:** Custom properties appearing inconsistently or missing entirely

## Code Examples

Verified patterns from official sources and existing implementation:

### DI Registration with Configuration
```csharp
// Source: Imagile.BlazorApplicationInsights samples/Program.cs
builder.Services.AddBlazorApplicationInsights(config =>
{
    config.ConnectionString = "InstrumentationKey=xxx;IngestionEndpoint=https://...";
    config.EnableAutoRouteTracking = true; // Note: Requires manual wiring in WASM
},
async applicationInsights =>
{
    // Optional initialization callback for telemetry initializers
    var telemetryItem = new TelemetryItem()
    {
        Tags = new Dictionary<string, object?>()
        {
            { "ai.cloud.role", "SPA" },
            { "ai.cloud.roleInstance", "Blazor Wasm" },
        }
    };
    await applicationInsights.AddTelemetryInitializer(telemetryItem);
});
```

### Component Usage for Custom Events
```csharp
// Source: Imagile.BlazorApplicationInsights samples/TestComponents.razor.cs
@inject IApplicationInsights AppInsights

private async Task TrackCustomEvent()
{
    await AppInsights.TrackEvent(new EventTelemetry()
    {
        Name = "ButtonClicked",
        Properties = new Dictionary<string, object?>()
        {
            { "buttonId", "submit" },
            { "timestamp", DateTime.UtcNow }
        }
    });
    await AppInsights.Flush(); // Optional immediate send
}

private async Task TrackException()
{
    await AppInsights.TrackException(new ExceptionTelemetry()
    {
        Exception = new() { Message = "my message", Name = "ValidationError" },
        SeverityLevel = SeverityLevel.Error,
        Properties = new Dictionary<string, object?>()
        {
            { "userId", userId }
        }
    });
}
```

### Component Initialization
```razor
<!-- Source: Imagile.BlazorApplicationInsights README.md -->
<!-- In App.razor, add after <base href="/" /> -->
<ApplicationInsightsInit IsWasmStandalone="true" />

<!-- For Blazor Web App (not standalone WASM): -->
<ApplicationInsightsInit @rendermode="@InteractiveAuto" />
```

### JavaScript Module for Extended Functionality
```javascript
// Source: Imagile.BlazorApplicationInsights wwwroot/BlazorApplicationInsights.lib.module.js
window.blazorApplicationInsights = {
    addTelemetryInitializer: function (telemetryItem) {
        var telemetryInitializer = (envelope) => {
            if (telemetryItem.tags !== null) {
                Object.assign(envelope.tags, telemetryItem.tags);
            }
            if (telemetryItem.data !== null) {
                Object.assign(envelope.data, telemetryItem.data);
            }
        };
        appInsights.addTelemetryInitializer(telemetryInitializer);
    },

    trackDependencyData: function (dependencyTelemetry) {
        // Convert C# DateTime to JS Date
        if (dependencyTelemetry.startTime !== null) {
            dependencyTelemetry.startTime = new Date(dependencyTelemetry.startTime);
        }
        appInsights.trackDependencyData(dependencyTelemetry);
    },

    getContext: function () {
        if (appInsights.context !== undefined) {
            return appInsights.context
        }
    }
};
```

### ILogger Integration
```csharp
// Source: Imagile.BlazorApplicationInsights samples
@inject ILogger<MyComponent> Logger

private async Task DoWork()
{
    Logger.LogInformation("Processing started");
    Logger.LogWarning("Validation warning: {message}", warningMessage);
    Logger.LogError(exception, "Operation failed");

    // Logs automatically sent to Application Insights via ILoggerProvider
    await AppInsights.Flush(); // Optional: force immediate send
}
```

### Error Boundary Integration (Future Enhancement)
```razor
<!-- Pattern from research - not in current implementation -->
<!-- Source: Blazor error boundary documentation -->
<ErrorBoundary @ref="errorBoundary">
    <ChildContent>
        <Router AppAssembly="@typeof(App).Assembly">
            <!-- Routes -->
        </Router>
    </ChildContent>
    <ErrorContent Context="exception">
        <div>An error occurred</div>
    </ErrorContent>
</ErrorBoundary>

@code {
    private ErrorBoundary? errorBoundary;

    protected override void OnParametersSet()
    {
        errorBoundary?.Recover();
    }

    // Hook error boundary to send exceptions to App Insights
    // This would require extending ErrorBoundary or using global error handler
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Instrumentation Key | Connection String | SDK v2.3.0 (2020) | Connection strings include ingestion endpoint and other configuration; more flexible than IKey alone |
| Manual SDK loading | Snippet-based injection | Always recommended | Snippet includes CDN fallbacks and automatic retry; more resilient |
| Server-side .NET SDK for WASM | JS SDK via interop | N/A | .NET SDK doesn't work in browser; JS SDK is designed for client |
| enableAutoRouteTracking in WASM | Manual NavigationManager tracking | Current | JS SDK's auto-tracking watches history API, but Blazor routing needs manual wiring |
| InstrumentationKey property | ConnectionString property | SDK 2.3.0+ | Migration path: replace `instrumentationKey` config field with `connectionString` |
| Nullable strings in telemetry models | Required strings with null-conditional | .NET 10 | Nullable reference types enabled; better null safety |

**Deprecated/outdated:**
- **Instrumentation Key configuration:** Still works but connection strings are recommended. Connection strings provide ingestion endpoint configuration and are more future-proof.
- **disableTelemetry + instrumentationKey in snippet:** Modern approach uses connectionString; disableTelemetry can be set via Config object in C#
- **Blazor Server specific implementation in existing package:** Current implementation mixes Server and WASM patterns; framework version should focus on WASM as primary use case (per requirements)

## Open Questions

Things that couldn't be fully resolved:

1. **NavigationManager.LocationChanged automatic tracking**
   - What we know: Existing package has EnableAutoRouteTracking config, but implementation doesn't show NavigationManager wiring
   - What's unclear: Whether auto-tracking "just works" via JS SDK or requires manual implementation
   - Recommendation: Implement explicit NavigationManager.LocationChanged → TrackPageView wiring as part of migration. Place in MainLayout or App component with proper disposal.

2. **Server vs. WASM support priority**
   - What we know: Requirements specify "WASM use cases primary, Server support secondary" (BLAZOR-11)
   - What's unclear: Whether to maintain Server compatibility at all in framework package
   - Recommendation: Focus exclusively on WASM. If Server support is needed later, create separate Imagile.Framework.Blazor.ApplicationInsights.Server package. Clean separation avoids complexity.

3. **TelemetryClient vs. IApplicationInsights naming**
   - What we know: Existing package uses IApplicationInsights; .NET SDK uses TelemetryClient
   - What's unclear: Whether framework should align naming with .NET SDK conventions or maintain existing interface naming
   - Recommendation: Keep IApplicationInsights naming - it's clear, not tied to implementation, and avoids confusion with server-side TelemetryClient. Document relationship in XML comments.

4. **Exception tracking mechanism**
   - What we know: JS SDK has autoExceptionTracking; ErrorBoundary exists in Blazor
   - What's unclear: How to wire ErrorBoundary exceptions to Application Insights automatically
   - Recommendation: Phase 4 should include automatic exception tracking via global error handler (AppDomain.UnhandledException, TaskScheduler.UnobservedTaskException) and provide example of ErrorBoundary integration in documentation.

5. **Static web asset configuration**
   - What we know: Package uses Sdk="Microsoft.NET.Sdk.Razor" for wwwroot support
   - What's unclear: Whether StaticWebAssetFingerprintingEnabled should be true/false for framework package
   - Recommendation: Research shows .NET 10 has improved static asset fingerprinting. Set to true for cache-busting benefits unless testing reveals issues.

## Sources

### Primary (HIGH confidence)
- Imagile.BlazorApplicationInsights repository at C:\Code\Imagile.BlazorApplicationInsights - examined source code, samples, and architecture
- [Application Insights JavaScript SDK - Microsoft Learn](https://learn.microsoft.com/en-us/azure/azure-monitor/app/javascript-sdk) - official SDK documentation
- [Connection strings in Application Insights - Microsoft Learn](https://learn.microsoft.com/en-us/azure/azure-monitor/app/connection-strings) - connection string configuration
- [ASP.NET Core Blazor configuration - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/configuration?view=aspnetcore-9.0) - appsettings.json patterns
- [Handle errors in ASP.NET Core Blazor apps - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/handle-errors?view=aspnetcore-10.0) - ErrorBoundary usage

### Secondary (MEDIUM confidence)
- [GitHub - IvanJosipovic/BlazorApplicationInsights](https://github.com/IvanJosipovic/BlazorApplicationInsights) - original upstream project
- [Blazor University - Detecting navigation events](https://blazor-university.com/routing/detecting-navigation-events/) - NavigationManager patterns
- [ASP.NET Core Blazor navigation - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/navigation?view=aspnetcore-10.0) - navigation and routing
- [ApplicationInsights-JS API Reference - GitHub](https://github.com/microsoft/ApplicationInsights-JS/blob/master/API-reference.md) - JavaScript SDK API
- [Blazor WebAssembly Exception Handling With Error Boundaries - Code Maze](https://code-maze.com/blazor-webassembly-exception-handling-error-boundaries/) - error boundary patterns

### Tertiary (LOW confidence)
- [Performance Tuning in ASP.NET Core: Best Practices for 2026 - Syncfusion](https://www.syncfusion.com/blogs/post/performance-tuning-in-aspnetcore-2026) - .NET 10 AOT recommendations
- [Blazor & .NET 10: A Developer's Guide - Gap Velocity](https://www.gapvelocity.ai/blog/blazor-dotnet-10-a-developers-guide) - .NET 10 Blazor features
- [NuGet Gallery - Microsoft.AspNetCore.Components.WebAssembly 10.0.0](https://www.nuget.org/packages/Microsoft.AspNetCore.Components.WebAssembly/) - package reference

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - examined existing working implementation, verified with official docs
- Architecture: HIGH - existing codebase provides complete reference implementation with proven patterns
- Pitfalls: MEDIUM - documented from official sources and existing code, but NavigationManager auto-tracking specifics need verification during implementation
- Don't hand-roll: HIGH - JS SDK features well-documented in official Microsoft docs

**Research date:** 2026-01-25
**Valid until:** 2026-02-25 (30 days - stable domain, but .NET/Blazor moves quickly)

**Key migration notes:**
- Source repository uses namespace `Imagile.BlazorApplicationInsights` → framework will use `Imagile.Framework.Blazor.ApplicationInsights`
- Source uses `BlazorApplicationInsights` in file paths → framework will standardize to package name
- Source includes Blazor Server samples → framework migration should focus on WASM samples only (per requirements)
- Source package is net10.0 already → no upgrade needed
- Source uses Microsoft.ApplicationInsights 2.22.0 which is already in framework's Directory.Packages.props
