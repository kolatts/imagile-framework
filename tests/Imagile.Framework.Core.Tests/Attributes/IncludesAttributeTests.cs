using FluentAssertions;
using Imagile.Framework.Core.Attributes;
using Imagile.Framework.Core.Extensions;
using Xunit;

namespace Imagile.Framework.Core.Tests.Attributes;

/// <summary>
/// Tests for IncludesAttribute construction and retrieval.
/// </summary>
public class IncludesAttributeTests
{
    private enum HierarchicalEnum
    {
        Base,

        [Includes<HierarchicalEnum>(Base)]
        Level1,

        [Includes<HierarchicalEnum>(Level1)]
        Level2,

        [Includes<HierarchicalEnum>]
        EmptyIncludes,

        NoIncludes
    }

    [Fact]
    public void Constructor_StoresIncludedValues()
    {
        var attribute = new IncludesAttribute<HierarchicalEnum>(HierarchicalEnum.Base, HierarchicalEnum.Level1);

        attribute.Included.Should().HaveCount(2);
        attribute.Included.Should().Contain(HierarchicalEnum.Base);
        attribute.Included.Should().Contain(HierarchicalEnum.Level1);
    }

    [Fact]
    public void Constructor_AllowsEmptyArray()
    {
        var attribute = new IncludesAttribute<HierarchicalEnum>();

        attribute.Included.Should().BeEmpty();
    }

    [Fact]
    public void GetIncluded_ReturnsDirectIncludes()
    {
        var included = HierarchicalEnum.Level1.GetIncluded();

        included.Should().HaveCount(1);
        included.Should().Contain(HierarchicalEnum.Base);
    }

    [Fact]
    public void GetIncluded_ReturnsRecursiveIncludes()
    {
        var included = HierarchicalEnum.Level2.GetIncluded().ToList();

        included.Should().HaveCount(2);
        included.Should().Contain(HierarchicalEnum.Base);
        included.Should().Contain(HierarchicalEnum.Level1);
    }

    [Fact]
    public void GetIncluded_ExcludesSelf()
    {
        var included = HierarchicalEnum.Level1.GetIncluded();

        included.Should().NotContain(HierarchicalEnum.Level1);
    }

    [Fact]
    public void GetIncluded_ReturnsEmpty_ForEmptyAttribute()
    {
        var included = HierarchicalEnum.EmptyIncludes.GetIncluded();

        included.Should().BeEmpty();
    }

    [Fact]
    public void GetIncluded_ReturnsEmpty_WhenNoAttribute()
    {
        var included = HierarchicalEnum.NoIncludes.GetIncluded();

        included.Should().BeEmpty();
    }

    [Fact]
    public void Attribute_Inheritable()
    {
        // IncludesAttribute is a non-sealed class that can be derived from
        // This allows custom attributes like DerivedIncludesAttribute in test fixtures
        typeof(IncludesAttribute<HierarchicalEnum>).IsClass.Should().BeTrue();
        typeof(IncludesAttribute<HierarchicalEnum>).IsSealed.Should().BeFalse();
    }

    [Fact]
    public void DerivedAttribute_WorksWithGetIncluded()
    {
        // This is tested in EnumExtensionTests.GetIncluded_DerivedAttribute_Succeeds
        // Just verify the attribute class itself can be inherited
        typeof(IncludesAttribute<HierarchicalEnum>).IsClass.Should().BeTrue();
        typeof(IncludesAttribute<HierarchicalEnum>).IsSealed.Should().BeFalse();
    }
}
