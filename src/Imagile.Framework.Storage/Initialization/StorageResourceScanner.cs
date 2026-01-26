using System.Reflection;
using Imagile.Framework.Storage.Attributes;
using Imagile.Framework.Storage.Interfaces;

namespace Imagile.Framework.Storage.Initialization;

/// <summary>
/// Scans assemblies for types implementing Azure Storage interfaces.
/// </summary>
/// <remarks>
/// <para>
/// The scanner discovers all concrete classes implementing <see cref="IQueueMessage"/>
/// and <see cref="IBlobContainer"/> in the specified assemblies. These types are then
/// used for automatic queue/container initialization.
/// </para>
/// <para>
/// For performance, pass only assemblies that contain storage types. Do not use
/// <c>AppDomain.CurrentDomain.GetAssemblies()</c> as it includes all loaded assemblies.
/// </para>
/// </remarks>
public static class StorageResourceScanner
{
    /// <summary>
    /// Scans assemblies for types implementing storage interfaces.
    /// </summary>
    /// <param name="assemblies">
    /// Assemblies to scan. If empty, scans the calling assembly.
    /// </param>
    /// <returns>
    /// A <see cref="StorageResources"/> instance containing discovered queue and container types.
    /// </returns>
    /// <example>
    /// <code>
    /// // Scan specific assemblies
    /// var resources = StorageResourceScanner.ScanForResources(
    ///     typeof(MyQueueMessage).Assembly,
    ///     typeof(MyBlobContainer).Assembly);
    ///
    /// Console.WriteLine($"Found {resources.QueueTypes.Count} queues");
    /// Console.WriteLine($"Found {resources.ContainerTypes.Count} containers");
    /// </code>
    /// </example>
    public static StorageResources ScanForResources(params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
        {
            assemblies = [Assembly.GetCallingAssembly()];
        }

        var queueTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IQueueMessage).IsAssignableFrom(t))
            .ToList();

        var containerTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IBlobContainer).IsAssignableFrom(t))
            .ToList();

        return new StorageResources(queueTypes, containerTypes);
    }

    /// <summary>
    /// Gets the queue name from a type implementing <see cref="IQueueMessage"/>.
    /// </summary>
    /// <param name="queueMessageType">
    /// The type implementing <see cref="IQueueMessage"/>.
    /// </param>
    /// <returns>The queue name from the type's <c>DefaultQueueName</c> property.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="queueMessageType"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the type does not have a <c>DefaultQueueName</c> static property.
    /// </exception>
    public static string GetQueueName(Type queueMessageType)
    {
        ArgumentNullException.ThrowIfNull(queueMessageType);

        var property = queueMessageType.GetProperty(
            nameof(IQueueMessage.DefaultQueueName),
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        return property?.GetValue(null) as string
            ?? throw new InvalidOperationException(
                $"Type {queueMessageType.FullName} does not have a static DefaultQueueName property.");
    }

    /// <summary>
    /// Gets the container name from a type implementing <see cref="IBlobContainer"/>.
    /// </summary>
    /// <param name="blobContainerType">
    /// The type implementing <see cref="IBlobContainer"/>.
    /// </param>
    /// <returns>The container name from the type's <c>DefaultContainerName</c> property.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="blobContainerType"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the type does not have a <c>DefaultContainerName</c> static property.
    /// </exception>
    public static string GetContainerName(Type blobContainerType)
    {
        ArgumentNullException.ThrowIfNull(blobContainerType);

        var property = blobContainerType.GetProperty(
            nameof(IBlobContainer.DefaultContainerName),
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        return property?.GetValue(null) as string
            ?? throw new InvalidOperationException(
                $"Type {blobContainerType.FullName} does not have a static DefaultContainerName property.");
    }

    /// <summary>
    /// Gets the storage account name from a type's <see cref="StorageAccountAttribute"/>.
    /// </summary>
    /// <param name="type">The type to check for the attribute.</param>
    /// <returns>
    /// The storage account name if the attribute is present; otherwise, <c>null</c> for the default account.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="type"/> is null.
    /// </exception>
    public static string? GetStorageAccountName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.GetCustomAttribute<StorageAccountAttribute>()?.Name;
    }
}
