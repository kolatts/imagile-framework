namespace Imagile.Framework.Storage.Interfaces;

/// <summary>
/// Base interface for Azure Blob containers with a default container name.
/// </summary>
/// <remarks>
/// <para>
/// Implementing types must provide a static <see cref="DefaultContainerName"/> property that returns
/// the container name for this blob container type. This enables type-safe container client retrieval
/// via <c>GetBlobContainerClient&lt;T&gt;()</c> extension methods.
/// </para>
/// <para>
/// Container names must be lowercase and can contain only letters, numbers, and hyphens.
/// The recommended convention is kebab-case (e.g., "tenant-documents").
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class TenantDocumentContainer : IBlobContainer
/// {
///     public static string DefaultContainerName => "tenant-documents";
/// }
///
/// // Usage with type-safe extension method
/// var container = blobServiceClient.GetBlobContainerClient&lt;TenantDocumentContainer&gt;();
/// var blob = container.GetBlobClient($"tenants/{tenantId}/document.pdf");
/// await blob.UploadAsync(stream);
/// </code>
/// </example>
public interface IBlobContainer
{
    /// <summary>
    /// Gets the default container name for this container type.
    /// </summary>
    /// <value>
    /// The Azure Blob Storage container name. Must be lowercase, 3-63 characters,
    /// and contain only letters, numbers, and hyphens.
    /// </value>
    static abstract string DefaultContainerName { get; }
}
