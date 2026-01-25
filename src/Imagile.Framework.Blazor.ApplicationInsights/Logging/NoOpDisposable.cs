using System;

namespace Imagile.Framework.Blazor.ApplicationInsights.Logging;

/// <summary>
/// A disposable object that performs no operation when disposed.
/// </summary>
/// <remarks>
/// This is used as a placeholder where an <see cref="IDisposable"/> is required
/// but no cleanup action is needed, such as when monitoring configuration changes
/// that don't require disposal.
/// </remarks>
internal sealed class NoOpDisposable : IDisposable
{
    /// <summary>
    /// Gets the singleton instance of <see cref="NoOpDisposable"/>.
    /// </summary>
    /// <value>
    /// A shared instance of the no-op disposable object.
    /// </value>
    public static IDisposable Instance { get; } = new NoOpDisposable();

    /// <summary>
    /// Prevents a default instance of the <see cref="NoOpDisposable"/> class from being created externally.
    /// </summary>
    /// <remarks>
    /// Use the <see cref="Instance"/> property to access the singleton instance.
    /// </remarks>
    private NoOpDisposable() { }

    /// <summary>
    /// Performs no operation when disposing the object.
    /// </summary>
    /// <remarks>
    /// This method intentionally does nothing as this class is designed to be a no-op placeholder.
    /// </remarks>
    public void Dispose() { }
}
