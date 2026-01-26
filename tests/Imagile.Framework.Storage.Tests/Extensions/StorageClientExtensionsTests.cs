using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using FluentAssertions;
using Imagile.Framework.Storage.Extensions;
using Imagile.Framework.Storage.Tests.TestFixtures;
using NSubstitute;
using Xunit;

namespace Imagile.Framework.Storage.Tests.Extensions;

[Trait("Category", "Unit")]
public class StorageClientExtensionsTests
{
    [Fact]
    public void GetQueueClient_WithNullClient_ThrowsArgumentNullException()
    {
        // Arrange
        QueueServiceClient client = null!;

        // Act
        var act = () => client.GetQueueClient<TestQueueMessage>();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("client");
    }

    [Fact]
    public void GetQueueClient_UsesDefaultQueueName()
    {
        // Arrange
        var client = Substitute.For<QueueServiceClient>();
        var expectedQueueClient = Substitute.For<QueueClient>();
        client.GetQueueClient("test-queue").Returns(expectedQueueClient);

        // Act
        var result = client.GetQueueClient<TestQueueMessage>();

        // Assert
        result.Should().BeSameAs(expectedQueueClient);
        client.Received(1).GetQueueClient("test-queue");
    }

    [Fact]
    public void GetBlobContainerClient_WithNullClient_ThrowsArgumentNullException()
    {
        // Arrange
        BlobServiceClient client = null!;

        // Act
        var act = () => client.GetBlobContainerClient<TestBlobContainer>();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("client");
    }

    [Fact]
    public void GetBlobContainerClient_UsesDefaultContainerName()
    {
        // Arrange
        var client = Substitute.For<BlobServiceClient>();
        var expectedContainerClient = Substitute.For<BlobContainerClient>();
        client.GetBlobContainerClient("test-container").Returns(expectedContainerClient);

        // Act
        var result = client.GetBlobContainerClient<TestBlobContainer>();

        // Assert
        result.Should().BeSameAs(expectedContainerClient);
        client.Received(1).GetBlobContainerClient("test-container");
    }
}
