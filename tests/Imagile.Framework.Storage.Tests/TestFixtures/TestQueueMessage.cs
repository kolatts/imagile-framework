using Imagile.Framework.Storage.Attributes;
using Imagile.Framework.Storage.Interfaces;

namespace Imagile.Framework.Storage.Tests.TestFixtures;

/// <summary>
/// Test queue message for default storage account.
/// </summary>
public class TestQueueMessage : IQueueMessage
{
    public static string DefaultQueueName => "test-queue";

    public int TestId { get; set; }
    public string? TestData { get; set; }
}

/// <summary>
/// Test queue message for named storage account.
/// </summary>
[StorageAccount("archive")]
public class ArchiveQueueMessage : IQueueMessage
{
    public static string DefaultQueueName => "archive-queue";

    public DateTime ArchivedAt { get; set; }
}

/// <summary>
/// Another test queue message for scanning tests.
/// </summary>
public class AnotherQueueMessage : IQueueMessage
{
    public static string DefaultQueueName => "another-queue";
}
