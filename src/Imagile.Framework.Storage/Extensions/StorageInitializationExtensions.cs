using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Imagile.Framework.Storage.Initialization;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace Imagile.Framework.Storage.Extensions;

/// <summary>
/// Extension methods for initializing Azure Storage resources.
/// </summary>
public static class StorageInitializationExtensions
{
    /// <summary>
    /// Initializes all storage resources (queues and containers) discovered by assembly scanning.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method creates queues and containers if they don't exist, grouping them by storage account
    /// based on the <see cref="Attributes.StorageAccountAttribute"/> on each type.
    /// </para>
    /// <para>
    /// Use this method during application startup for local development to ensure all required
    /// storage resources exist. For production, consider using infrastructure-as-code (Bicep, Terraform)
    /// instead.
    /// </para>
    /// </remarks>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// An <see cref="InitializationResult"/> containing lists of created resources.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="serviceProvider"/> is null.
    /// </exception>
    /// <exception cref="AggregateException">
    /// Thrown when one or more resources fail to create. Contains all individual exceptions.
    /// </exception>
    /// <example>
    /// <code>
    /// var app = builder.Build();
    ///
    /// // Initialize storage resources at startup (local development)
    /// if (app.Environment.IsDevelopment())
    /// {
    ///     var result = await app.Services.InitializeStorageResourcesAsync();
    ///     app.Logger.LogInformation("Storage init: {Result}", result);
    /// }
    /// </code>
    /// </example>
    public static async Task<InitializationResult> InitializeStorageResourcesAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var resources = serviceProvider.GetRequiredService<StorageResources>();
        var queueFactory = serviceProvider.GetRequiredService<IAzureClientFactory<QueueServiceClient>>();
        var blobFactory = serviceProvider.GetRequiredService<IAzureClientFactory<BlobServiceClient>>();

        var result = new InitializationResult();
        var exceptions = new List<Exception>();

        // Initialize queues grouped by storage account
        var queuesByAccount = resources.QueueTypes
            .GroupBy(t => StorageResourceScanner.GetStorageAccountName(t) ?? "Default");

        foreach (var group in queuesByAccount)
        {
            var queueServiceClient = queueFactory.CreateClient(group.Key);

            foreach (var queueType in group)
            {
                try
                {
                    var queueName = StorageResourceScanner.GetQueueName(queueType);
                    var queueClient = queueServiceClient.GetQueueClient(queueName);
                    var response = await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

                    // Check if queue was created (status 201)
                    if (response.Status == 201)
                    {
                        result.CreatedQueues.Add($"{group.Key}/{queueName}");
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(new InvalidOperationException(
                        $"Failed to create queue for type {queueType.FullName}: {ex.Message}", ex));
                }
            }
        }

        // Initialize containers grouped by storage account
        var containersByAccount = resources.ContainerTypes
            .GroupBy(t => StorageResourceScanner.GetStorageAccountName(t) ?? "Default");

        foreach (var group in containersByAccount)
        {
            var blobServiceClient = blobFactory.CreateClient(group.Key);

            foreach (var containerType in group)
            {
                try
                {
                    var containerName = StorageResourceScanner.GetContainerName(containerType);
                    var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                    var response = await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

                    // Check if container was created (response has value when created, null when exists)
                    if (response?.Value != null)
                    {
                        result.CreatedContainers.Add($"{group.Key}/{containerName}");
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(new InvalidOperationException(
                        $"Failed to create container for type {containerType.FullName}: {ex.Message}", ex));
                }
            }
        }

        // Fail-fast: throw aggregate exception if any initialization failed
        if (exceptions.Count > 0)
        {
            throw new AggregateException(
                $"Failed to initialize {exceptions.Count} storage resource(s). See inner exceptions for details.",
                exceptions);
        }

        return result;
    }
}
