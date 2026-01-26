namespace Imagile.Framework.Configuration;

/// <summary>
/// Internal configuration options for the Framework Configuration builder.
/// </summary>
/// <remarks>
/// This class holds the configuration state collected during the fluent API builder calls.
/// It is not exposed publicly and is only used internally by <see cref="FrameworkConfigurationBuilder"/>.
/// </remarks>
internal class ConfigurationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether Key Vault integration is enabled.
    /// </summary>
    public bool EnableKeyVault { get; set; }

    /// <summary>
    /// Gets or sets the Key Vault URI.
    /// </summary>
    public Uri? KeyVaultUri { get; set; }

    /// <summary>
    /// Gets or sets the managed identity client ID for Key Vault authentication.
    /// </summary>
    public string? ManagedIdentityClientId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether configuration validation is enabled.
    /// </summary>
    public bool EnableValidation { get; set; }
}
