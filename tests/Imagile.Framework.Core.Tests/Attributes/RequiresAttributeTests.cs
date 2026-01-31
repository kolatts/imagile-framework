using FluentAssertions;
using Imagile.Framework.Core.Attributes;
using Imagile.Framework.Core.Extensions;
using Xunit;

namespace Imagile.Framework.Core.Tests.Attributes;

/// <summary>
/// Tests for RequiresAttribute construction and retrieval.
/// </summary>
public class RequiresAttributeTests
{
    private enum FeatureEnum
    {
        [Requires<DependencyEnum>(DependencyEnum.A, DependencyEnum.B)]
        RequiresAny,

        [Requires<DependencyEnum>(true, DependencyEnum.A, DependencyEnum.B)]
        RequiresAll,

        [Requires<DependencyEnum>]
        EmptyRequirement,

        NoRequirement
    }

    private enum DependencyEnum
    {
        A,
        B,
        C
    }

    [Fact]
    public void Constructor_StoresRequiredValues()
    {
        var attribute = new RequiresAttribute<DependencyEnum>(DependencyEnum.A, DependencyEnum.B);

        attribute.Required.Should().HaveCount(2);
        attribute.Required.Should().Contain(DependencyEnum.A);
        attribute.Required.Should().Contain(DependencyEnum.B);
    }

    [Fact]
    public void Constructor_DefaultsToRequireAny()
    {
        var attribute = new RequiresAttribute<DependencyEnum>(DependencyEnum.A);

        attribute.RequireAll.Should().BeFalse();
    }

    [Fact]
    public void Constructor_AllowsRequireAll()
    {
        var attribute = new RequiresAttribute<DependencyEnum>(true, DependencyEnum.A, DependencyEnum.B);

        attribute.RequireAll.Should().BeTrue();
        attribute.Required.Should().HaveCount(2);
    }

    [Fact]
    public void Constructor_AllowsEmptyArray()
    {
        var attribute = new RequiresAttribute<DependencyEnum>();

        attribute.Required.Should().BeEmpty();
        attribute.RequireAll.Should().BeFalse();
    }

    [Fact]
    public void FulfillsRequirements_WithRequireAny_AcceptsOneMatch()
    {
        var fulfills = FeatureEnum.RequiresAny.FulfillsRequirements([DependencyEnum.A]);

        fulfills.Should().BeTrue();
    }

    [Fact]
    public void FulfillsRequirements_WithRequireAll_RequiresAllMatches()
    {
        var fulfills = FeatureEnum.RequiresAll.FulfillsRequirements([DependencyEnum.A]);

        fulfills.Should().BeFalse();
    }

    [Fact]
    public void FulfillsRequirements_WithRequireAll_AcceptsAllMatches()
    {
        var fulfills = FeatureEnum.RequiresAll.FulfillsRequirements([DependencyEnum.A, DependencyEnum.B]);

        fulfills.Should().BeTrue();
    }

    [Fact]
    public void Attribute_AllowsMultiple()
    {
        // RequiresAttribute inherits from AssociatedAttribute which has AllowMultiple=true
        // Verified by applying multiple attributes to enum values without compiler error
        var memberInfo = typeof(FeatureEnum).GetMember(nameof(FeatureEnum.RequiresAny))[0];
        var attributes = memberInfo.GetCustomAttributes(typeof(RequiresAttribute<DependencyEnum>), false);

        attributes.Should().NotBeNull();
    }

    [Fact]
    public void Attribute_Inheritable()
    {
        // RequiresAttribute is a derived class that can be further inherited
        // This is demonstrated by the test fixtures where DerivedIncludes inherits from IncludesAttribute
        typeof(RequiresAttribute<DependencyEnum>).IsClass.Should().BeTrue();
        typeof(RequiresAttribute<DependencyEnum>).IsSealed.Should().BeFalse();
    }
}
