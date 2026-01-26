namespace Imagile.Framework.Storage.Initialization;

/// <summary>
/// Contains discovered queue message and blob container types from assembly scanning.
/// </summary>
/// <param name="QueueTypes">
/// Types implementing <see cref="Interfaces.IQueueMessage"/> discovered during scanning.
/// </param>
/// <param name="ContainerTypes">
/// Types implementing <see cref="Interfaces.IBlobContainer"/> discovered during scanning.
/// </param>
public sealed record StorageResources(
    IReadOnlyList<Type> QueueTypes,
    IReadOnlyList<Type> ContainerTypes)
{
    /// <summary>
    /// Gets the total count of discovered storage resources.
    /// </summary>
    public int TotalCount => QueueTypes.Count + ContainerTypes.Count;

    /// <summary>
    /// Gets a value indicating whether any storage resources were discovered.
    /// </summary>
    public bool HasResources => TotalCount > 0;

    /// <summary>
    /// Creates an empty <see cref="StorageResources"/> instance.
    /// </summary>
    public static StorageResources Empty { get; } = new([], []);
}
