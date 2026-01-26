using FluentAssertions;
using Imagile.Framework.Storage.Attributes;
using Xunit;

namespace Imagile.Framework.Storage.Tests.Attributes;

[Trait("Category", "Unit")]
public class StorageAccountAttributeTests
{
    [Fact]
    public void Constructor_WithValidName_SetsNameProperty()
    {
        // Act
        var attribute = new StorageAccountAttribute("archive");

        // Assert
        attribute.Name.Should().Be("archive");
    }

    [Fact]
    public void Constructor_WithNull_ThrowsArgumentException()
    {
        // Act
        var act = () => new StorageAccountAttribute(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEmptyString_ThrowsArgumentException()
    {
        // Act
        var act = () => new StorageAccountAttribute("");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithWhitespace_ThrowsArgumentException()
    {
        // Act
        var act = () => new StorageAccountAttribute("   ");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Attribute_CanBeAppliedToClass()
    {
        // Arrange
        var type = typeof(TestFixtures.ArchiveQueueMessage);

        // Act
        var attribute = type.GetCustomAttributes(typeof(StorageAccountAttribute), false)
            .FirstOrDefault() as StorageAccountAttribute;

        // Assert
        attribute.Should().NotBeNull();
        attribute!.Name.Should().Be("archive");
    }

    [Fact]
    public void Attribute_NotPresentOnUnmarkedClass()
    {
        // Arrange
        var type = typeof(TestFixtures.TestQueueMessage);

        // Act
        var attribute = type.GetCustomAttributes(typeof(StorageAccountAttribute), false)
            .FirstOrDefault() as StorageAccountAttribute;

        // Assert
        attribute.Should().BeNull();
    }
}
