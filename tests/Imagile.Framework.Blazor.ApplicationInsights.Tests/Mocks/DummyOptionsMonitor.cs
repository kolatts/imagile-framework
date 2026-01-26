using Microsoft.Extensions.Options;

namespace Imagile.Framework.Blazor.ApplicationInsights.Tests.Mocks;

/// <summary>
/// Simple IOptionsMonitor implementation for testing without full configuration infrastructure.
/// </summary>
public class DummyOptionsMonitor<T> : IOptionsMonitor<T> where T : class
{
    private readonly T _currentValue;

    public DummyOptionsMonitor(T currentValue)
    {
        _currentValue = currentValue;
    }

    public T CurrentValue => _currentValue;

    public T Get(string? name) => _currentValue;

    public IDisposable OnChange(Action<T, string?> listener) => new NoOpDisposable();

    private class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
