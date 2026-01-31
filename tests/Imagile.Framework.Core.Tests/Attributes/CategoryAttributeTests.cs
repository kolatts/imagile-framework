using FluentAssertions;
using Imagile.Framework.Core.Attributes;
using Imagile.Framework.Core.Extensions;
using Xunit;

namespace Imagile.Framework.Core.Tests.Attributes;

/// <summary>
/// Tests for CategoryAttribute construction and retrieval.
/// </summary>
public class CategoryAttributeTests
{
    private enum TestEnum
    {
        [Category("Financial")]
        FinancialReport,

        [Category("Operational")]
        OperationalReport,

        NoCategory
    }

    [Fact]
    public void Constructor_StoresCategory()
    {
        var attribute = new CategoryAttribute("TestCategory");

        attribute.Category.Should().Be("TestCategory");
    }

    [Fact]
    public void GetCategory_ReturnsCorrectValue()
    {
        var category = TestEnum.FinancialReport.GetCategory();

        category.Should().Be("Financial");
    }

    [Fact]
    public void GetCategory_ReturnsDefaultValue_WhenNoAttribute()
    {
        var category = TestEnum.NoCategory.GetCategory();

        category.Should().Be("[None]");
    }

    [Fact]
    public void GetCategory_ReturnsCustomDefaultValue_WhenNoAttribute()
    {
        var category = TestEnum.NoCategory.GetCategory("Unknown");

        category.Should().Be("Unknown");
    }

    [Fact]
    public void GetCategory_ReturnsNull_WhenNoAttributeAndNullDefault()
    {
        var category = TestEnum.NoCategory.GetCategory(null);

        category.Should().BeNull();
    }
}
