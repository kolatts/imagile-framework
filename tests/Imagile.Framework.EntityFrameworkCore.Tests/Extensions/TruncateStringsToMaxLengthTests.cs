using FluentAssertions;
using Xunit;
using Imagile.Framework.EntityFrameworkCore.Extensions;
using Imagile.Framework.EntityFrameworkCore.Tests.TestFixtures;

namespace Imagile.Framework.EntityFrameworkCore.Tests.Extensions;

public class TruncateStringsToMaxLengthTests
{
    [Fact]
    public void TruncateStringsToMaxLength_TruncatesAddedEntityStringExceedingMaxLength()
    {
        using var context = TestDbContext.CreateInMemory();
        context.Products.Add(new TestProduct { Id = 1, Name = "This is way too long" });

        context.TruncateStringsToMaxLength();

        var entry = context.ChangeTracker.Entries<TestProduct>().Single();
        entry.Property(p => p.Name).CurrentValue.Should().Be("This is wa"); // MaxLength = 10
    }

    [Fact]
    public void TruncateStringsToMaxLength_TruncatesModifiedEntityStringExceedingMaxLength()
    {
        using var context = TestDbContext.CreateInMemory();
        context.Products.Add(new TestProduct { Id = 1, Name = "Short" });
        context.SaveChanges();

        var product = context.Products.Find(1)!;
        product.Name = "An updated name that is too long";

        context.TruncateStringsToMaxLength();

        var entry = context.ChangeTracker.Entries<TestProduct>().Single();
        entry.Property(p => p.Name).CurrentValue.Should().Be("An updated"); // MaxLength = 10
    }

    [Fact]
    public void TruncateStringsToMaxLength_DoesNotTruncateStringWithinMaxLength()
    {
        using var context = TestDbContext.CreateInMemory();
        context.Products.Add(new TestProduct { Id = 1, Name = "Short" });

        context.TruncateStringsToMaxLength();

        var entry = context.ChangeTracker.Entries<TestProduct>().Single();
        entry.Property(p => p.Name).CurrentValue.Should().Be("Short");
    }

    [Fact]
    public void TruncateStringsToMaxLength_DoesNotTruncateStringWithNoMaxLength()
    {
        using var context = TestDbContext.CreateInMemory();
        var longDescription = new string('x', 1000);
        context.Products.Add(new TestProduct { Id = 1, Name = "Test", Description = longDescription });

        context.TruncateStringsToMaxLength();

        var entry = context.ChangeTracker.Entries<TestProduct>().Single();
        entry.Property(p => p.Description).CurrentValue.Should().Be(longDescription);
    }

    [Fact]
    public void TruncateStringsToMaxLength_DoesNotTruncateNullString()
    {
        using var context = TestDbContext.CreateInMemory();
        context.Products.Add(new TestProduct { Id = 1, Name = "Test", Code = null });

        context.TruncateStringsToMaxLength();

        var entry = context.ChangeTracker.Entries<TestProduct>().Single();
        entry.Property(p => p.Code).CurrentValue.Should().BeNull();
    }

    [Fact]
    public void TruncateStringsToMaxLength_TruncatesAllOversizedStringsInSingleEntity()
    {
        using var context = TestDbContext.CreateInMemory();
        context.Products.Add(new TestProduct
        {
            Id = 1,
            Name = "0123456789extra", // MaxLength 10
            Code = "ABCDEFGHIJ"       // MaxLength 5
        });

        context.TruncateStringsToMaxLength();

        var entry = context.ChangeTracker.Entries<TestProduct>().Single();
        entry.Property(p => p.Name).CurrentValue.Should().Be("0123456789");
        entry.Property(p => p.Code).CurrentValue.Should().Be("ABCDE");
    }
}
