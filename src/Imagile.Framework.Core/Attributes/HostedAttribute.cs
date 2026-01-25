namespace Imagile.Framework.Core.Attributes;

/// <summary>
/// Specifies hosting URLs for an enum value representing a service or application.
/// Used to associate API and web URLs with service identifiers.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is useful for defining service endpoints in a type-safe way,
/// avoiding magic strings scattered throughout code. The URLs can vary by environment
/// and are typically used for service discovery or configuration.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// enum ExternalService
/// {
///     [Hosted(
///         ApiUrl = "https://api.payments.example.com",
///         WebUrl = "https://payments.example.com")]
///     PaymentGateway,
///
///     [Hosted(
///         ApiUrl = "https://api.shipping.example.com",
///         WebUrl = "https://track.shipping.example.com")]
///     ShippingProvider
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field)]
public sealed class HostedAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the API endpoint URL for the service.
    /// </summary>
    public string? ApiUrl { get; init; }

    /// <summary>
    /// Gets or sets the web/UI URL for the service.
    /// </summary>
    public string? WebUrl { get; init; }
}
