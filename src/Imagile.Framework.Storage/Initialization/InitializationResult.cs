namespace Imagile.Framework.Storage.Initialization;

/// <summary>
/// Result of storage resource initialization.
/// </summary>
/// <remarks>
/// Contains lists of queues and containers that were created during initialization.
/// Resources that already existed are not included in these lists.
/// </remarks>
public sealed class InitializationResult
{
    /// <summary>
    /// Gets the queues that were created during initialization.
    /// </summary>
    /// <remarks>
    /// Format: "{accountName}/{queueName}" (e.g., "Default/tenant-verification").
    /// Queues that already existed are not included.
    /// </remarks>
    public List<string> CreatedQueues { get; } = [];

    /// <summary>
    /// Gets the containers that were created during initialization.
    /// </summary>
    /// <remarks>
    /// Format: "{accountName}/{containerName}" (e.g., "archive/audit-logs").
    /// Containers that already existed are not included.
    /// </remarks>
    public List<string> CreatedContainers { get; } = [];

    /// <summary>
    /// Gets the total count of resources created.
    /// </summary>
    public int TotalCreated => CreatedQueues.Count + CreatedContainers.Count;

    /// <summary>
    /// Gets a value indicating whether any resources were created.
    /// </summary>
    public bool HasCreatedResources => TotalCreated > 0;

    /// <summary>
    /// Returns a summary string of the initialization result.
    /// </summary>
    public override string ToString() =>
        $"Created {CreatedQueues.Count} queue(s), {CreatedContainers.Count} container(s)";
}
