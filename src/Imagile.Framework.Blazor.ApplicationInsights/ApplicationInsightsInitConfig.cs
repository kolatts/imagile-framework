using Imagile.Framework.Blazor.ApplicationInsights.Interfaces;
using Imagile.Framework.Blazor.ApplicationInsights.Models;

namespace Imagile.Framework.Blazor.ApplicationInsights;

/// <summary>
/// Configuration container for Application Insights initialization.
/// </summary>
public class ApplicationInsightsInitConfig
{
    /// <summary>
    /// Gets or sets the Application Insights SDK configuration.
    /// </summary>
    public Config? Config { get; set; }

    /// <summary>
    /// Gets or sets the callback that executes after Application Insights SDK is initialized.
    /// This allows calling additional setup commands on IApplicationInsights.
    /// </summary>
    public Func<IApplicationInsights, Task>? OnAppInsightsInit { get; set; }
}
