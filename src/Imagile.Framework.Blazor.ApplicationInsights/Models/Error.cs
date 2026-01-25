using System.Text.Json.Serialization;

namespace Imagile.Framework.Blazor.ApplicationInsights.Models;

/// <summary>
/// Error object for exception telemetry.
/// </summary>
public class Error
{
    /// <summary>
    /// The name/type of the error.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// The error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; }

    /// <summary>
    /// The stack trace of the error.
    /// </summary>
    [JsonPropertyName("stack")]
    public string? Stack { get; set; }
}
