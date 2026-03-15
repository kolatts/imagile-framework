using FluentAssertions;
using Xunit;
using Imagile.Framework.EntityFrameworkCore.Entities;
using Imagile.Framework.EntityFrameworkCore.Extensions;
using Imagile.Framework.EntityFrameworkCore.Tests.TestFixtures;

namespace Imagile.Framework.EntityFrameworkCore.Tests.Extensions;

/// <summary>
/// Tests for <see cref="DbContextExtensions.SeedEnumLookupValues"/>.
/// Requires SQLite (not InMemory) because GetTableMappings() is relational-only.
/// </summary>
public class SeedEnumLookupValuesTests
{
    [Fact]
    public void SeedEnumLookupValues_CreatesRowForEachEnumMember()
    {
        using var context = TestDbContext.CreateSqlite(enableEnumLookupValues: true);

        context.SeedEnumLookupValues();

        var lookupValues = context.Set<EnumLookupValue>().ToList();
        var orderStatusValues = lookupValues.Where(v => v.ColumnName == "Status").ToList();

        // OrderStatus has 3 members: Pending, Active, Cancelled
        orderStatusValues.Should().HaveCount(3);
    }

    [Fact]
    public void SeedEnumLookupValues_SetsCorrectIntegerValue()
    {
        using var context = TestDbContext.CreateSqlite(enableEnumLookupValues: true);

        context.SeedEnumLookupValues();

        var pending = context.Set<EnumLookupValue>()
            .FirstOrDefault(v => v.ColumnName == "Status" && v.Name == "Pending");

        pending.Should().NotBeNull();
        pending!.Value.Should().Be(0);
    }

    [Fact]
    public void SeedEnumLookupValues_UsesDescriptionAttributeWhenPresent()
    {
        using var context = TestDbContext.CreateSqlite(enableEnumLookupValues: true);

        context.SeedEnumLookupValues();

        var pending = context.Set<EnumLookupValue>()
            .FirstOrDefault(v => v.ColumnName == "Status" && v.Name == "Pending");

        pending!.Description.Should().Be("Waiting for payment");
    }

    [Fact]
    public void SeedEnumLookupValues_FallsBackToMemberNameWhenNoDescription()
    {
        using var context = TestDbContext.CreateSqlite(enableEnumLookupValues: true);

        context.SeedEnumLookupValues();

        var cancelled = context.Set<EnumLookupValue>()
            .FirstOrDefault(v => v.ColumnName == "Status" && v.Name == "Cancelled");

        cancelled!.Description.Should().Be("Cancelled");
    }

    [Fact]
    public void SeedEnumLookupValues_HandlesNullableEnumColumn()
    {
        using var context = TestDbContext.CreateSqlite(enableEnumLookupValues: true);

        context.SeedEnumLookupValues();

        var nullableStatusValues = context.Set<EnumLookupValue>()
            .Where(v => v.ColumnName == "NullableStatus")
            .ToList();

        // Nullable enum should produce same 3 entries as the non-nullable column
        nullableStatusValues.Should().HaveCount(3);
    }

    [Fact]
    public void SeedEnumLookupValues_IsIdempotent()
    {
        using var context = TestDbContext.CreateSqlite(enableEnumLookupValues: true);

        context.SeedEnumLookupValues();
        context.SeedEnumLookupValues();

        var lookupValues = context.Set<EnumLookupValue>().ToList();
        var orderStatusValues = lookupValues.Where(v => v.ColumnName == "Status").ToList();
        orderStatusValues.Should().HaveCount(3);
    }
}
