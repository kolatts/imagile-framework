using Imagile.Framework.Blazor.ApplicationInsights.Interfaces;
using Imagile.Framework.Blazor.ApplicationInsights.Logging;
using Imagile.Framework.Blazor.ApplicationInsights.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Imagile.Framework.Blazor.ApplicationInsights;

/// <summary>
/// Extension methods for adding Application Insights telemetry to Blazor WebAssembly applications.
/// </summary>
public static class IServiceCollectionExtensions
{
    // Internal property for unit testing to simulate browser platform
    internal static bool PretendBrowserPlatform { get; set; }
    private static bool IsBrowserPlatform => PretendBrowserPlatform || OperatingSystem.IsBrowser();

    /// <summary>
    /// Adds Application Insights telemetry services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="builder">Callback for configuring the Application Insights SDK.</param>
    /// <param name="onAppInsightsInit">Optional callback to execute after SDK initialization. Note: requires component to be interactive.</param>
    /// <param name="addWasmLogger">When true, registers the Application Insights logger provider. This is disabled automatically on Blazor Server.</param>
    /// <param name="loggingOptions">Optional callback for configuring logging options. Only applies to Blazor WASM.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method registers <see cref="IApplicationInsights"/> as a singleton service and optionally
    /// adds the Application Insights logger provider to route standard <see cref="ILogger"/> calls to telemetry.
    /// </para>
    /// <para>
    /// The <paramref name="onAppInsightsInit"/> callback is invoked by the <see cref="Components.ApplicationInsightsInit"/>
    /// component after the JavaScript SDK has loaded, allowing you to configure authenticated user context or
    /// add telemetry initializers.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic configuration with connection string
    /// builder.Services.AddBlazorApplicationInsights(config =>
    /// {
    ///     config.ConnectionString = "InstrumentationKey=...";
    ///     config.EnableAutoRouteTracking = true;
    /// });
    ///
    /// // With initialization callback
    /// builder.Services.AddBlazorApplicationInsights(
    ///     config => config.ConnectionString = "...",
    ///     onAppInsightsInit: async appInsights =>
    ///     {
    ///         await appInsights.SetAuthenticatedUserContext(userId);
    ///     });
    ///
    /// // Configure logging
    /// builder.Services.AddBlazorApplicationInsights(
    ///     config => config.ConnectionString = "...",
    ///     loggingOptions: options =>
    ///     {
    ///         options.MinLogLevel = LogLevel.Information;
    ///         options.IncludeScopes = true;
    ///     });
    /// </code>
    /// </example>
    public static IServiceCollection AddBlazorApplicationInsights(
        this IServiceCollection services,
        Action<Config>? builder = null,
        Func<IApplicationInsights, Task>? onAppInsightsInit = null,
        bool addWasmLogger = true,
        Action<ApplicationInsightsLoggerOptions>? loggingOptions = null)
    {
        var initConfig = new ApplicationInsightsInitConfig();

        if (builder != null)
        {
            var config = new Config();
            builder(config);
            initConfig.Config = config;
        }

        if (onAppInsightsInit != null)
        {
            initConfig.OnAppInsightsInit = onAppInsightsInit;
        }

        services.TryAddSingleton(initConfig);

        if (addWasmLogger && IsBrowserPlatform)
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ApplicationInsightsLoggerProvider>(x => CreateLoggerProvider(x, loggingOptions)));

        services.TryAddSingleton<IApplicationInsights, ApplicationInsights>();

        return services;
    }

    /// <summary>
    /// Adds Application Insights telemetry services with configuration from <see cref="IConfiguration"/>.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The configuration instance containing Application Insights settings.</param>
    /// <param name="sectionName">The configuration section name. Defaults to "ApplicationInsights".</param>
    /// <param name="onAppInsightsInit">Optional callback to execute after SDK initialization.</param>
    /// <param name="addWasmLogger">When true, registers the Application Insights logger provider.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// <para>
    /// This overload binds Application Insights configuration from appsettings.json or other configuration sources.
    /// The configuration section should match the <see cref="Config"/> class structure.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // appsettings.json:
    /// // {
    /// //   "ApplicationInsights": {
    /// //     "ConnectionString": "InstrumentationKey=...",
    /// //     "EnableAutoRouteTracking": true
    /// //   }
    /// // }
    ///
    /// builder.Services.AddBlazorApplicationInsights(builder.Configuration);
    /// </code>
    /// </example>
    public static IServiceCollection AddBlazorApplicationInsights(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "ApplicationInsights",
        Func<IApplicationInsights, Task>? onAppInsightsInit = null,
        bool addWasmLogger = true)
    {
        return services.AddBlazorApplicationInsights(
            c => configuration.GetSection(sectionName).Bind(c),
            onAppInsightsInit,
            addWasmLogger);
    }

    private static ApplicationInsightsLoggerProvider CreateLoggerProvider(IServiceProvider services, Action<ApplicationInsightsLoggerOptions>? configure)
    {
        configure ??= delegate { };

        var options = new ApplicationInsightsLoggerOptions();
        configure(options);

        // Use DummyOptionsMonitor to wrap the configured options
        var optionsMonitor = new DummyOptionsMonitor(options);
        var appInsights = services.GetRequiredService<IApplicationInsights>();

        return new ApplicationInsightsLoggerProvider(appInsights, optionsMonitor);
    }

    private class DummyOptionsMonitor : IOptionsMonitor<ApplicationInsightsLoggerOptions>
    {
        public DummyOptionsMonitor(ApplicationInsightsLoggerOptions currentValue)
        {
            CurrentValue = currentValue;
        }

        public ApplicationInsightsLoggerOptions Get(string? name)
        {
            if (name != string.Empty)
                return null!;

            return CurrentValue;
        }

        public IDisposable OnChange(Action<ApplicationInsightsLoggerOptions, string> listener)
            => NoOpDisposable.Instance;

        public ApplicationInsightsLoggerOptions CurrentValue { get; set; }
    }
}
