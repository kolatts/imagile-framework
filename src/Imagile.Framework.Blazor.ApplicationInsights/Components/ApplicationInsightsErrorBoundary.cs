using Imagile.Framework.Blazor.ApplicationInsights.Interfaces;
using Imagile.Framework.Blazor.ApplicationInsights.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace Imagile.Framework.Blazor.ApplicationInsights.Components;

/// <summary>
/// Error boundary that automatically tracks unhandled exceptions to Application Insights.
/// </summary>
/// <remarks>
/// <para>
/// Wrap your application content or specific components with this error boundary to enable
/// automatic exception tracking. When an unhandled exception occurs, it is sent to Application Insights
/// as exception telemetry with Error severity.
/// </para>
/// <para>
/// This component inherits from <see cref="ErrorBoundary"/> and overrides <see cref="OnErrorAsync"/>
/// to intercept exceptions before displaying the error UI.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In App.razor or layout:
/// &lt;ApplicationInsightsErrorBoundary&gt;
///     &lt;ChildContent&gt;
///         @Body
///     &lt;/ChildContent&gt;
///     &lt;ErrorContent Context="ex"&gt;
///         &lt;h1&gt;An error occurred&lt;/h1&gt;
///         &lt;p&gt;@ex.Message&lt;/p&gt;
///     &lt;/ErrorContent&gt;
/// &lt;/ApplicationInsightsErrorBoundary&gt;
/// </code>
/// </example>
public class ApplicationInsightsErrorBoundary : ErrorBoundary
{
    [Inject] private IApplicationInsights ApplicationInsights { get; set; } = default!;
    [Inject] private ILogger<ApplicationInsightsErrorBoundary> Logger { get; set; } = default!;

    /// <summary>
    /// Called when an unhandled exception occurs within the error boundary.
    /// </summary>
    /// <param name="exception">The unhandled exception.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method tracks the exception to Application Insights before calling the base implementation
    /// to display the error UI. If tracking fails, a warning is logged but the error UI is still displayed.
    /// </remarks>
    protected override async Task OnErrorAsync(Exception exception)
    {
        try
        {
            await ApplicationInsights.TrackException(new ExceptionTelemetry
            {
                Exception = new Error
                {
                    Name = exception.GetType().FullName ?? exception.GetType().Name,
                    Message = exception.Message,
                    Stack = exception.StackTrace
                },
                SeverityLevel = SeverityLevel.Error
            });
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to track exception to Application Insights");
        }

        await base.OnErrorAsync(exception);
    }
}
