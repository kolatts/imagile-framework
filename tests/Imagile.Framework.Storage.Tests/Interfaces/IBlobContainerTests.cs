using FluentAssertions;
using Imagile.Framework.Storage.Interfaces;
using Imagile.Framework.Storage.Tests.TestFixtures;
using Xunit;

namespace Imagile.Framework.Storage.Tests.Interfaces;

[Trait("Category", "Unit")]
public class IBlobContainerTests
{
    [Fact]
    public void DefaultContainerName_ReturnsExpectedValue()
    {
        // Act
        var containerName = TestBlobContainer.DefaultContainerName;

        // Assert
        containerName.Should().Be("test-container");
    }

    [Fact]
    public void DefaultContainerName_AccessibleViaGenericConstraint()
    {
        // Act
        var containerName = GetContainerName<TestBlobContainer>();

        // Assert
        containerName.Should().Be("test-container");
    }

    [Fact]
    public void MultipleImplementations_HaveDistinctContainerNames()
    {
        // Act
        var testContainerName = TestBlobContainer.DefaultContainerName;
        var archiveContainerName = ArchiveBlobContainer.DefaultContainerName;
        var anotherContainerName = AnotherBlobContainer.DefaultContainerName;

        // Assert
        testContainerName.Should().NotBe(archiveContainerName);
        testContainerName.Should().NotBe(anotherContainerName);
        archiveContainerName.Should().NotBe(anotherContainerName);
    }

    private static string GetContainerName<T>() where T : IBlobContainer
    {
        return T.DefaultContainerName;
    }
}
