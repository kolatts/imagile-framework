# Testing Patterns

**Analysis Date:** 2026-01-25

## Test Framework

**Runner:**
- xUnit 2.9.3
- Config: No explicit config file; uses default xUnit behavior
- Test projects marked with `<IsTestProject>false</IsTestProject>` for library projects
- Main library: `Imagile.EntityFrameworkCore.Tests.csproj`
- Sample tests: `Samples/SampleApp.Tests/SampleApp.Tests.csproj`

**Assertion Library:**
- FluentAssertions 6.12.0 for readable assertions
- xUnit's built-in [Fact] and [Theory] attributes

**Run Commands:**
```bash
dotnet test                        # Run all tests
dotnet test --watch               # Watch mode (if supported)
dotnet test --collect:"XPlat Code Coverage"  # Generate coverage report
```

## Test File Organization

**Location:**
- Convention tests co-located with implementation: Main library and sample tests in separate test projects
- Infrastructure base classes in `Imagile.EntityFrameworkCore.Tests/Infrastructure/`
- Sample test in `Samples/SampleApp.Tests/DatabaseConventionTests.cs` extends `DbContextConventionTests`

**Naming:**
- Test classes end with "Tests": `DbContextConventionTests`, `InMemoryDatabaseTest<TContext>`, `DatabaseConventionTests`
- Individual test methods use descriptive names with no "Test" suffix: `PrimaryKeysMustBeInts()`, `StringPropertiesMustHaveMaxLength()`, `BooleansMustStartWithPrefix()`

**Structure:**
```
Imagile.EntityFrameworkCore.Tests/
├── Infrastructure/
│   └── InMemoryDatabaseTest.cs          # Base class for in-memory database tests
├── Rules/
│   ├── IConventionRule.cs               # Rule interface
│   ├── PrimaryKeysMustBeIntsRule.cs
│   ├── PropertyNamesMustBePascalCaseRule.cs
│   ├── BooleansMustStartWithPrefixRule.cs
│   └── [13 other rule implementations]
├── Configuration/
│   ├── ConventionTestOptions.cs
│   ├── ConventionTestOptionsBuilder.cs
│   ├── ExclusionKey.cs
│   └── RuleExclusionBuilder.cs
├── DbContextConventionTests.cs          # Base class for convention tests
├── ConventionViolation.cs               # Result model
└── Imagile.EntityFrameworkCore.Tests.csproj

Samples/SampleApp.Tests/
└── DatabaseConventionTests.cs           # Concrete test implementation
```

## Test Structure

**Suite Organization:**
```csharp
// From DbContextConventionTests.cs
public abstract class DbContextConventionTests : IAsyncLifetime
{
    private List<DbContext> _contexts = new();
    private ConventionTestOptions _options = new();

    // Override to provide contexts to test
    protected abstract IEnumerable<DbContext> CreateContexts();

    // Override to configure exclusions
    protected virtual void Configure(ConventionTestOptionsBuilder builder)
    {
        // Override in derived classes to configure exclusions
    }

    // xUnit lifecycle
    public Task InitializeAsync() { /* Setup */ }
    public virtual async Task DisposeAsync() { /* Cleanup */ }

    // Fact-based tests for each rule
    [Fact]
    public void PrimaryKeysMustBeInts() => RunRule<PrimaryKeysMustBeIntsRule>();

    private void RunRule<TRule>() where TRule : IConventionRule, new()
    {
        var rule = new TRule();
        var violations = rule.Validate(_contexts, _options).ToList();
        violations.Should().BeEmpty(
            $"because all entities should comply with the rule: {rule.Name}");
    }
}
```

**Patterns:**

**Setup (InitializeAsync):**
- Contexts created via `CreateContexts()` abstract method
- Options built via fluent `ConventionTestOptionsBuilder`
- Uses xUnit's `IAsyncLifetime` for async setup/teardown
- Example from `DatabaseConventionTests.cs`:
```csharp
protected override IEnumerable<DbContext> CreateContexts()
{
    _connection = new SqliteConnection("DataSource=:memory:");
    _connection.Open();

    var options = new DbContextOptionsBuilder<SampleDbContext>()
        .UseSqlite(_connection)
        .Options;

    var context = new SampleDbContext(options);
    context.Database.EnsureCreated();

    yield return context;
}
```

**Teardown (DisposeAsync):**
- Base class disposes all contexts: `await context.DisposeAsync();`
- Derived classes clean up additional resources
- Example from `DatabaseConventionTests.cs`:
```csharp
public override async Task DisposeAsync()
{
    await base.DisposeAsync();

    if (_connection is not null)
    {
        await _connection.DisposeAsync();
    }
}
```

**Assertion Pattern:**
- FluentAssertions for readable assertions
- Each test calls `RunRule<TRule>()` which:
  1. Instantiates the rule
  2. Calls `rule.Validate(_contexts, _options)`
  3. Asserts no violations: `violations.Should().BeEmpty(reason)`
- Example from `DbContextConventionTests.cs`:
```csharp
private void RunRule<TRule>() where TRule : IConventionRule, new()
{
    var rule = new TRule();
    var violations = rule.Validate(_contexts, _options).ToList();

    violations.Should().BeEmpty(
        $"because all entities should comply with the rule: {rule.Name}");
}
```

## Mocking

**Framework:** No mocking framework used

**Strategy:**
- Uses real in-memory SQLite databases instead of mocks
- `InMemoryDatabaseTest<TContext>` provides real database instance for each test
- Connection string: `"DataSource=:memory:"` for transient, isolated databases

**Pattern from InMemoryDatabaseTest.cs:**
```csharp
public abstract class InMemoryDatabaseTest<TContext> : IAsyncLifetime
    where TContext : DbContext
{
    private SqliteConnection? _connection;
    protected TContext Context { get; private set; } = null!;
    protected DbContextOptions<TContext> ContextOptions { get; private set; } = null!;

    protected abstract TContext CreateContext(DbContextOptions<TContext> options);

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        ContextOptions = new DbContextOptionsBuilder<TContext>()
            .UseSqlite(_connection)
            .Options;

        Context = CreateContext(ContextOptions);
        await Context.Database.EnsureCreatedAsync();
    }

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
```

**What to Test:**
- Convention violations (presence and absence)
- Exclusion logic (entity-level and property-level)
- Multiple DbContext scenarios
- Custom entity configurations

**What NOT to Mock:**
- DbContext instances - use real in-memory SQLite
- DbSet collections - use real EF Core context
- Reflection operations - test actual ELM metadata

## Fixtures and Factories

**Test Data:**
- Sample data defined in entity classes in `Samples/SampleApp.Data/Entities/`
- Example entity from `User.cs`:
```csharp
public class User
{
    public int UserId { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public UserRoleType RoleType { get; set; }
}
```

- Database configuration in `SampleDbContext.cs`:
```csharp
modelBuilder.Entity<User>(entity =>
{
    entity.HasKey(e => e.UserId);
    entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
    entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
    entity.Property(e => e.IsActive).IsRequired();
    entity.Property(e => e.CreatedDate).IsRequired();
    entity.Property(e => e.RoleType).IsRequired();
});
```

**Location:**
- `Samples/SampleApp.Data/` - Contains entity definitions and DbContext
- `Samples/SampleApp.Tests/` - Contains convention test implementations

## Coverage

**Requirements:** Not explicitly enforced

**View Coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
# Coverage report generated in TestResults/ directory
```

## Test Types

**Convention Tests (Primary):**
- Scope: Validate database design and naming conventions across all entities
- Approach: Abstract base class `DbContextConventionTests` provides test methods for 13 built-in rules
- Each rule is a [Fact] test method
- Location: Classes inheriting from `DbContextConventionTests`
- Examples:
  - `PrimaryKeysMustBeInts()` - Validates int/long primary keys
  - `StringPropertiesMustHaveMaxLength()` - Validates max length configuration
  - `BooleansMustStartWithPrefix()` - Validates Is/Has/Are/Does prefix
  - `ForeignKeysMustEndWithId()` - Validates foreign key naming

**Database Tests (Secondary):**
- Scope: Test data access and repository operations
- Approach: Inherit from `InMemoryDatabaseTest<TContext>`
- Location: Any test class testing DbContext operations
- Pattern: Setup in InitializeAsync, assertions in test methods

## Convention Rule Pattern

**Rule Implementation:**
Each rule implements `IConventionRule` interface:
```csharp
public interface IConventionRule
{
    string Name { get; }
    IEnumerable<ConventionViolation> Validate(
        IEnumerable<DbContext> contexts,
        ConventionTestOptions options);
}
```

**Typical Rule Implementation (from PrimaryKeysMustBeIntsRule.cs):**
```csharp
public class PrimaryKeysMustBeIntsRule : IConventionRule
{
    public string Name => "Primary keys must be int or long";

    public IEnumerable<ConventionViolation> Validate(
        IEnumerable<DbContext> contexts,
        ConventionTestOptions options)
    {
        var violations = new List<ConventionViolation>();

        foreach (var context in contexts)
        {
            var contextName = context.GetType().Name;

            foreach (var entityType in context.Model.GetEntityTypes())
            {
                // Skip owned entities
                if (entityType.IsOwned())
                    continue;

                var entityName = entityType.ClrType.Name;

                // Check if entity is excluded
                if (options.IsExcluded<PrimaryKeysMustBeIntsRule>(entityName))
                    continue;

                var primaryKey = entityType.FindPrimaryKey();
                if (primaryKey is null || primaryKey.Properties.Count > 1)
                    continue;

                var keyProperty = primaryKey.Properties.Single();
                var propertyName = keyProperty.Name;

                // Check if property is excluded
                if (options.IsExcluded<PrimaryKeysMustBeIntsRule>(entityName, propertyName))
                    continue;

                var clrType = keyProperty.ClrType;

                // Allow int and long (including nullable versions)
                if (clrType != typeof(int) && clrType != typeof(int?) &&
                    clrType != typeof(long) && clrType != typeof(long?))
                {
                    violations.Add(new ConventionViolation(contextName, entityName, propertyName));
                }
            }
        }

        return violations;
    }
}
```

## Exclusion Configuration Pattern

**Using Fluent Builder:**
```csharp
protected override void Configure(ConventionTestOptionsBuilder builder)
{
    builder
        // Exclude specific entity from all rules
        .ExcludeEntityFromAllRules<__EFMigrationsHistory>()

        // Exclude properties from specific rule
        .ForRule<ProhibitNullableStringsRule>(rule => rule
            .ExcludeProperty<User, string?>(u => u.OptionalField))

        // Exclude entity from specific rule
        .ForRule<StringsMustHaveMaxLengthRule>(rule => rule
            .ExcludeEntity<AuditLog>())

        // String-based exclusions
        .ForRule<SomeRule>(rule => rule
            .ExcludeEntity("SomeEntity")
            .ExcludeProperty("Entity", "PropertyName"));
}
```

---

*Testing analysis: 2026-01-25*
