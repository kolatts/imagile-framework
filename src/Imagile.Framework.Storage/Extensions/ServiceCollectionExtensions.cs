using Imagile.Framework.Storage.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Imagile.Framework.Storage.Extensions;

/// <summary>
/// Extension methods for registering Azure Storage abstractions with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Azure Storage abstractions to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure storage options.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configure"/> is null.
    /// </exception>
    /// <example>
    /// <code>
    /// services.AddStorageAbstractions(storage =>
    /// {
    ///     // Default storage account
    ///     storage.AddStorageAccount(connectionString);
    ///
    ///     // Or with identity-based auth
    ///     storage.AddStorageAccount(serviceUri, new AppTokenCredential(clientId));
    ///
    ///     // Named storage account for archive
    ///     storage.AddStorageAccount("archive", archiveConnectionString);
    ///
    ///     // Scan for IQueueMessage and IBlobContainer implementations
    ///     storage.ScanAssemblies(typeof(MyQueueMessage).Assembly);
    ///
    ///     // Optional: Configure retry policy
    ///     storage.ConfigureRetry(new RetryOptions
    ///     {
    ///         MaxRetries = 5,
    ///         Delay = TimeSpan.FromSeconds(2),
    ///         Mode = RetryMode.Exponential
    ///     });
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddStorageAbstractions(
        this IServiceCollection services,
        Action<StorageBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new StorageBuilder(services);
        configure(builder);
        builder.Build();

        return services;
    }
}
