using FluentAssertions;
using Imagile.Framework.Core.Attributes;
using Imagile.Framework.Core.Extensions;
using Xunit;

namespace Imagile.Framework.Core.Tests.Attributes;

/// <summary>
/// Tests for DoNotUpdateAttribute detection on properties.
/// </summary>
public class DoNotUpdateAttributeTests
{
    private class TestClass
    {
        public string? RegularProperty { get; set; }

        [DoNotUpdate]
        public string? ProtectedProperty { get; set; }

        [DoNotUpdate]
        public int ReadOnlyValue { get; init; }
    }

    [Fact]
    public void HasDoNotUpdate_ReturnsFalse_ForRegularProperty()
    {
        var property = typeof(TestClass).GetProperty(nameof(TestClass.RegularProperty))!;

        var hasAttribute = property.HasDoNotUpdate();

        hasAttribute.Should().BeFalse();
    }

    [Fact]
    public void HasDoNotUpdate_ReturnsTrue_ForProtectedProperty()
    {
        var property = typeof(TestClass).GetProperty(nameof(TestClass.ProtectedProperty))!;

        var hasAttribute = property.HasDoNotUpdate();

        hasAttribute.Should().BeTrue();
    }

    [Fact]
    public void HasDoNotUpdate_ReturnsTrue_ForInitOnlyProperty()
    {
        var property = typeof(TestClass).GetProperty(nameof(TestClass.ReadOnlyValue))!;

        var hasAttribute = property.HasDoNotUpdate();

        hasAttribute.Should().BeTrue();
    }

    [Fact]
    public void Constructor_CreatesAttribute()
    {
        var attribute = new DoNotUpdateAttribute();

        attribute.Should().NotBeNull();
        attribute.Should().BeAssignableTo<Attribute>();
    }
}
