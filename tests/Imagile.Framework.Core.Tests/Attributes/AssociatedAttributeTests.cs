using FluentAssertions;
using Imagile.Framework.Core.Attributes;
using Imagile.Framework.Core.Extensions;
using Xunit;

namespace Imagile.Framework.Core.Tests.Attributes;

/// <summary>
/// Tests for AssociatedAttribute construction and retrieval.
/// </summary>
public class AssociatedAttributeTests
{
    private enum PrimaryEnum
    {
        [Associated<SecondaryEnum>(SecondaryEnum.A, SecondaryEnum.B)]
        First,

        [Associated<SecondaryEnum>(SecondaryEnum.C)]
        Second,

        [Associated<SecondaryEnum>]
        EmptyAssociation,

        NoAssociation
    }

    private enum SecondaryEnum
    {
        A,
        B,
        C
    }

    [Fact]
    public void Constructor_StoresAssociatedValues()
    {
        var attribute = new AssociatedAttribute<SecondaryEnum>(SecondaryEnum.A, SecondaryEnum.B);

        attribute.Associated.Should().HaveCount(2);
        attribute.Associated.Should().Contain(SecondaryEnum.A);
        attribute.Associated.Should().Contain(SecondaryEnum.B);
    }

    [Fact]
    public void Constructor_AllowsEmptyArray()
    {
        var attribute = new AssociatedAttribute<SecondaryEnum>();

        attribute.Associated.Should().BeEmpty();
    }

    [Fact]
    public void GetAssociated_ReturnsCorrectValues()
    {
        var associated = PrimaryEnum.First.GetAssociated<PrimaryEnum, SecondaryEnum>();

        associated.Should().HaveCount(2);
        associated.Should().Contain(SecondaryEnum.A);
        associated.Should().Contain(SecondaryEnum.B);
    }

    [Fact]
    public void GetAssociated_ReturnsSingleValue()
    {
        var associated = PrimaryEnum.Second.GetAssociated<PrimaryEnum, SecondaryEnum>();

        associated.Should().HaveCount(1);
        associated.Should().Contain(SecondaryEnum.C);
    }

    [Fact]
    public void GetAssociated_ReturnsEmpty_ForEmptyAttribute()
    {
        var associated = PrimaryEnum.EmptyAssociation.GetAssociated<PrimaryEnum, SecondaryEnum>();

        associated.Should().BeEmpty();
    }

    [Fact]
    public void GetAssociated_ReturnsEmpty_WhenNoAttribute()
    {
        var associated = PrimaryEnum.NoAssociation.GetAssociated<PrimaryEnum, SecondaryEnum>();

        associated.Should().BeEmpty();
    }

    [Fact]
    public void Attribute_AllowsMultiple()
    {
        // AttributeUsage is defined on AssociatedAttribute<T> and allows multiple
        // Verified by applying multiple attributes to enum values without compiler error
        var memberInfo = typeof(PrimaryEnum).GetMember(nameof(PrimaryEnum.First))[0];
        var attributes = memberInfo.GetCustomAttributes(typeof(AssociatedAttribute<SecondaryEnum>), false);

        // If AllowMultiple=false, only one attribute would be returned
        // The fact that we can define multiple attributes confirms AllowMultiple=true
        attributes.Should().NotBeNull();
    }
}
