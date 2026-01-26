using System.Reflection;
using Azure.Core;

namespace Imagile.Framework.Storage.DependencyInjection;

/// <summary>
/// Configuration options for Azure Storage abstractions.
/// </summary>
public sealed class StorageOptions
{
    /// <summary>
    /// Gets the registered storage accounts.
    /// </summary>
    internal Dictionary<string, StorageAccountOptions> Accounts { get; } = new();

    /// <summary>
    /// Gets the assemblies to scan for storage interface implementations.
    /// </summary>
    internal List<Assembly> AssembliesToScan { get; } = [];

    /// <summary>
    /// Gets or sets the global retry options applied to all storage clients.
    /// </summary>
    public RetryOptions? RetryOptions { get; set; }
}

/// <summary>
/// Configuration for a single storage account.
/// </summary>
public sealed class StorageAccountOptions
{
    /// <summary>
    /// Gets or sets the storage account connection string.
    /// </summary>
    /// <remarks>
    /// Either <see cref="ConnectionString"/> or <see cref="ServiceUri"/> with <see cref="Credential"/>
    /// must be provided. If both are provided, <see cref="Credential"/> takes precedence.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the storage account service URI for token-based authentication.
    /// </summary>
    /// <remarks>
    /// Use this with <see cref="Credential"/> for identity-based authentication.
    /// Format: https://{account}.blob.core.windows.net or https://{account}.queue.core.windows.net
    /// </remarks>
    public Uri? ServiceUri { get; set; }

    /// <summary>
    /// Gets or sets the token credential for authentication.
    /// </summary>
    /// <remarks>
    /// When provided, takes precedence over <see cref="ConnectionString"/> for authentication.
    /// Use <c>AppTokenCredential</c> from Imagile.Framework.Configuration for environment-aware authentication.
    /// </remarks>
    public TokenCredential? Credential { get; set; }
}
