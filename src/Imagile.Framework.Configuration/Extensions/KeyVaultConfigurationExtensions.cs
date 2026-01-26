using Azure;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace Imagile.Framework.Configuration.Extensions
{
    /// <summary>
    /// Extension methods for IConfiguration to enable Azure Key Vault reference replacement.
    /// </summary>
    public static class KeyVaultConfigurationExtensions
    {
        /// <summary>
        /// Replaces Key Vault references in the configuration with actual secret values from Azure Key Vault.
        /// </summary>
        /// <param name="configuration">The configuration instance to process.</param>
        /// <param name="secretClient">The SecretClient used to retrieve secrets from Azure Key Vault.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> or <paramref name="secretClient"/> is null.</exception>
        /// <exception cref="RequestFailedException">Thrown when a referenced secret cannot be retrieved from Key Vault (fail-fast behavior).</exception>
        /// <remarks>
        /// <para>
        /// This method recursively traverses the configuration tree and replaces any values matching the
        /// <c>@KeyVault(SecretName)</c> syntax with the actual secret value retrieved from Azure Key Vault.
        /// </para>
        /// <para>
        /// The syntax follows Azure App Service Key Vault reference format for developer familiarity:
        /// <code>
        /// @KeyVault(SecretName)
        /// </code>
        /// </para>
        /// <para>
        /// Behavior:
        /// - Values starting with <c>@KeyVault(</c> and ending with <c>)</c> are treated as Key Vault references
        /// - Invalid formats (missing parentheses, missing @, etc.) are ignored and left unchanged
        /// - Non-Key Vault values remain untouched
        /// - Nested configuration sections are processed recursively
        /// - Missing or inaccessible secrets cause immediate exception (fail-fast)
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var configuration = new ConfigurationBuilder()
        ///     .AddInMemoryCollection(new Dictionary&lt;string, string?&gt;
        ///     {
        ///         ["Database:Password"] = "@KeyVault(DbPassword)",
        ///         ["Database:Host"] = "localhost"
        ///     })
        ///     .Build();
        ///
        /// var secretClient = new SecretClient(
        ///     new Uri("https://myvault.vault.azure.net"),
        ///     new DefaultAzureCredential());
        ///
        /// configuration.ReplaceKeyVaultReferences(secretClient);
        ///
        /// // configuration["Database:Password"] now contains the actual secret value
        /// // configuration["Database:Host"] remains "localhost"
        /// </code>
        /// </example>
        public static void ReplaceKeyVaultReferences(
            this IConfiguration configuration,
            SecretClient secretClient)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(secretClient);

            ReplaceKeyVaultReferences(configuration.GetChildren(), secretClient);
        }

        private static void ReplaceKeyVaultReferences(
            IEnumerable<IConfigurationSection> configurationSections,
            SecretClient secretClient)
        {
            foreach (var section in configurationSections)
            {
                if (section.Value?.StartsWith("@KeyVault(") == true && section.Value.EndsWith(")"))
                {
                    var secretName = section.Value.Substring(10, section.Value.Length - 11);
                    var secret = secretClient.GetSecret(secretName);
                    section.Value = secret.Value.Value;
                }

                ReplaceKeyVaultReferences(section.GetChildren(), secretClient);
            }
        }
    }
}
