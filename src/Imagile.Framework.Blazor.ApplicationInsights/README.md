# Imagile.Framework.Blazor.ApplicationInsights

Application Insights telemetry integration for Blazor WebAssembly with automatic page tracking, custom event support, exception tracking, and ILogger integration.

## Installation

```bash
dotnet add package Imagile.Framework.Blazor.ApplicationInsights
```

## Features

- **Automatic Page View Tracking** - Track route changes and page navigation automatically
- **Custom Event Tracking** - Send custom telemetry events with properties
- **Exception Tracking** - Capture and log exceptions with error boundary integration
- **ILogger Integration** - Route standard .NET logging to Application Insights
- **Custom Telemetry Initializers** - Modify telemetry before sending (add custom properties, filter events)
- **Authenticated User Context** - Track user identity and account information

## Usage Examples

### Basic Setup with Automatic Page Tracking

Configure Application Insights in your Blazor WASM `Program.cs`:

```csharp
using Imagile.Framework.Blazor.ApplicationInsights;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Add Application Insights with connection string
builder.Services.AddBlazorApplicationInsights(config =>
{
    config.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";
    config.EnableAutoRouteTracking = true; // Track page navigation automatically
});

await builder.Build().RunAsync();
```

**Add the initialization component to your App.razor or MainLayout.razor:**

```razor
@using Imagile.Framework.Blazor.ApplicationInsights.Components

<ApplicationInsightsInit />

@* Your app content *@
<Router AppAssembly="@typeof(Program).Assembly">
    ...
</Router>
```

The `<ApplicationInsightsInit />` component loads the JavaScript SDK and initializes telemetry tracking.

### Configuration from appsettings.json

Use configuration file instead of inline configuration:

**appsettings.json:**
```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=...;IngestionEndpoint=...",
    "EnableAutoRouteTracking": true
  }
}
```

**Program.cs:**
```csharp
builder.Services.AddBlazorApplicationInsights(builder.Configuration);
```

### Custom Event Tracking

Track custom business events with properties:

```csharp
@inject IApplicationInsights AppInsights
@using Imagile.Framework.Blazor.ApplicationInsights.Interfaces
@using Imagile.Framework.Blazor.ApplicationInsights.Models

@code {
    private async Task OnButtonClick()
    {
        // Track custom event with properties
        await AppInsights.TrackEvent(new EventTelemetry
        {
            Name = "ButtonClicked",
            Properties = new Dictionary<string, object?>
            {
                { "buttonId", "submit" },
                { "page", "checkout" },
                { "cartItems", 3 }
            }
        });
    }

    private async Task OnFormSubmit()
    {
        // Track conversion or business event
        await AppInsights.TrackEvent(new EventTelemetry
        {
            Name = "OrderSubmitted",
            Properties = new Dictionary<string, object?>
            {
                { "orderId", 12345 },
                { "total", 149.99m },
                { "paymentMethod", "CreditCard" }
            },
            Measurements = new Dictionary<string, double>
            {
                { "orderTotal", 149.99 },
                { "itemCount", 3 }
            }
        });
    }
}
```

### Exception Tracking with Error Boundary

Capture and log exceptions automatically:

**ErrorBoundary integration:**
```razor
<ErrorBoundary @ref="errorBoundary">
    <ChildContent>
        @Body
    </ChildContent>
    <ErrorContent Context="exception">
        <p class="error-message">An error occurred.</p>
        @code {
            // Exception automatically sent to Application Insights via ILogger integration
        }
    </ErrorContent>
</ErrorBoundary>

@code {
    private ErrorBoundary? errorBoundary;

    protected override void OnParametersSet()
    {
        errorBoundary?.Recover();
    }
}
```

**Manual exception tracking:**
```csharp
@inject IApplicationInsights AppInsights

@code {
    private async Task RiskyOperation()
    {
        try
        {
            // Your code here
            await PerformDatabaseOperation();
        }
        catch (Exception ex)
        {
            // Track exception with custom properties
            await AppInsights.TrackException(new ExceptionTelemetry
            {
                Exception = ex,
                SeverityLevel = SeverityLevel.Error,
                Properties = new Dictionary<string, object?>
                {
                    { "operation", "DatabaseSync" },
                    { "userId", CurrentUserId }
                }
            });
            throw;
        }
    }
}
```

### ILogger Integration

Standard .NET logging automatically flows to Application Insights:

```csharp
@inject ILogger<MyComponent> Logger

@code {
    protected override async Task OnInitializedAsync()
    {
        // Logs automatically sent to Application Insights
        Logger.LogInformation("Component initialized for user {UserId}", userId);
        Logger.LogWarning("Slow API response detected: {Duration}ms", duration);
        Logger.LogError(ex, "Failed to load data for {EntityType}", entityType);
    }
}
```

**Configure logging levels in Program.cs:**
```csharp
builder.Services.AddBlazorApplicationInsights(
    config => config.ConnectionString = "...",
    loggingOptions: options =>
    {
        options.MinLogLevel = LogLevel.Information; // Only log Information and above
        options.IncludeScopes = true; // Include logging scopes in telemetry
    });
```

### Authenticated User Context

Track authenticated users for user-level analytics:

```csharp
builder.Services.AddBlazorApplicationInsights(
    config => config.ConnectionString = "...",
    onAppInsightsInit: async appInsights =>
    {
        // Called after Application Insights SDK initializes
        var userId = await GetCurrentUserIdAsync();
        if (!string.IsNullOrEmpty(userId))
        {
            await appInsights.SetAuthenticatedUserContext(
                authenticatedUserId: userId,
                accountId: "tenant-123", // Optional tenant/account ID
                storeInCookie: true // Persist across page reloads
            );
        }
    });
```

**Clear authenticated context on logout:**
```csharp
@inject IApplicationInsights AppInsights

@code {
    private async Task OnLogout()
    {
        await AppInsights.ClearAuthenticatedUserContext();
        // Proceed with logout
    }
}
```

### Custom Telemetry Initializers

Modify all telemetry before sending (add custom properties, filter events):

```csharp
@inject IApplicationInsights AppInsights
@using Imagile.Framework.Blazor.ApplicationInsights.Models

@code {
    protected override async Task OnInitializedAsync()
    {
        // Add telemetry initializer to attach custom properties to all events
        await AppInsights.AddTelemetryInitializer(new TelemetryInitializer
        {
            InitializerFunction = "(item) => { " +
                "item.data = item.data || {}; " +
                "item.data.appVersion = '1.2.3'; " +
                "item.data.environment = 'production'; " +
            "}"
        });
    }
}
```

## Configuration Options

**Config class properties:**
- `ConnectionString` - Application Insights connection string (required)
- `EnableAutoRouteTracking` - Enable automatic page view tracking on route changes (default: false)
- `DisableTelemetry` - Disable telemetry collection (useful for testing)
- `SamplingPercentage` - Percentage of telemetry to sample (1-100, default: 100)
- `MaxBatchInterval` - Maximum time (ms) to wait before sending batch (default: 15000)
- `MaxBatchSizeInBytes` - Maximum batch size in bytes (default: 102400)

## Key Types

### Services
- **IApplicationInsights** - Main telemetry API (TrackEvent, TrackException, TrackPageView, etc.)

### Models
- **EventTelemetry** - Custom event with properties and measurements
- **ExceptionTelemetry** - Exception data with severity and custom properties
- **PageViewTelemetry** - Page view event with URI and duration
- **TraceTelemetry** - Trace/log message
- **Config** - Application Insights SDK configuration

### Components
- **ApplicationInsightsInit** - Initialization component that loads JavaScript SDK

## Dependencies

Requires **Imagile.Framework.Core** package.

## License

MIT License - see LICENSE file for details.

## Contributing

Contributions are welcome! Please open an issue or pull request on GitHub.

## Repository

https://github.com/imagile/imagile-framework
