using FluentAssertions;
using Xunit;
using Imagile.Framework.EntityFrameworkCore.Entities;
using Imagile.Framework.EntityFrameworkCore.Tests.TestFixtures;
using Microsoft.EntityFrameworkCore;

namespace Imagile.Framework.EntityFrameworkCore.Tests.Extensions;

public class ModelBuilderExtensionsTests
{
    // ── SetDecimalDefaultPrecisionAndScale ────────────────────────────────────

    [Fact]
    public void SetDecimalDefaultPrecisionAndScale_AppliesDefaultToDecimalWithoutAttributes()
    {
        using var context = TestDbContext.CreateInMemory(enableDecimalDefaults: true);

        var entityType = context.Model.FindEntityType(typeof(TestPriceItem))!;
        var priceProperty = entityType.FindProperty(nameof(TestPriceItem.Price))!;

        priceProperty.GetPrecision().Should().Be(18);
        priceProperty.GetScale().Should().Be(2);
    }

    [Fact]
    public void SetDecimalDefaultPrecisionAndScale_SkipsDecimalWithPrecisionAttribute()
    {
        using var context = TestDbContext.CreateInMemory(enableDecimalDefaults: true);

        var entityType = context.Model.FindEntityType(typeof(TestPriceItem))!;
        var exactPriceProperty = entityType.FindProperty(nameof(TestPriceItem.ExactPrice))!;

        // The [Precision(10,4)] attribute is respected; defaults of 18,2 must NOT be applied
        exactPriceProperty.GetPrecision().Should().Be(10);
        exactPriceProperty.GetScale().Should().Be(4);
    }

    // ── ConfigureEnumLookupValues ─────────────────────────────────────────────

    [Fact]
    public void ConfigureEnumLookupValues_RegistersEnumLookupValueEntityInModel()
    {
        using var context = TestDbContext.CreateInMemory(enableEnumLookupValues: true);

        var entityType = context.Model.FindEntityType(typeof(EnumLookupValue));

        entityType.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureEnumLookupValues_ConfiguresCompositeKey()
    {
        using var context = TestDbContext.CreateInMemory(enableEnumLookupValues: true);

        var entityType = context.Model.FindEntityType(typeof(EnumLookupValue))!;
        var key = entityType.FindPrimaryKey()!;

        key.Properties.Select(p => p.Name)
            .Should().BeEquivalentTo([
                nameof(EnumLookupValue.TableName),
                nameof(EnumLookupValue.ColumnName),
                nameof(EnumLookupValue.Value)
            ]);
    }

    [Fact]
    public void ConfigureEnumLookupValues_UsesCorrectTableName()
    {
        using var context = TestDbContext.CreateSqlite(enableEnumLookupValues: true);

        var entityType = context.Model.FindEntityType(typeof(EnumLookupValue))!;

        entityType.GetTableName().Should().Be("__EnumLookupValues");
    }
}
