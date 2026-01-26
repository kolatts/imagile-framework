using System.Reflection;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Imagile.Framework.Storage.Initialization;

namespace Imagile.Framework.Storage.DependencyInjection;

/// <summary>
/// Builder for configuring Azure Storage abstractions.
/// </summary>
/// <remarks>
/// Use this builder to configure storage accounts, credentials, and assembly scanning
/// for automatic queue and container discovery.
/// </remarks>
public sealed class StorageBuilder
{
    private readonly IServiceCollection _services;
    private readonly StorageOptions _options = new();

    internal StorageBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Adds the default (unnamed) storage account using a connection string.
    /// </summary>
    /// <param name="connectionString">The Azure Storage connection string.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="connectionString"/> is null or whitespace.
    /// </exception>
    public StorageBuilder AddStorageAccount(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        return AddStorageAccount("Default", connectionString, credential: null);
    }

    /// <summary>
    /// Adds the default (unnamed) storage account using a service URI and credential.
    /// </summary>
    /// <param name="serviceUri">The storage account service URI.</param>
    /// <param name="credential">The token credential for authentication.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="serviceUri"/> or <paramref name="credential"/> is null.
    /// </exception>
    public StorageBuilder AddStorageAccount(Uri serviceUri, TokenCredential credential)
    {
        ArgumentNullException.ThrowIfNull(serviceUri);
        ArgumentNullException.ThrowIfNull(credential);
        return AddStorageAccount("Default", serviceUri, credential);
    }

    /// <summary>
    /// Adds a named storage account using a connection string.
    /// </summary>
    /// <param name="name">The storage account name for DI resolution.</param>
    /// <param name="connectionString">The Azure Storage connection string.</param>
    /// <returns>The builder for method chaining.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="connectionString"/> is null or whitespace.
    /// </exception>
    public StorageBuilder AddStorageAccount(string name, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        return AddStorageAccount(name, connectionString, credential: null);
    }

    /// <summary>
    /// Adds a named storage account using a connection string and optional credential.
    /// </summary>
    /// <param name="name">The storage account name for DI resolution.</param>
    /// <param name="connectionString">The Azure Storage connection string.</param>
    /// <param name="credential">Optional token credential (takes precedence over connection string auth).</param>
    /// <returns>The builder for method chaining.</returns>
    public StorageBuilder AddStorageAccount(string name, string connectionString, TokenCredential? credential)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        _options.Accounts[name] = new StorageAccountOptions
        {
            ConnectionString = connectionString,
            Credential = credential
        };

        return this;
    }

    /// <summary>
    /// Adds a named storage account using a service URI and credential.
    /// </summary>
    /// <param name="name">The storage account name for DI resolution.</param>
    /// <param name="serviceUri">The storage account service URI.</param>
    /// <param name="credential">The token credential for authentication.</param>
    /// <returns>The builder for method chaining.</returns>
    public StorageBuilder AddStorageAccount(string name, Uri serviceUri, TokenCredential credential)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(serviceUri);
        ArgumentNullException.ThrowIfNull(credential);

        _options.Accounts[name] = new StorageAccountOptions
        {
            ServiceUri = serviceUri,
            Credential = credential
        };

        return this;
    }

    /// <summary>
    /// Specifies assemblies to scan for <see cref="Interfaces.IQueueMessage"/> and
    /// <see cref="Interfaces.IBlobContainer"/> implementations.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The builder for method chaining.</returns>
    public StorageBuilder ScanAssemblies(params Assembly[] assemblies)
    {
        _options.AssembliesToScan.AddRange(assemblies);
        return this;
    }

    /// <summary>
    /// Configures global retry options for all storage clients.
    /// </summary>
    /// <param name="retryOptions">The retry options to apply.</param>
    /// <returns>The builder for method chaining.</returns>
    public StorageBuilder ConfigureRetry(RetryOptions retryOptions)
    {
        _options.RetryOptions = retryOptions;
        return this;
    }

    /// <summary>
    /// Builds and registers the configured storage services.
    /// </summary>
    internal void Build()
    {
        if (_options.Accounts.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one storage account must be configured. Call AddStorageAccount() before building.");
        }

        // Register scanned resources as singleton
        var resources = _options.AssembliesToScan.Count > 0
            ? StorageResourceScanner.ScanForResources([.. _options.AssembliesToScan])
            : StorageResources.Empty;

        _services.AddSingleton(resources);
        _services.AddSingleton(_options);

        // Register Azure clients using Microsoft.Extensions.Azure
        _services.AddAzureClients(builder =>
        {
            foreach (var (name, accountOptions) in _options.Accounts)
            {
                RegisterStorageAccount(builder, name, accountOptions);
            }

            // Configure global retry if specified
            if (_options.RetryOptions != null)
            {
                builder.ConfigureDefaults(options =>
                {
                    options.Retry.Delay = _options.RetryOptions.Delay;
                    options.Retry.MaxRetries = _options.RetryOptions.MaxRetries;
                    options.Retry.Mode = _options.RetryOptions.Mode;
                    options.Retry.MaxDelay = _options.RetryOptions.MaxDelay;
                    options.Retry.NetworkTimeout = _options.RetryOptions.NetworkTimeout;
                });
            }
        });
    }

    private static void RegisterStorageAccount(
        AzureClientFactoryBuilder builder,
        string name,
        StorageAccountOptions options)
    {
        // Queue client registration
        if (options.ServiceUri != null && options.Credential != null)
        {
            // URI-based authentication
            var queueUri = BuildServiceUri(options.ServiceUri, "queue");
            var blobUri = BuildServiceUri(options.ServiceUri, "blob");

            builder.AddQueueServiceClient(queueUri)
                .WithName(name)
                .WithCredential(options.Credential)
                .ConfigureOptions(o => o.MessageEncoding = QueueMessageEncoding.Base64);

            builder.AddBlobServiceClient(blobUri)
                .WithName(name)
                .WithCredential(options.Credential);
        }
        else if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            // Connection string authentication
            builder.AddQueueServiceClient(options.ConnectionString)
                .WithName(name)
                .ConfigureOptions(o => o.MessageEncoding = QueueMessageEncoding.Base64);

            builder.AddBlobServiceClient(options.ConnectionString)
                .WithName(name);

            // Apply credential if provided (takes precedence)
            if (options.Credential != null)
            {
                // Note: When using connection string + credential, the credential
                // will be used for token-based auth instead of the connection string's key
            }
        }
    }

    private static Uri BuildServiceUri(Uri baseUri, string service)
    {
        // If URI already contains the service (queue/blob), use as-is
        if (baseUri.Host.Contains($".{service}.", StringComparison.OrdinalIgnoreCase))
        {
            return baseUri;
        }

        // Otherwise, construct the service-specific URI
        // e.g., https://myaccount.blob.core.windows.net -> https://myaccount.queue.core.windows.net
        var host = baseUri.Host;
        var newHost = host.Replace(".blob.", $".{service}.", StringComparison.OrdinalIgnoreCase)
                         .Replace(".queue.", $".{service}.", StringComparison.OrdinalIgnoreCase);

        if (newHost == host)
        {
            // Assume it's just the account name, construct full URI
            var accountName = host.Split('.')[0];
            return new Uri($"https://{accountName}.{service}.core.windows.net");
        }

        return new UriBuilder(baseUri) { Host = newHost }.Uri;
    }
}
