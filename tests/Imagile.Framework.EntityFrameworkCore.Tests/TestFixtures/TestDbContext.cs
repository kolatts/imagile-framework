using Imagile.Framework.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Data.Sqlite;

namespace Imagile.Framework.EntityFrameworkCore.Tests.TestFixtures;

/// <summary>
/// EF Core caches compiled models per DbContext type. To allow different configurations
/// (enableEnumLookupValues, enableDecimalDefaults) to produce different models within the
/// same test run, we implement a custom cache key factory that incorporates the flags.
/// </summary>
internal class TestDbContextModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(Microsoft.EntityFrameworkCore.DbContext context, bool designTime) =>
        context is TestDbContext ctx
            ? (typeof(TestDbContext), ctx.EnableEnumLookupValues, ctx.EnableDecimalDefaults, designTime)
            : (object)(context.GetType(), designTime);
}

/// <summary>
/// Minimal DbContext for testing extensions that work with any DbContext (not just ImagileDbContext).
/// </summary>
public class TestDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    internal readonly bool EnableEnumLookupValues;
    internal readonly bool EnableDecimalDefaults;

    public TestDbContext(
        DbContextOptions<TestDbContext> options,
        bool enableEnumLookupValues = false,
        bool enableDecimalDefaults = false)
        : base(options)
    {
        EnableEnumLookupValues = enableEnumLookupValues;
        EnableDecimalDefaults = enableDecimalDefaults;
    }

    public DbSet<TestPerson> People => Set<TestPerson>();
    public DbSet<TestCategory> Categories => Set<TestCategory>();
    public DbSet<TestOrder> Orders => Set<TestOrder>();
    public DbSet<TestProduct> Products => Set<TestProduct>();
    public DbSet<TestPriceItem> PriceItems => Set<TestPriceItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        if (EnableEnumLookupValues)
        {
            modelBuilder.ConfigureEnumLookupValues();
        }

        if (EnableDecimalDefaults)
        {
            modelBuilder.SetDecimalDefaultPrecisionAndScale(18, 2);
        }
    }

    /// <summary>
    /// Creates a SQLite in-memory context. The connection is kept open for the lifetime of the context.
    /// </summary>
    public static TestDbContext CreateInMemory(bool enableEnumLookupValues = false, bool enableDecimalDefaults = false) =>
        CreateSqlite(enableEnumLookupValues, enableDecimalDefaults);

    /// <summary>Creates a SQLite context backed by a real in-memory SQLite database.</summary>
    public static TestDbContext CreateSqlite(bool enableEnumLookupValues = false, bool enableDecimalDefaults = false)
    {
        // Use an explicit SqliteConnection so the in-memory DB persists for the context lifetime.
        // Register a custom model cache key factory so different flag combinations produce
        // separate compiled models (EF Core's default key is just the DbContext type).
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .ReplaceService<IModelCacheKeyFactory, TestDbContextModelCacheKeyFactory>()
            .Options;

        var context = new TestDbContext(options, enableEnumLookupValues, enableDecimalDefaults);
        context.Database.EnsureCreated();
        return context;
    }
}
