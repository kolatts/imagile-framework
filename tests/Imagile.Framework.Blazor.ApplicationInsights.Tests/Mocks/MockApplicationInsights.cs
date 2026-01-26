using Imagile.Framework.Blazor.ApplicationInsights.Interfaces;
using Imagile.Framework.Blazor.ApplicationInsights.Models;
using Microsoft.JSInterop;

namespace Imagile.Framework.Blazor.ApplicationInsights.Tests.Mocks;

/// <summary>
/// Mock implementation of IApplicationInsights for testing components that inject telemetry.
/// Records all telemetry method calls for verification.
/// </summary>
public class MockApplicationInsights : IApplicationInsights
{
    private readonly List<TelemetryCall> _calls = new();

    /// <summary>
    /// Gets all recorded telemetry calls.
    /// </summary>
    public IReadOnlyList<TelemetryCall> Calls => _calls;

    /// <summary>
    /// Clears all recorded calls.
    /// </summary>
    public void Clear()
    {
        _calls.Clear();
    }

    public void InitJSRuntime(IJSRuntime jSRuntime)
    {
        _calls.Add(new TelemetryCall(nameof(InitJSRuntime), jSRuntime));
    }

    public Task AddTelemetryInitializer(TelemetryItem telemetryItem)
    {
        _calls.Add(new TelemetryCall(nameof(AddTelemetryInitializer), telemetryItem));
        return Task.CompletedTask;
    }

    public Task ClearAuthenticatedUserContext()
    {
        _calls.Add(new TelemetryCall(nameof(ClearAuthenticatedUserContext), null));
        return Task.CompletedTask;
    }

    public Task<TelemetryContext> Context()
    {
        _calls.Add(new TelemetryCall(nameof(Context), null));
        return Task.FromResult(new TelemetryContext());
    }

    public Task Flush()
    {
        _calls.Add(new TelemetryCall(nameof(Flush), null));
        return Task.CompletedTask;
    }

    public CookieManager GetCookieMgr()
    {
        _calls.Add(new TelemetryCall(nameof(GetCookieMgr), null));
        return new CookieManager(new MockJSRuntime());
    }

    public Task SetAuthenticatedUserContext(string authenticatedUserId, string? accountId = null, bool? storeInCookie = null)
    {
        _calls.Add(new TelemetryCall(nameof(SetAuthenticatedUserContext), new { authenticatedUserId, accountId, storeInCookie }));
        return Task.CompletedTask;
    }

    public Task StartTrackEvent(string name)
    {
        _calls.Add(new TelemetryCall(nameof(StartTrackEvent), name));
        return Task.CompletedTask;
    }

    public Task StartTrackPage(string? name = null)
    {
        _calls.Add(new TelemetryCall(nameof(StartTrackPage), name));
        return Task.CompletedTask;
    }

    public Task StopTrackEvent(string name, Dictionary<string, object?>? properties = null, Dictionary<string, decimal>? measurements = null)
    {
        _calls.Add(new TelemetryCall(nameof(StopTrackEvent), new { name, properties, measurements }));
        return Task.CompletedTask;
    }

    public Task StopTrackPage(string? name = null, string? url = null, Dictionary<string, object?>? properties = null, Dictionary<string, decimal>? measurements = null)
    {
        _calls.Add(new TelemetryCall(nameof(StopTrackPage), new { name, url, properties, measurements }));
        return Task.CompletedTask;
    }

    public Task TrackDependencyData(DependencyTelemetry dependency)
    {
        _calls.Add(new TelemetryCall(nameof(TrackDependencyData), dependency));
        return Task.CompletedTask;
    }

    public Task TrackEvent(EventTelemetry eventTelemetry)
    {
        _calls.Add(new TelemetryCall(nameof(TrackEvent), eventTelemetry));
        return Task.CompletedTask;
    }

    public Task TrackException(ExceptionTelemetry exception)
    {
        _calls.Add(new TelemetryCall(nameof(TrackException), exception));
        return Task.CompletedTask;
    }

    public Task TrackMetric(MetricTelemetry metric)
    {
        _calls.Add(new TelemetryCall(nameof(TrackMetric), metric));
        return Task.CompletedTask;
    }

    public Task TrackPageView(PageViewTelemetry? pageView = null)
    {
        _calls.Add(new TelemetryCall(nameof(TrackPageView), pageView));
        return Task.CompletedTask;
    }

    public Task TrackPageViewPerformance(PageViewPerformanceTelemetry pageViewPerformance)
    {
        _calls.Add(new TelemetryCall(nameof(TrackPageViewPerformance), pageViewPerformance));
        return Task.CompletedTask;
    }

    public Task TrackTrace(TraceTelemetry traceTelemetry)
    {
        _calls.Add(new TelemetryCall(nameof(TrackTrace), traceTelemetry));
        return Task.CompletedTask;
    }

    public Task UpdateCfg(Config newConfig, bool mergeExisting = true)
    {
        _calls.Add(new TelemetryCall(nameof(UpdateCfg), new { newConfig, mergeExisting }));
        return Task.CompletedTask;
    }
}

/// <summary>
/// Represents a recorded telemetry method call.
/// </summary>
public record TelemetryCall(string MethodName, object? Argument);
