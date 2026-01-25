using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Imagile.Framework.Blazor.ApplicationInsights.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Imagile.Framework.Blazor.ApplicationInsights.Logging;

/// <summary>
/// Logger provider implementation for logging to Application Insights in Blazor Client-Side (WASM) applications.
/// </summary>
/// <remarks>
/// This provider creates <see cref="ApplicationInsightsLogger"/> instances that route standard
/// <see cref="ILogger"/> calls to Application Insights as trace telemetry. This allows existing
/// logging code to automatically appear in Application Insights without refactoring.
/// </remarks>
public class ApplicationInsightsLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly IApplicationInsights _applicationInsights;
    private readonly ConcurrentDictionary<string, ApplicationInsightsLogger> _loggers = new();
    private readonly IDisposable _optionsReloadToken;
    private Action<Dictionary<string, object?>> _enrichmentCallback = delegate { };
    private IExternalScopeProvider? _scopeProvider;
    private ApplicationInsightsLoggerOptions _options = new();
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationInsightsLoggerProvider"/> class.
    /// </summary>
    /// <param name="applicationInsights">The Application Insights instance to use for transmitting logging messages.</param>
    /// <remarks>
    /// This constructor uses default options. For configuration support, use the constructor
    /// that accepts <see cref="IOptionsMonitor{TOptions}"/>.
    /// </remarks>
    public ApplicationInsightsLoggerProvider(IApplicationInsights applicationInsights)
    {
        _applicationInsights = applicationInsights;
        _optionsReloadToken = NoOpDisposable.Instance;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationInsightsLoggerProvider"/> class with configuration support.
    /// </summary>
    /// <param name="applicationInsights">The Application Insights instance to use for transmitting logging messages.</param>
    /// <param name="options">The options monitor that provides configuration and change notification support.</param>
    /// <remarks>
    /// This constructor is marked with <see cref="ActivatorUtilitiesConstructorAttribute"/> to ensure
    /// it's used when resolving the logger provider from dependency injection.
    /// Configuration changes are automatically applied to all existing loggers.
    /// </remarks>
    [ActivatorUtilitiesConstructor]
    public ApplicationInsightsLoggerProvider(IApplicationInsights applicationInsights, IOptionsMonitor<ApplicationInsightsLoggerOptions> options)
    {
        _applicationInsights = applicationInsights;
        _optionsReloadToken = options.OnChange(ReloadOptions);
        ReloadOptions(options.CurrentValue);
    }

    /// <summary>
    /// Creates a new <see cref="ILogger"/> instance for the specified category name.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger. Typically the full type name.</param>
    /// <returns>
    /// An <see cref="ILogger"/> instance that sends log messages to Application Insights.
    /// </returns>
    /// <remarks>
    /// Logger instances are cached per category name. Multiple requests for the same category
    /// return the same logger instance.
    /// </remarks>
    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, CreateLoggerInstance);

    /// <summary>
    /// Sets the scope provider for all loggers created by this provider.
    /// </summary>
    /// <param name="scopeProvider">The external scope provider to use for logging scopes.</param>
    /// <remarks>
    /// This method is called by the logging infrastructure to enable scope sharing across loggers.
    /// It updates both new and existing logger instances.
    /// </remarks>
    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;

        foreach (var logger in _loggers.Values)
            logger.ScopeProvider = scopeProvider;
    }

    /// <summary>
    /// Disposes the logger provider and releases all resources.
    /// </summary>
    /// <remarks>
    /// This method releases the options reload token to stop monitoring configuration changes.
    /// It is safe to call multiple times.
    /// </remarks>
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _optionsReloadToken.Dispose();
    }

    private ApplicationInsightsLogger CreateLoggerInstance(string categoryName)
    {
        return new ApplicationInsightsLogger(categoryName, _applicationInsights)
        {
            ScopeProvider = GetScopeProvider(),
            MinLogLevel = _options.MinLogLevel,
            IncludeScopes = _options.IncludeScopes,
            IncludeCategoryName = _options.IncludeCategoryName,

#pragma warning disable CS0618 // Type or member is obsolete
            EnrichmentCallback = _enrichmentCallback
#pragma warning restore CS0618 // Type or member is obsolete
        };
    }

    private IExternalScopeProvider GetScopeProvider()
    {
        _scopeProvider ??= new LoggerExternalScopeProvider();
        return _scopeProvider;
    }

    private void ReloadOptions(ApplicationInsightsLoggerOptions options)
    {
        _options = options;

#pragma warning disable CS0618 // Type or member is obsolete
        _enrichmentCallback = options.EnrichCallback ?? delegate { };
#pragma warning restore CS0618 // Type or member is obsolete

        var scopeProvider = GetScopeProvider();
        foreach (var logger in _loggers.Values)
        {
            logger.ScopeProvider = scopeProvider;
            logger.IncludeCategoryName = options.IncludeCategoryName;
            logger.IncludeScopes = options.IncludeScopes;
            logger.MinLogLevel = options.MinLogLevel;

#pragma warning disable CS0618 // Type or member is obsolete
            logger.EnrichmentCallback = _enrichmentCallback;
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
