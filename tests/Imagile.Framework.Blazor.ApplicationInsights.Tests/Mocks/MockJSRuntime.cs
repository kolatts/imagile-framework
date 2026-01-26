using Microsoft.JSInterop;

namespace Imagile.Framework.Blazor.ApplicationInsights.Tests.Mocks;

/// <summary>
/// Mock implementation of IJSRuntime for testing without browser environment.
/// Records all JS method invocations for verification.
/// </summary>
public class MockJSRuntime : IJSRuntime
{
    private readonly List<JsInvocation> _invocations = new();
    private readonly Dictionary<string, object?> _returnValues = new();

    /// <summary>
    /// Gets all recorded JS method invocations.
    /// </summary>
    public IReadOnlyList<JsInvocation> Invocations => _invocations;

    /// <summary>
    /// Configures a return value for a specific JS method identifier.
    /// </summary>
    public void SetupReturn<T>(string identifier, T value)
    {
        _returnValues[identifier] = value;
    }

    /// <summary>
    /// Clears all recorded invocations.
    /// </summary>
    public void Clear()
    {
        _invocations.Clear();
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        _invocations.Add(new JsInvocation(identifier, args ?? Array.Empty<object?>()));

        if (_returnValues.TryGetValue(identifier, out var returnValue))
        {
            if (returnValue is TValue typed)
            {
                return ValueTask.FromResult(typed);
            }
        }

        return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        return InvokeAsync<TValue>(identifier, args);
    }
}

/// <summary>
/// Represents a recorded JavaScript method invocation.
/// </summary>
public record JsInvocation(string Identifier, object?[] Arguments);
