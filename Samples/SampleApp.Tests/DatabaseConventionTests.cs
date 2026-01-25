using Imagile.EntityFrameworkCore.Tests;
using Imagile.EntityFrameworkCore.Tests.Configuration;
using Imagile.EntityFrameworkCore.Tests.Rules;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SampleApp.Data;

namespace SampleApp.Tests;

/// <summary>
/// Example convention tests for the SampleApp database.
/// This demonstrates how to use Imagile.EntityFrameworkCore.Tests
/// to enforce database design and naming conventions.
/// </summary>
public class DatabaseConventionTests : DbContextConventionTests
{
    private SqliteConnection? _connection;

    protected override IEnumerable<DbContext> CreateContexts()
    {
        // Create an in-memory SQLite database for testing
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SampleDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new SampleDbContext(options);
        context.Database.EnsureCreated();

        yield return context;
    }

    protected override void Configure(ConventionTestOptionsBuilder builder)
    {
        // Example: Exclude the EF Migrations history table from all rules
        builder.ExcludeEntityFromAllRules("__EFMigrationsHistory");

        // Example: If you had specific exceptions to rules, you could configure them here:
        // builder.ForRule<ProhibitNullableStringsRule>(rule => rule
        //     .ExcludeProperty<User, string?>(u => u.OptionalField));
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
