using System.Text.Json.Serialization;

namespace Imagile.Framework.Blazor.ApplicationInsights.Models.Context;

/// <summary>
/// Operating system context information for telemetry.
/// Source: https://github.com/microsoft/ApplicationInsights-JS/blob/main/shared/AppInsightsCommon/src/Interfaces/Context/IOperatingSystem.ts
/// </summary>
public class OperatingSystem
{
    /// <summary>
    /// The name of the operating system.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }
}
