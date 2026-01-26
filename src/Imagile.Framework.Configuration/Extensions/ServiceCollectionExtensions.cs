using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Imagile.Framework.Configuration.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to configure Framework Configuration features.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds and configures Framework Configuration services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="configure">An action to configure the Framework Configuration builder.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/>, <paramref name="configuration"/>, or <paramref name="configure"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is the primary entry point for configuring Framework Configuration features in a .NET application.
    /// It provides a fluent API for enabling Key Vault integration, configuration validation, and other features.
    /// </para>
    /// <para>
    /// The configuration action receives a <see cref="FrameworkConfigurationBuilder"/> instance, which exposes
    /// methods like <see cref="FrameworkConfigurationBuilder.WithKeyVault(Uri, string?)"/> and
    /// <see cref="FrameworkConfigurationBuilder.WithValidation"/> for configuring specific features.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para><strong>Basic Usage (Key Vault only):</strong></para>
    /// <code>
    /// services.AddFrameworkConfiguration(configuration, builder => builder
    ///     .WithKeyVault(new Uri("https://myvault.vault.azure.net/")));
    /// </code>
    /// <para><strong>Full Configuration (Key Vault + Validation):</strong></para>
    /// <code>
    /// services.AddFrameworkConfiguration(configuration, builder => builder
    ///     .WithKeyVault(
    ///         new Uri("https://myvault.vault.azure.net/"),
    ///         configuration["AzureManagedIdentityClientId"])
    ///     .WithValidation());
    /// </code>
    /// <para><strong>Local Development:</strong></para>
    /// <code>
    /// // Uses Azure CLI or Visual Studio credentials automatically
    /// services.AddFrameworkConfiguration(configuration, builder => builder
    ///     .WithKeyVault("https://myvault.vault.azure.net/")
    ///     .WithValidation());
    /// </code>
    /// </example>
    public static IServiceCollection AddFrameworkConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<FrameworkConfigurationBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new FrameworkConfigurationBuilder(services, configuration);
        configure(builder);
        builder.Build();

        return services;
    }
}
