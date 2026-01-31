using FluentAssertions;
using Imagile.Framework.Core.Extensions;
using Imagile.Framework.Core.Tests.TestFixtures;
using Xunit;

namespace Imagile.Framework.Core.Tests.Extensions;

/// <summary>
/// Tests for EnumExtensions methods, validating attribute retrieval and enum operations.
/// Migrated from Arcoro.One.IntegrationTests.Domain.Extensions.EnumExtensionTests.
/// </summary>
public class EnumExtensionTests
{
    [Fact]
    public void GetIncluded_Succeeds()
    {
        var included = TestEnumTypes.B.GetIncluded().ToList();

        included.Should().HaveCount(1);
        included.Should().Contain(TestEnumTypes.A);
    }

    [Fact]
    public void GetIncluded_Succeeds_Empty()
    {
        var included = TestEnumTypes.A.GetIncluded().ToList();

        included.Should().BeEmpty();
    }

    [Fact]
    public void GetIncluded_Succeeds_WithRecursion()
    {
        var included = TestEnumTypes.C.GetIncluded().ToList();

        included.Should().HaveCount(2);
        included.Should().Contain(TestEnumTypes.A);
        included.Should().Contain(TestEnumTypes.B);
    }

    [Fact]
    public void GetIncluded_DerivedAttribute_Succeeds()
    {
        var included = TestEnumTypes.E.GetIncluded().ToList();

        included.Should().HaveCount(1);
        included.Should().Contain(TestEnumTypes.D);
    }

    [Fact]
    public void FulfillsRequirements_ReturnsTrue_WhenNoAttributePresent()
    {
        var value = TestEnumTypes.C;

        var result = value.FulfillsRequirements(new List<TestEnumAlternativeTypes>());

        result.Should().BeTrue();
    }

    [Fact]
    public void FulfillsRequirements_WhenRequiresAllFalse_ReturnsTrue_WhenAnyRequirement_Provided()
    {
        var value = TestEnumTypes.A;

        var result = value.FulfillsRequirements([TestEnumAlternativeTypes.Z]);

        result.Should().BeTrue();
    }

    [Fact]
    public void FulfillsRequirements_WhenRequiresAllFalse_ReturnsFalse_WhenNoRequirement_Provided()
    {
        var value = TestEnumTypes.A;

        var result = value.FulfillsRequirements([TestEnumAlternativeTypes.X]);

        result.Should().BeFalse();
    }

    [Fact]
    public void FulfillsRequirements_WhenRequiresAllTrue_ReturnsFalse_WhenNoRequirements_Provided()
    {
        var value = TestEnumTypes.G;

        var result = value.FulfillsRequirements(new List<TestEnumAlternativeTypes>());

        result.Should().BeFalse();
    }

    [Fact]
    public void FulfillsRequirements_WhenRequiresAllTrue_ReturnsFalse_WhenNotAllRequirements_Provided()
    {
        var value = TestEnumTypes.G;

        var result = value.FulfillsRequirements([TestEnumAlternativeTypes.Z]);

        result.Should().BeFalse();
    }

    [Fact]
    public void FulfillsRequirements_WhenRequiresAllTrue_ReturnsTrue_WhenAllRequirements_Provided()
    {
        var value = TestEnumTypes.G;

        var result = value.FulfillsRequirements([TestEnumAlternativeTypes.Z, TestEnumAlternativeTypes.Y]);

        result.Should().BeTrue();
    }

    [Fact]
    public void GetAssociated_Succeeds()
    {
        var value = TestEnumTypes.D;

        var result = value.GetAssociated<TestEnumTypes, TestEnumAlternativeTypes>();

        result.Should().HaveCount(1).And.Subject.Single().Should().Be(TestEnumAlternativeTypes.X);
    }

    [Fact]
    public void AddFlag_AddsFlagCorrectly()
    {
        var value = TestFlagsEnum.First;
        var result = value.AddFlag(TestFlagsEnum.Second);

        result.HasFlag(TestFlagsEnum.First).Should().BeTrue();
        result.HasFlag(TestFlagsEnum.Second).Should().BeTrue();
        result.HasFlag(TestFlagsEnum.Third).Should().BeFalse();
    }

    [Fact]
    public void AddFlag_DuplicateFlag_DoesNotChangeValue()
    {
        var value = TestFlagsEnum.First | TestFlagsEnum.Second;
        var result = value.AddFlag(TestFlagsEnum.Second);

        result.Should().Be(value);
    }

    [Fact]
    public void RemoveFlag_RemovesFlagCorrectly()
    {
        var value = TestFlagsEnum.First | TestFlagsEnum.Second;
        var result = value.RemoveFlag(TestFlagsEnum.Second);

        result.HasFlag(TestFlagsEnum.First).Should().BeTrue();
        result.HasFlag(TestFlagsEnum.Second).Should().BeFalse();
    }

    [Fact]
    public void RemoveFlag_RemovingNonPresentFlag_DoesNotChangeValue()
    {
        var value = TestFlagsEnum.First;
        var result = value.RemoveFlag(TestFlagsEnum.Second);

        result.Should().Be(value);
    }

    [Fact]
    public void AddFlag_ThrowsException_IfNotFlagsEnum()
    {
        Action act = () => TestEnumTypes.A.AddFlag(TestEnumTypes.B);
        act.Should().Throw<ArgumentException>().WithMessage("*[Flags]*");
    }

    [Fact]
    public void RemoveFlag_ThrowsException_IfNotFlagsEnum()
    {
        Action act = () => TestEnumTypes.A.RemoveFlag(TestEnumTypes.B);
        act.Should().Throw<ArgumentException>().WithMessage("*[Flags]*");
    }
}
