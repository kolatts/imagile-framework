using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Imagile.Framework.Blazor.ApplicationInsights.Logging;

/// <summary>
/// Configuration options for <see cref="ApplicationInsightsLogger"/> to control logging behavior.
/// </summary>
/// <remarks>
/// These options determine what information is included in telemetry sent to Application Insights
/// and the minimum severity level for log messages.
/// </remarks>
public class ApplicationInsightsLoggerOptions
{
    /// <summary>
    /// Gets or sets whether to include the logger category name in custom dimensions under the 'CategoryName' key.
    /// </summary>
    /// <value>
    /// <c>true</c> to include category name in telemetry; otherwise, <c>false</c>. Default is <c>true</c>.
    /// </value>
    /// <remarks>
    /// The category name typically matches the full type name of the class using the logger.
    /// Including it helps filter and identify log sources in Application Insights.
    /// </remarks>
    public bool IncludeCategoryName { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include scope information in custom dimensions.
    /// </summary>
    /// <value>
    /// <c>true</c> to include scope information; otherwise, <c>false</c>. Default is <c>true</c>.
    /// </value>
    /// <remarks>
    /// Log scopes provide contextual information across multiple log entries.
    /// When enabled, scope data appears under the 'Scope' key in custom dimensions.
    /// </remarks>
    public bool IncludeScopes { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum log level to send to Application Insights.
    /// </summary>
    /// <value>
    /// The minimum <see cref="LogLevel"/> to record. Default is <see cref="LogLevel.Trace"/> (most verbose).
    /// </value>
    /// <remarks>
    /// Log messages below this level are ignored and not sent to Application Insights.
    /// Use <see cref="LogLevel.Information"/> or higher to reduce telemetry volume in production.
    /// </remarks>
    public LogLevel MinLogLevel { get; set; } = LogLevel.Trace;

    /// <summary>
    /// Gets or sets a callback to enrich custom dimensions with additional properties.
    /// </summary>
    /// <value>
    /// An action that receives the custom dimensions dictionary before it's sent to Application Insights.
    /// </value>
    /// <remarks>
    /// <para>
    /// This callback allows enriching custom dimensions with values that should apply to all log lines.
    /// On key conflict, the enriched value will be overwritten by logger-specific values.
    /// </para>
    /// <para>
    /// This property is not part of the stable API and may change in future versions.
    /// </para>
    /// </remarks>
    [JsonIgnore]
    [Obsolete("Not part of the stable API")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Action<Dictionary<string, object?>>? EnrichCallback { get; set; }
}
