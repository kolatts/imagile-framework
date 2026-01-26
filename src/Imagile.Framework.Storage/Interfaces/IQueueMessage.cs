namespace Imagile.Framework.Storage.Interfaces;

/// <summary>
/// Base interface for Azure Queue messages with a default queue name.
/// </summary>
/// <remarks>
/// <para>
/// Implementing types must provide a static <see cref="DefaultQueueName"/> property that returns
/// the queue name for this message type. This enables type-safe queue client retrieval
/// via <c>GetQueueClient&lt;T&gt;()</c> extension methods.
/// </para>
/// <para>
/// Queue names must be lowercase and can contain only letters, numbers, and hyphens.
/// The recommended convention is kebab-case (e.g., "tenant-verification").
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class TenantVerificationMessage : IQueueMessage
/// {
///     public static string DefaultQueueName => "tenant-verification";
///
///     public int TenantId { get; set; }
///     public DateTime RequestedAt { get; set; }
/// }
///
/// // Usage with type-safe extension method
/// var queue = queueServiceClient.GetQueueClient&lt;TenantVerificationMessage&gt;();
/// await queue.SendMessageAsync(JsonSerializer.Serialize(message));
/// </code>
/// </example>
public interface IQueueMessage
{
    /// <summary>
    /// Gets the default queue name for this message type.
    /// </summary>
    /// <value>
    /// The Azure Queue Storage queue name. Must be lowercase, 3-63 characters,
    /// and contain only letters, numbers, and hyphens.
    /// </value>
    static abstract string DefaultQueueName { get; }
}
