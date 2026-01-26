using Imagile.Framework.Storage.Attributes;
using Imagile.Framework.Storage.Interfaces;

namespace Imagile.Framework.Storage.Tests.TestFixtures;

/// <summary>
/// Test blob container for default storage account.
/// </summary>
public class TestBlobContainer : IBlobContainer
{
    public static string DefaultContainerName => "test-container";
}

/// <summary>
/// Test blob container for named storage account.
/// </summary>
[StorageAccount("archive")]
public class ArchiveBlobContainer : IBlobContainer
{
    public static string DefaultContainerName => "archive-container";
}

/// <summary>
/// Another test blob container for scanning tests.
/// </summary>
public class AnotherBlobContainer : IBlobContainer
{
    public static string DefaultContainerName => "another-container";
}
