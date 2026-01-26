using System.Reflection;
using FluentAssertions;
using Imagile.Framework.Storage.Initialization;
using Imagile.Framework.Storage.Tests.TestFixtures;
using Xunit;

namespace Imagile.Framework.Storage.Tests.Initialization;

[Trait("Category", "Unit")]
public class StorageResourceScannerTests
{
    [Fact]
    public void ScanForResources_DiscoversQueueMessageTypes()
    {
        // Act
        var resources = StorageResourceScanner.ScanForResources(typeof(TestQueueMessage).Assembly);

        // Assert
        resources.QueueTypes.Should().Contain(typeof(TestQueueMessage));
        resources.QueueTypes.Should().Contain(typeof(ArchiveQueueMessage));
        resources.QueueTypes.Should().Contain(typeof(AnotherQueueMessage));
    }

    [Fact]
    public void ScanForResources_DiscoversBlobContainerTypes()
    {
        // Act
        var resources = StorageResourceScanner.ScanForResources(typeof(TestBlobContainer).Assembly);

        // Assert
        resources.ContainerTypes.Should().Contain(typeof(TestBlobContainer));
        resources.ContainerTypes.Should().Contain(typeof(ArchiveBlobContainer));
        resources.ContainerTypes.Should().Contain(typeof(AnotherBlobContainer));
    }

    [Fact]
    public void ScanForResources_ExcludesAbstractTypes()
    {
        // Act
        var resources = StorageResourceScanner.ScanForResources(typeof(TestQueueMessage).Assembly);

        // Assert
        resources.QueueTypes.Should().NotContain(t => t.IsAbstract);
        resources.ContainerTypes.Should().NotContain(t => t.IsAbstract);
    }

    [Fact]
    public void ScanForResources_WithNoAssemblies_UsesCallingAssembly()
    {
        // Act
        var resources = StorageResourceScanner.ScanForResources();

        // Assert - should scan this test assembly
        resources.Should().NotBeNull();
    }

    [Fact]
    public void ScanForResources_HasResources_ReturnsTrueWhenTypesFound()
    {
        // Act
        var resources = StorageResourceScanner.ScanForResources(typeof(TestQueueMessage).Assembly);

        // Assert
        resources.HasResources.Should().BeTrue();
        resources.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetQueueName_ReturnsCorrectName()
    {
        // Act
        var queueName = StorageResourceScanner.GetQueueName(typeof(TestQueueMessage));

        // Assert
        queueName.Should().Be("test-queue");
    }

    [Fact]
    public void GetQueueName_WithNullType_ThrowsArgumentNullException()
    {
        // Act
        var act = () => StorageResourceScanner.GetQueueName(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetQueueName_WithInvalidType_ThrowsInvalidOperationException()
    {
        // Arrange - string doesn't have DefaultQueueName
        var invalidType = typeof(string);

        // Act
        var act = () => StorageResourceScanner.GetQueueName(invalidType);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("DefaultQueueName");
    }

    [Fact]
    public void GetContainerName_ReturnsCorrectName()
    {
        // Act
        var containerName = StorageResourceScanner.GetContainerName(typeof(TestBlobContainer));

        // Assert
        containerName.Should().Be("test-container");
    }

    [Fact]
    public void GetContainerName_WithNullType_ThrowsArgumentNullException()
    {
        // Act
        var act = () => StorageResourceScanner.GetContainerName(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetContainerName_WithInvalidType_ThrowsInvalidOperationException()
    {
        // Arrange - string doesn't have DefaultContainerName
        var invalidType = typeof(string);

        // Act
        var act = () => StorageResourceScanner.GetContainerName(invalidType);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("DefaultContainerName");
    }

    [Fact]
    public void GetStorageAccountName_ReturnsNameForAttributedType()
    {
        // Act
        var accountName = StorageResourceScanner.GetStorageAccountName(typeof(ArchiveQueueMessage));

        // Assert
        accountName.Should().Be("archive");
    }

    [Fact]
    public void GetStorageAccountName_ReturnsNullForUnattributedType()
    {
        // Act
        var accountName = StorageResourceScanner.GetStorageAccountName(typeof(TestQueueMessage));

        // Assert
        accountName.Should().BeNull();
    }

    [Fact]
    public void GetStorageAccountName_WithNullType_ThrowsArgumentNullException()
    {
        // Act
        var act = () => StorageResourceScanner.GetStorageAccountName(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void StorageResources_Empty_HasNoResources()
    {
        // Act
        var empty = StorageResources.Empty;

        // Assert
        empty.QueueTypes.Should().BeEmpty();
        empty.ContainerTypes.Should().BeEmpty();
        empty.HasResources.Should().BeFalse();
        empty.TotalCount.Should().Be(0);
    }
}
