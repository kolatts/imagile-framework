using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Imagile.Framework.Configuration.Exceptions;
using Imagile.Framework.Configuration.Extensions;
using Xunit;

namespace Imagile.Framework.Configuration.Tests.Extensions;

public class ConfigurationValidationExtensionsTests
{
    [Fact]
    public void ValidateRecursively_WithValidObject_DoesNotThrow()
    {
        // Arrange
        var testObject = new TestConfiguration
        {
            Name = "ValidName",
            Count = 5,
            Nested = new NestedConfiguration
            {
                Required = "Present"
            }
        };

        // Act
        var act = () => testObject.ValidateRecursively();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateRecursively_WithInvalidObject_ThrowsWithAllErrors()
    {
        // Arrange
        var testObject = new TestConfiguration
        {
            Name = "", // Required - will fail
            Count = 150, // Range 1-100 - will fail
            Nested = new NestedConfiguration
            {
                Required = "Present"
            }
        };

        // Act
        var act = () => testObject.ValidateRecursively();

        // Assert
        act.Should().Throw<ConfigurationValidationException>()
            .WithMessage("*Name*")
            .WithMessage("*Count*");
    }

    [Fact]
    public void ValidateRecursively_WithNestedInvalidObject_IncludesNestedPath()
    {
        // Arrange
        var testObject = new TestConfiguration
        {
            Name = "ValidName",
            Count = 5,
            Nested = new NestedConfiguration
            {
                Required = "" // Required - will fail
            }
        };

        // Act
        var act = () => testObject.ValidateRecursively();

        // Assert
        act.Should().Throw<ConfigurationValidationException>()
            .WithMessage("*Nested.Required*");
    }

    [Fact]
    public void ValidateRecursively_WithNullObject_ThrowsArgumentNullException()
    {
        // Arrange
        object? testObject = null;

        // Act
        var act = () => testObject!.ValidateRecursively();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TryValidateRecursive_WithValidObject_ReturnsTrue()
    {
        // Arrange
        var testObject = new TestConfiguration
        {
            Name = "ValidName",
            Count = 5,
            Nested = new NestedConfiguration
            {
                Required = "Present"
            }
        };
        var results = new List<ValidationResult>();

        // Act
        var isValid = testObject.TryValidateRecursive(results);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Fact]
    public void TryValidateRecursive_WithInvalidObject_ReturnsFalseAndPopulatesResults()
    {
        // Arrange
        var testObject = new TestConfiguration
        {
            Name = "", // Required - will fail
            Count = 150, // Range 1-100 - will fail
            Nested = new NestedConfiguration
            {
                Required = "Present"
            }
        };
        var results = new List<ValidationResult>();

        // Act
        var isValid = testObject.TryValidateRecursive(results);

        // Assert
        isValid.Should().BeFalse();
        results.Should().HaveCountGreaterOrEqualTo(2);
        results.Should().Contain(r => r.MemberNames.Contains("Name"));
        results.Should().Contain(r => r.MemberNames.Contains("Count"));
    }

    [Fact]
    public void TryValidateRecursive_WithNestedInvalidObject_IncludesNestedPathInResults()
    {
        // Arrange
        var testObject = new TestConfiguration
        {
            Name = "ValidName",
            Count = 5,
            Nested = new NestedConfiguration
            {
                Required = "" // Required - will fail
            }
        };
        var results = new List<ValidationResult>();

        // Act
        var isValid = testObject.TryValidateRecursive(results);

        // Assert
        isValid.Should().BeFalse();
        results.Should().ContainSingle();
        results.First().MemberNames.Should().Contain("Nested.Required");
    }

    [Fact]
    public void TryValidateRecursive_WithNullObject_ThrowsArgumentNullException()
    {
        // Arrange
        object? testObject = null;
        var results = new List<ValidationResult>();

        // Act
        var act = () => testObject!.TryValidateRecursive(results);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TryValidateRecursive_WithNullResults_ThrowsArgumentNullException()
    {
        // Arrange
        var testObject = new TestConfiguration
        {
            Name = "ValidName",
            Count = 5,
            Nested = new NestedConfiguration { Required = "Present" }
        };

        // Act
        var act = () => testObject.TryValidateRecursive(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    // Test configuration classes
    private class TestConfiguration
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Range(1, 100)]
        public int Count { get; set; }

        [Required]
        public NestedConfiguration Nested { get; set; } = null!;
    }

    private class NestedConfiguration
    {
        [Required]
        public string Required { get; set; } = string.Empty;
    }
}
