using System;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Imagile.Framework.Blazor.ApplicationInsights.Models;

/// <summary>
/// Dependency Telemetry
/// Source: https://github.com/microsoft/ApplicationInsights-JS/blob/main/shared/AppInsightsCommon/src/Interfaces/IDependencyTelemetry.ts
/// </summary>
public class DependencyTelemetry : PartC
{
    /// <summary>
    /// Unique identifier for the dependency call.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// Name of the dependency.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Duration of the dependency call.
    /// </summary>
    [JsonPropertyName("duration")]
    public double? Duration { get; set; }

    /// <summary>
    /// Indicates whether the dependency call was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool? Success { get; set; }

    /// <summary>
    /// Start time of the dependency call.
    /// </summary>
    [JsonPropertyName("startTime")]
    [JsonConverter(typeof(DateTimeJsonConverter))]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// HTTP response code from the dependency call.
    /// </summary>
    [JsonPropertyName("responseCode")]
    public int ResponseCode { get; set; }

    /// <summary>
    /// Correlation context for distributed tracing.
    /// </summary>
    [JsonPropertyName("correlationContext")]
    public string? CorrelationContext { get; set; }

    /// <summary>
    /// Type of dependency (HTTP, SQL, etc.).
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Data associated with the dependency call.
    /// </summary>
    [JsonPropertyName("data")]
    public string? Data { get; set; }

    /// <summary>
    /// Target of the dependency call.
    /// </summary>
    [JsonPropertyName("target")]
    public string? Target { get; set; }

    /// <summary>
    /// Custom defined iKey.
    /// </summary>
    [JsonPropertyName("iKey")]
    public string? IKey { get; set; }
}

/// <summary>
/// JSON converter for DateTime to Unix timestamp milliseconds for Application Insights.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[Browsable(false)]
public class DateTimeJsonConverter : JsonConverter<DateTime>
{
    /// <summary>
    /// Read is not implemented as conversion from JSON to DateTime is not needed.
    /// </summary>
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Writes DateTime as Unix timestamp in milliseconds.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue((decimal)(new DateTimeOffset(value)).ToUnixTimeMilliseconds());
    }
}
