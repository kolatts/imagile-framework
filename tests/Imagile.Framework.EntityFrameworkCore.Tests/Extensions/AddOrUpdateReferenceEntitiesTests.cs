using FluentAssertions;
using Xunit;
using Imagile.Framework.EntityFrameworkCore.Extensions;
using Imagile.Framework.EntityFrameworkCore.Tests.TestFixtures;

namespace Imagile.Framework.EntityFrameworkCore.Tests.Extensions;

public class AddOrUpdateReferenceEntitiesTests
{
    [Fact]
    public void AddOrUpdateReferenceEntities_AddsAllSeedDataWhenDatabaseIsEmpty()
    {
        using var context = TestDbContext.CreateInMemory();

        context.AddOrUpdateReferenceEntities<TestCategory>();

        context.Categories.Should().HaveCount(3);
        context.Categories.Should().Contain(c => c.Name == "Electronics");
        context.Categories.Should().Contain(c => c.Name == "Clothing");
        context.Categories.Should().Contain(c => c.Name == "Food");
    }

    [Fact]
    public void AddOrUpdateReferenceEntities_UpdatesExistingRecord()
    {
        using var context = TestDbContext.CreateInMemory();
        context.Categories.Add(new TestCategory { Id = 1, Name = "Old Name" });
        context.SaveChanges();

        context.AddOrUpdateReferenceEntities<TestCategory>();

        var updated = context.Categories.Find(1)!;
        updated.Name.Should().Be("Electronics");
    }

    [Fact]
    public void AddOrUpdateReferenceEntities_DeletesRecordAbsentFromSeedData()
    {
        using var context = TestDbContext.CreateInMemory();
        // Id = 99 is not in GetSeedData() so it should be removed
        context.Categories.AddRange(
            new TestCategory { Id = 1, Name = "Electronics" },
            new TestCategory { Id = 99, Name = "Obsolete" });
        context.SaveChanges();

        context.AddOrUpdateReferenceEntities<TestCategory>();

        context.Categories.Should().NotContain(c => c.Id == 99);
        context.Categories.Should().HaveCount(3);
    }

    [Fact]
    public void AddOrUpdateReferenceEntities_AcceptsExplicitSeedData()
    {
        using var context = TestDbContext.CreateInMemory();
        var customSeed = new List<TestCategory>
        {
            new() { Id = 10, Name = "Custom A" },
            new() { Id = 11, Name = "Custom B" },
        };

        context.AddOrUpdateReferenceEntities<TestCategory>(customSeed);

        context.Categories.Should().HaveCount(2);
        context.Categories.Should().Contain(c => c.Name == "Custom A");
        context.Categories.Should().Contain(c => c.Name == "Custom B");
    }

    [Fact]
    public void AddOrUpdateReferenceEntities_IsIdempotent()
    {
        using var context = TestDbContext.CreateInMemory();

        context.AddOrUpdateReferenceEntities<TestCategory>();
        context.AddOrUpdateReferenceEntities<TestCategory>();

        context.Categories.Should().HaveCount(3);
    }
}
