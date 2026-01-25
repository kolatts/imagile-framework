using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Imagile.Framework.Blazor.ApplicationInsights.Interfaces;
using Imagile.Framework.Blazor.ApplicationInsights.Models;

namespace Imagile.Framework.Blazor.ApplicationInsights.Logging;

/// <summary>
/// Logger implementation for logging to Application Insights in Blazor Client-Side (WASM) applications.
/// </summary>
/// <remarks>
/// <para>
/// This logger routes standard <see cref="ILogger"/> calls to Application Insights as trace telemetry.
/// It automatically converts log levels to Application Insights severity levels and includes
/// structured data in custom dimensions.
/// </para>
/// <para>
/// The logger is designed for Blazor WASM environments where traditional server-side logging
/// infrastructure is not available. Log entries are sent asynchronously to Application Insights
/// via JavaScript interop.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Standard ILogger usage automatically sends to Application Insights
/// _logger.LogInformation("User {UserId} logged in", userId);
///
/// // Exception logging
/// _logger.LogError(ex, "Failed to process order {OrderId}", orderId);
///
/// // Scoped logging
/// using (_logger.BeginScope(new Dictionary&lt;string, object&gt; { ["OrderId"] = orderId }))
/// {
///     _logger.LogInformation("Processing order");
/// }
/// </code>
/// </example>
[ProviderAlias("BlazorApplicationInsights")]
public class ApplicationInsightsLogger : ILogger
{
    private readonly string? _categoryName;
    private readonly IApplicationInsights _applicationInsights;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationInsightsLogger"/> class without a category name.
    /// </summary>
    /// <param name="applicationInsights">The Application Insights instance to use for transmitting logging messages.</param>
    /// <remarks>
    /// This constructor is marked with <see cref="ActivatorUtilitiesConstructorAttribute"/> for dependency injection.
    /// When using this constructor, the category name will not be included in custom dimensions.
    /// </remarks>
    [ActivatorUtilitiesConstructor]
    public ApplicationInsightsLogger(IApplicationInsights applicationInsights)
        : this(null, applicationInsights)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationInsightsLogger"/> class with a category name.
    /// </summary>
    /// <param name="categoryName">The category of the logger, typically the full type name of the class using the logger.</param>
    /// <param name="applicationInsights">The Application Insights instance to use for transmitting logging messages.</param>
    /// <remarks>
    /// The category name is stored in custom dimensions under the 'CategoryName' key if
    /// <see cref="IncludeCategoryName"/> is <c>true</c>.
    /// </remarks>
    public ApplicationInsightsLogger(string? categoryName, IApplicationInsights applicationInsights)
    {
        _categoryName = categoryName;
        _applicationInsights = applicationInsights;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to include the logger category name in custom dimensions under the 'CategoryName' key.
    /// </summary>
    /// <value>
    /// <c>true</c> to include category name; otherwise, <c>false</c>. Default is <c>false</c>.
    /// </value>
    public bool IncludeCategoryName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include scope information in custom dimensions.
    /// </summary>
    /// <value>
    /// <c>true</c> to include scope information; otherwise, <c>false</c>. Default is <c>false</c>.
    /// </value>
    /// <remarks>
    /// When enabled, scope data appears under the 'Scope' key in custom dimensions.
    /// Structured scope data (dictionaries) are flattened into individual custom dimension entries.
    /// </remarks>
    public bool IncludeScopes { get; set; }

    /// <summary>
    /// Gets or sets the minimum log level to send to Application Insights.
    /// </summary>
    /// <value>
    /// The minimum <see cref="LogLevel"/> to record.
    /// </value>
    /// <remarks>
    /// Log messages below this level are ignored by <see cref="IsEnabled"/>.
    /// </remarks>
    public LogLevel MinLogLevel { get; set; }

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
    [Obsolete("Not part of the stable API")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Action<Dictionary<string, object?>> EnrichmentCallback { get; set; } = delegate { };

    /// <summary>
    /// Gets or sets the active scope provider.
    /// </summary>
    /// <value>
    /// The external scope provider used for logging scopes.
    /// </value>
    /// <remarks>
    /// This property is set by <see cref="ApplicationInsightsLoggerProvider"/> to enable scope sharing.
    /// </remarks>
    internal IExternalScopeProvider ScopeProvider { private get; set; } = new LoggerExternalScopeProvider();

    /// <summary>
    /// Writes a log entry to Application Insights.
    /// </summary>
    /// <typeparam name="TState">The type of the object to be written.</typeparam>
    /// <param name="logLevel">The severity level of the log entry.</param>
    /// <param name="eventId">The event identifier associated with the log entry.</param>
    /// <param name="state">The entry to be written. Can be any object that the formatter knows how to serialize.</param>
    /// <param name="exception">The exception related to this entry, or <c>null</c> if no exception.</param>
    /// <param name="formatter">Function to create a string message from the state and exception.</param>
    /// <remarks>
    /// <para>
    /// This method converts the log entry to either a <see cref="TraceTelemetry"/> (for logs without exceptions)
    /// or an <see cref="ExceptionTelemetry"/> (for logs with exceptions) and sends it to Application Insights.
    /// </para>
    /// <para>
    /// Custom dimensions are populated with:
    /// - Category name (if <see cref="IncludeCategoryName"/> is <c>true</c>)
    /// - Event ID and name
    /// - Structured state data (if state is a dictionary)
    /// - Scope information (if <see cref="IncludeScopes"/> is <c>true</c>)
    /// - Enriched data (if <see cref="EnrichmentCallback"/> is set)
    /// </para>
    /// </remarks>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var severity = GetSeverityLevel(logLevel);
        var message = formatter(state, exception);
        var customDimensions = GetCustomDimensions(state, eventId);

        if (exception is null)
        {
            _applicationInsights.TrackTrace(new TraceTelemetry { Message = message, SeverityLevel = severity, Properties = customDimensions });
            return;
        }

        var error = new Error
        {
            Name = exception.GetType().Name,
            Message = exception.Message,
            Stack = exception.ToString()
        };

        _applicationInsights.TrackException(new ExceptionTelemetry { Exception = error, Id = $"{eventId}", SeverityLevel = severity, Properties = customDimensions });
    }

    /// <summary>
    /// Checks if the given log level is enabled.
    /// </summary>
    /// <param name="logLevel">The log level to be checked.</param>
    /// <returns>
    /// <c>true</c> if the log level is enabled; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Log levels below <see cref="MinLogLevel"/> or equal to <see cref="LogLevel.None"/> are disabled.
    /// </remarks>
    public bool IsEnabled(LogLevel logLevel) => MinLogLevel <= logLevel && logLevel != LogLevel.None;

    /// <summary>
    /// Begins a logical operation scope.
    /// </summary>
    /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
    /// <param name="state">The identifier for the scope.</param>
    /// <returns>
    /// An <see cref="IDisposable"/> that ends the logical operation scope on dispose.
    /// </returns>
    /// <remarks>
    /// Scope information is included in custom dimensions when <see cref="IncludeScopes"/> is <c>true</c>.
    /// </remarks>
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => ScopeProvider.Push(state);

    private Dictionary<string, object?> GetCustomDimensions<TState>(TState state, EventId eventId)
    {
        var result = new Dictionary<string, object?>();

        // Give a chance to customize customDimensions
#pragma warning disable CS0618 // Type or member is obsolete
        EnrichmentCallback(result);
#pragma warning restore CS0618 // Type or member is obsolete

        ApplyScopes(result);
        ApplyLogState(result, state, eventId);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ApplyLogState<TState>(Dictionary<string, object?> customDimensions, TState state, EventId eventId)
    {
        if (IncludeCategoryName && !string.IsNullOrEmpty(_categoryName))
            customDimensions["CategoryName"] = _categoryName;

        if (eventId.Id != 0)
            customDimensions["EventId"] = eventId.Id.ToString(CultureInfo.InvariantCulture);

        if (!string.IsNullOrEmpty(eventId.Name))
            customDimensions["EventName"] = eventId.Name;

        if (state is IReadOnlyCollection<KeyValuePair<string, object?>> stateDictionary)
            ApplyDictionary(customDimensions, stateDictionary);
    }

    private void ApplyScopes(Dictionary<string, object?> customDimensions)
    {
        if (!IncludeScopes)
            return;

        var scopeBuilder = new StringBuilder();
        ScopeProvider.ForEachScope(ApplyScope, (customDimensions, scopeBuilder));

        if (scopeBuilder.Length > 0)
            customDimensions["Scope"] = scopeBuilder.ToString();

        static void ApplyScope(object scope, (Dictionary<string, object?> data, StringBuilder scopeBuilder) result)
        {
            if (scope is IReadOnlyCollection<KeyValuePair<string, object?>> scopeDictionary)
            {
                ApplyDictionary(result.data, scopeDictionary);
                return;
            }

            result.scopeBuilder.Append(" => ").Append(scope);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ApplyDictionary(Dictionary<string, object?> target, IReadOnlyCollection<KeyValuePair<string, object?>> source)
    {
        foreach (var kvp in source)
        {
            var key = kvp.Key == "{OriginalFormat}" ? "OriginalFormat" : kvp.Key;
            target[key] = Convert.ToString(kvp.Value, CultureInfo.InvariantCulture);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SeverityLevel GetSeverityLevel(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => SeverityLevel.Verbose,
        LogLevel.Debug => SeverityLevel.Verbose,
        LogLevel.Information => SeverityLevel.Information,
        LogLevel.Warning => SeverityLevel.Warning,
        LogLevel.Error => SeverityLevel.Error,
        LogLevel.Critical => SeverityLevel.Critical,
        _ => SeverityLevel.Verbose
    };
}
