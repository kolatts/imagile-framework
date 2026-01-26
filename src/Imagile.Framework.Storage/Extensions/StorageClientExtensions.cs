using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Imagile.Framework.Storage.Interfaces;

namespace Imagile.Framework.Storage.Extensions;

/// <summary>
/// Provides type-safe extension methods for Azure Storage service clients.
/// </summary>
/// <remarks>
/// These extensions leverage the static abstract <see cref="IQueueMessage.DefaultQueueName"/>
/// and <see cref="IBlobContainer.DefaultContainerName"/> properties to enable type-safe
/// client retrieval without hardcoded queue/container names.
/// </remarks>
public static class StorageClientExtensions
{
    /// <summary>
    /// Gets a queue client for the specified message type using its default queue name.
    /// </summary>
    /// <typeparam name="T">
    /// The queue message type implementing <see cref="IQueueMessage"/>.
    /// </typeparam>
    /// <param name="client">The queue service client.</param>
    /// <returns>
    /// A <see cref="QueueClient"/> configured for the queue associated with type <typeparamref name="T"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="client"/> is null.
    /// </exception>
    /// <example>
    /// <code>
    /// public class TenantVerificationMessage : IQueueMessage
    /// {
    ///     public static string DefaultQueueName => "tenant-verification";
    ///     public int TenantId { get; set; }
    /// }
    ///
    /// // Type-safe queue access
    /// var queue = queueServiceClient.GetQueueClient&lt;TenantVerificationMessage&gt;();
    /// await queue.SendMessageAsync(JsonSerializer.Serialize(message));
    /// </code>
    /// </example>
    public static QueueClient GetQueueClient<T>(this QueueServiceClient client)
        where T : IQueueMessage
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.GetQueueClient(T.DefaultQueueName);
    }

    /// <summary>
    /// Gets a blob container client for the specified container type using its default container name.
    /// </summary>
    /// <typeparam name="T">
    /// The blob container type implementing <see cref="IBlobContainer"/>.
    /// </typeparam>
    /// <param name="client">The blob service client.</param>
    /// <returns>
    /// A <see cref="BlobContainerClient"/> configured for the container associated with type <typeparamref name="T"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="client"/> is null.
    /// </exception>
    /// <example>
    /// <code>
    /// public class TenantDocumentContainer : IBlobContainer
    /// {
    ///     public static string DefaultContainerName => "tenant-documents";
    /// }
    ///
    /// // Type-safe container access
    /// var container = blobServiceClient.GetBlobContainerClient&lt;TenantDocumentContainer&gt;();
    /// var blob = container.GetBlobClient($"tenants/{tenantId}/document.pdf");
    /// await blob.UploadAsync(stream);
    /// </code>
    /// </example>
    public static BlobContainerClient GetBlobContainerClient<T>(this BlobServiceClient client)
        where T : IBlobContainer
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.GetBlobContainerClient(T.DefaultContainerName);
    }
}
