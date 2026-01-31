using FluentAssertions;
using Imagile.Framework.Core.Attributes;
using Imagile.Framework.Core.Extensions;
using Xunit;

namespace Imagile.Framework.Core.Tests.Attributes;

/// <summary>
/// Tests for CountAttribute construction and retrieval.
/// </summary>
public class CountAttributeTests
{
    private enum TestEnum
    {
        [Count(5)]
        FiveItems,

        [Count(0)]
        NoItems,

        [Count(100)]
        ManyItems,

        NoCount
    }

    [Fact]
    public void Constructor_StoresCount()
    {
        var attribute = new CountAttribute(42);

        attribute.Value.Should().Be(42);
    }

    [Fact]
    public void GetCount_ReturnsCorrectValue()
    {
        var count = TestEnum.FiveItems.GetCount();

        count.Should().Be(5);
    }

    [Fact]
    public void GetCount_ReturnsZero_WhenAttributeIsZero()
    {
        var count = TestEnum.NoItems.GetCount();

        count.Should().Be(0);
    }

    [Fact]
    public void GetCount_ReturnsLargeValue()
    {
        var count = TestEnum.ManyItems.GetCount();

        count.Should().Be(100);
    }

    [Fact]
    public void GetCount_ReturnsNull_WhenNoAttribute()
    {
        var count = TestEnum.NoCount.GetCount();

        count.Should().BeNull();
    }
}
