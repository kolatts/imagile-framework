using Imagile.Framework.Blazor.ApplicationInsights.Interfaces;
using Imagile.Framework.Blazor.ApplicationInsights.Models;
using Microsoft.JSInterop;

namespace Imagile.Framework.Blazor.ApplicationInsights;

/// <inheritdoc />
public class ApplicationInsights : IApplicationInsights
{
    private IJSRuntime? _jsRuntime;

    /// <inheritdoc />
    public bool IsInitialized => _jsRuntime is not null;

    /// <inheritdoc />
    public void InitJSRuntime(IJSRuntime jSRuntime)
    {
        _jsRuntime = jSRuntime;
    }

    /// <inheritdoc />
    public async Task TrackPageView(PageViewTelemetry? pageView = null)
    {
        if (!IsInitialized) return;
        await _jsRuntime!.InvokeVoidAsync("appInsights.trackPageView", pageView);
    }

    /// <inheritdoc />
    public async Task TrackEvent(EventTelemetry @event)
    {
        if (!IsInitialized) return;
        await _jsRuntime!.InvokeVoidAsync("appInsights.trackEvent", @event);
    }

    /// <inheritdoc />
    public async Task TrackTrace(TraceTelemetry trace)
    {
        if (!IsInitialized) return;
        await _jsRuntime!.InvokeVoidAsync("appInsights.trackTrace", trace);
    }

    /// <inheritdoc />
    public async Task TrackException(ExceptionTelemetry exception)
    {
        if (!IsInitialized) return;
        await _jsRuntime!.InvokeVoidAsync("appInsights.trackException", exception);
    }

    /// <inheritdoc />
    public async Task StartTrackPage(string? name = null)
    {
        if (!IsInitialized) return;
        await _jsRuntime!.InvokeVoidAsync("appInsights.startTrackPage", name!);
    }

    /// <inheritdoc />
    public async Task StopTrackPage(string? name = null, string? url = null, Dictionary<string, object?>? customProperties = null, Dictionary<string, decimal>? measurements = null)
    {
        if (!IsInitialized) return;
        await _jsRuntime!.InvokeVoidAsync("appInsights.stopTrackPage", name, url, customProperties, measurements);
    }

    /// <inheritdoc />
    public async Task TrackMetric(MetricTelemetry metric)
    {
        if (!IsInitialized) return;
        await _jsRuntime!.InvokeVoidAsync("appInsights.trackMetric", metric);
    }

    /// <inheritdoc />
    public async Task TrackDependencyData(DependencyTelemetry dependency)
    {
        if (!IsInitialized) return;
        await _jsRuntime!.InvokeVoidAsync("blazorApplicationInsights.trackDependencyData", dependency);
    }

    /// <inheritdoc />
    public async Task Flush()
    {
        if (!IsInitialized) return;
        await _jsRuntime!.InvokeVoidAsync("appInsights.flush");
    }

    /// <inheritdoc />
    public async Task ClearAuthenticatedUserContext()
    {
        if (!IsInitialized) return;
        await _jsRuntime!.InvokeVoidAsync("appInsights.clearAuthenticatedUserContext");
    }

    /// <inheritdoc />
    public async Task SetAuthenticatedUserContext(string authenticatedUserId, string? accountId = null, bool? storeInCookie = null)
    {
        if (!IsInitialized) return;
        await _jsRuntime!.InvokeVoidAsync("appInsights.setAuthenticatedUserContext", authenticatedUserId, accountId, storeInCookie);
    }

    /// <inheritdoc />
    public async Task AddTelemetryInitializer(TelemetryItem telemetryItem)
    {
        if (!IsInitialized) return;
        await _jsRuntime!.InvokeVoidAsync("blazorApplicationInsights.addTelemetryInitializer", telemetryItem);
    }

    /// <inheritdoc />
    public async Task TrackPageViewPerformance(PageViewPerformanceTelemetry pageViewPerformance)
    {
        if (!IsInitialized) return;
        await _jsRuntime!.InvokeVoidAsync("appInsights.trackPageViewPerformance", pageViewPerformance);
    }

    /// <inheritdoc />
    public async Task StartTrackEvent(string name)
    {
        if (!IsInitialized) return;
        await _jsRuntime!.InvokeVoidAsync("appInsights.startTrackEvent", name);
    }

    /// <inheritdoc />
    public async Task StopTrackEvent(string name, Dictionary<string, object?>? properties = null, Dictionary<string, decimal>? measurements = null)
    {
        if (!IsInitialized) return;
        await _jsRuntime!.InvokeVoidAsync("appInsights.stopTrackEvent", name, properties, measurements);
    }

    /// <inheritdoc />
    public async Task UpdateCfg(Config newConfig, bool mergeExisting = true)
    {
        if (!IsInitialized) return;
        await _jsRuntime!.InvokeVoidAsync("appInsights.updateCfg", newConfig, mergeExisting);
    }

    /// <inheritdoc />
    public async Task<TelemetryContext> Context()
    {
        if (!IsInitialized) return new TelemetryContext();
        return await _jsRuntime!.InvokeAsync<TelemetryContext>("blazorApplicationInsights.getContext");
    }

    /// <inheritdoc />
    public CookieManager GetCookieMgr()
    {
        return new CookieManager(_jsRuntime!);
    }
}
