using FluentAssertions;
using Imagile.Framework.EntityFrameworkCore.Testing.Configuration;
using Imagile.Framework.EntityFrameworkCore.Testing.Rules;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Imagile.Framework.EntityFrameworkCore.Testing;

/// <summary>
/// Abstract base class for convention tests on DbContexts.
/// </summary>
public abstract class DbContextConventionTests : IAsyncLifetime
{
    private List<DbContext> _contexts = new();
    private ConventionTestOptions _options = new();

    /// <summary>
    /// Creates the DbContexts to test.
    /// </summary>
    /// <returns>The DbContexts to validate.</returns>
    protected abstract IEnumerable<DbContext> CreateContexts();

    /// <summary>
    /// Configures convention test options and exclusions.
    /// </summary>
    /// <param name="builder">The options builder to configure.</param>
    protected virtual void Configure(ConventionTestOptionsBuilder builder)
    {
        // Override in derived classes to configure exclusions
    }

    /// <summary>
    /// Initializes the test by creating contexts and building options.
    /// </summary>
    public Task InitializeAsync()
    {
        _contexts = CreateContexts().ToList();

        var builder = new ConventionTestOptionsBuilder();
        Configure(builder);
        _options = builder.Build();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Cleans up by disposing all contexts.
    /// </summary>
    public virtual async Task DisposeAsync()
    {
        foreach (var context in _contexts)
        {
            await context.DisposeAsync();
        }
        _contexts.Clear();
    }

    [Fact]
    public void PrimaryKeysMustBeInts()
    {
        RunRule<PrimaryKeysMustBeIntsRule>();
    }

    [Fact]
    public void PrimaryKeysCannotBeGuids()
    {
        RunRule<ProhibitGuidPrimaryKeysRule>();
    }

    [Fact]
    public void PropertiesCannotBeNullableBooleans()
    {
        RunRule<ProhibitNullableBooleansRule>();
    }

    [Fact]
    public void StringPropertiesCannotBeNullable()
    {
        RunRule<ProhibitNullableStringsRule>();
    }

    [Fact]
    public void StringPropertiesMustHaveMaxLength()
    {
        RunRule<StringsMustHaveMaxLengthRule>();
    }

    [Fact]
    public void TableNamesMustBePlural()
    {
        RunRule<TableNamesMustBePluralRule>();
    }

    [Fact]
    public void TableNamesMustBePascalCase()
    {
        RunRule<TableNamesMustBePascalCaseRule>();
    }

    [Fact]
    public void PropertyNamesMustBePascalCase()
    {
        RunRule<PropertyNamesMustBePascalCaseRule>();
    }

    [Fact]
    public void ForeignKeysMustEndWithId()
    {
        RunRule<ForeignKeysMustEndWithIdRule>();
    }

    [Fact]
    public void PrimaryKeyMustBeEntityNameId()
    {
        RunRule<PrimaryKeyMustBeEntityNameIdRule>();
    }

    [Fact]
    public void DateTimesMustEndWithDate()
    {
        RunRule<DateTimesMustEndWithDateRule>();
    }

    [Fact]
    public void BooleansMustStartWithPrefix()
    {
        RunRule<BooleansMustStartWithPrefixRule>();
    }

    [Fact]
    public void GuidsMustEndWithUnique()
    {
        RunRule<GuidsMustEndWithUniqueRule>();
    }

    [Fact]
    public void EnumsMustEndWithType()
    {
        RunRule<EnumsMustEndWithTypeRule>();
    }

    private void RunRule<TRule>() where TRule : IConventionRule, new()
    {
        var rule = new TRule();
        var violations = rule.Validate(_contexts, _options).ToList();

        violations.Should().BeEmpty(
            $"because all entities should comply with the rule: {rule.Name}");
    }
}
