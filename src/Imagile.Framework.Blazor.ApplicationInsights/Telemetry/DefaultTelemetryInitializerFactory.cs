using Imagile.Framework.Blazor.ApplicationInsights.Models;

namespace Imagile.Framework.Blazor.ApplicationInsights.Telemetry;

/// <summary>
/// Default telemetry initializer factory that adds no additional context.
/// </summary>
/// <remarks>
/// This implementation returns an empty <see cref="TelemetryItem"/>, effectively adding
/// no additional properties to telemetry events. It serves as the default registration
/// when no custom factory is provided. Override by registering your own
/// <see cref="ITelemetryInitializerFactory"/> implementation in dependency injection.
/// </remarks>
public class DefaultTelemetryInitializerFactory : ITelemetryInitializerFactory
{
    /// <inheritdoc />
    public Task<TelemetryItem> CreateInitializerAsync()
    {
        return Task.FromResult(new TelemetryItem());
    }
}
