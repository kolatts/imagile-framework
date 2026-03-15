using FluentAssertions;
using Xunit;
using Imagile.Framework.EntityFrameworkCore.Extensions;
using Imagile.Framework.EntityFrameworkCore.Tests.TestFixtures;

namespace Imagile.Framework.EntityFrameworkCore.Tests.Extensions;

public class SoftDeleteQueryExtensionsTests
{
    [Fact]
    public void WhereNotDeleted_ExcludesSoftDeletedEntities()
    {
        using var context = TestDbContext.CreateInMemory();
        context.People.AddRange(
            new TestPerson { Id = 1, Name = "Alice", IsDeleted = false },
            new TestPerson { Id = 2, Name = "Bob", IsDeleted = true },
            new TestPerson { Id = 3, Name = "Carol", IsDeleted = false });
        context.SaveChanges();

        var result = context.People.WhereNotDeleted().ToList();

        result.Should().HaveCount(2);
        result.Should().NotContain(p => p.IsDeleted);
    }

    [Fact]
    public void WhereNotDeleted_ReturnsAllWhenNoneDeleted()
    {
        using var context = TestDbContext.CreateInMemory();
        context.People.AddRange(
            new TestPerson { Id = 1, Name = "Alice" },
            new TestPerson { Id = 2, Name = "Bob" });
        context.SaveChanges();

        var result = context.People.WhereNotDeleted().ToList();

        result.Should().HaveCount(2);
    }

    [Fact]
    public void WhereNotDeleted_ReturnsEmptyWhenAllDeleted()
    {
        using var context = TestDbContext.CreateInMemory();
        context.People.AddRange(
            new TestPerson { Id = 1, Name = "Alice", IsDeleted = true },
            new TestPerson { Id = 2, Name = "Bob", IsDeleted = true });
        context.SaveChanges();

        var result = context.People.WhereNotDeleted().ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void WhereNotDeleted_WorksOnInMemoryList()
    {
        var people = new List<TestPerson>
        {
            new() { Id = 1, Name = "Alice", IsDeleted = false },
            new() { Id = 2, Name = "Bob", IsDeleted = true },
        }.AsQueryable();

        var result = people.WhereNotDeleted().ToList();

        result.Should().ContainSingle().Which.Name.Should().Be("Alice");
    }
}
