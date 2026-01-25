using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Imagile.Framework.EntityFrameworkCore.Testing.Infrastructure;

/// <summary>
/// Abstract base class for tests that use an in-memory SQLite database.
/// </summary>
/// <typeparam name="TContext">The DbContext type to test.</typeparam>
public abstract class InMemoryDatabaseTest<TContext> : IAsyncLifetime
    where TContext : DbContext
{
    private SqliteConnection? _connection;

    /// <summary>
    /// Gets the DbContext instance for the test.
    /// </summary>
    protected TContext Context { get; private set; } = null!;

    /// <summary>
    /// Gets the DbContext options used to create the context.
    /// </summary>
    protected DbContextOptions<TContext> ContextOptions { get; private set; } = null!;

    /// <summary>
    /// Creates a new instance of the DbContext with the specified options.
    /// </summary>
    /// <param name="options">The options to use for creating the context.</param>
    /// <returns>A new DbContext instance.</returns>
    protected abstract TContext CreateContext(DbContextOptions<TContext> options);

    /// <summary>
    /// Initializes the test by setting up the in-memory database.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Create and open a connection to an in-memory SQLite database
        // The connection must remain open for the in-memory database to persist
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        // Configure DbContext options to use the in-memory connection
        ContextOptions = new DbContextOptionsBuilder<TContext>()
            .UseSqlite(_connection)
            .Options;

        // Create the context and ensure the database schema is created
        Context = CreateContext(ContextOptions);
        await Context.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Cleans up the test by disposing the database and connection.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (Context is not null)
        {
            await Context.Database.EnsureDeletedAsync();
            await Context.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
