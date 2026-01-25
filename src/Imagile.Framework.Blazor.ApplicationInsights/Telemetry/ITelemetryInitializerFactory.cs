using Imagile.Framework.Blazor.ApplicationInsights.Models;

namespace Imagile.Framework.Blazor.ApplicationInsights.Telemetry;

/// <summary>
/// Factory interface for creating telemetry initializers with dependency-injected context.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface to add tenant ID, user information, or other contextual data
/// to all outgoing telemetry. The factory is called once during SDK initialization, and
/// the resulting <see cref="TelemetryItem"/> is merged into every telemetry event sent to Application Insights.
/// </para>
/// <para>
/// A default implementation (<see cref="DefaultTelemetryInitializerFactory"/>) is registered automatically
/// and returns an empty telemetry item. Override by registering your own implementation in DI.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Custom implementation with tenant context
/// public class TenantTelemetryInitializerFactory : ITelemetryInitializerFactory
/// {
///     private readonly ITenantContextProvider _tenantContext;
///
///     public TenantTelemetryInitializerFactory(ITenantContextProvider tenantContext)
///     {
///         _tenantContext = tenantContext;
///     }
///
///     public Task&lt;TelemetryItem&gt; CreateInitializerAsync()
///     {
///         var item = new TelemetryItem
///         {
///             Tags = new Dictionary&lt;string, object?&gt;
///             {
///                 ["ai.cloud.role"] = _tenantContext.TenantId,
///                 ["ai.cloud.roleInstance"] = _tenantContext.InstanceId
///             }
///         };
///         return Task.FromResult(item);
///     }
/// }
///
/// // Register in DI (replaces default)
/// builder.Services.AddSingleton&lt;ITelemetryInitializerFactory, TenantTelemetryInitializerFactory&gt;();
/// </code>
/// </example>
public interface ITelemetryInitializerFactory
{
    /// <summary>
    /// Creates a telemetry initializer that will be merged into all outgoing telemetry.
    /// </summary>
    /// <returns>
    /// A <see cref="TelemetryItem"/> containing context properties to add to all telemetry events.
    /// </returns>
    /// <remarks>
    /// This method is called once during Application Insights SDK initialization.
    /// The returned item's properties (tags, data, ext, etc.) are merged into every
    /// telemetry event before it is sent to Application Insights.
    /// </remarks>
    Task<TelemetryItem> CreateInitializerAsync();
}
