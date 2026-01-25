# Imagile.EntityFrameworkCore.Tests - Implementation Plan

## Overview

Create a NuGet package that provides opinionated convention tests for EF Core DbContexts with fluent exclusion configuration.

## Project Structure

```
Imagile.EntityFrameworkCore.Tests/
├── Imagile.EntityFrameworkCore.Tests.csproj
├── ConventionViolation.cs
├── DbContextConventionTests.cs
├── Configuration/
│   ├── ExclusionKey.cs
│   ├── RuleExclusionBuilder.cs
│   ├── ConventionTestOptions.cs
│   └── ConventionTestOptionsBuilder.cs
├── Rules/
│   ├── IConventionRule.cs
│   ├── PrimaryKeysMustBeIntsRule.cs
│   ├── ProhibitGuidPrimaryKeysRule.cs
│   ├── ProhibitNullableBooleansRule.cs
│   ├── ProhibitNullableStringsRule.cs
│   └── StringsMustHaveMaxLengthRule.cs
└── Infrastructure/
    └── InMemoryDatabaseTest.cs
```

## Dependencies

- `Microsoft.EntityFrameworkCore` (>= 8.0.0)
- `Microsoft.EntityFrameworkCore.Sqlite` (>= 8.0.0)
- `Microsoft.Data.Sqlite` (>= 8.0.0)
- `xunit` (>= 2.6.0)
- `FluentAssertions` (>= 6.12.0)

## Target Framework

- `net8.0` (or multi-target `net8.0;net9.0` if needed)

---

## File Specifications

### 1. `Imagile.EntityFrameworkCore.Tests.csproj`

Standard class library targeting net8.0. Package metadata for NuGet. References listed dependencies.

### 2. `ConventionViolation.cs`

Record with `ContextName`, `EntityName`, `PropertyName?`. Override `ToString()` to format as `*ContextName* (EntityName) PropertyName`.

### 3. `Configuration/ExclusionKey.cs`

Record with `EntityName` and optional `PropertyName`. Static factory methods `ForEntity()` and `ForProperty()`.

### 4. `Configuration/RuleExclusionBuilder.cs`

Fluent builder with:

- `HashSet<ExclusionKey> Exclusions` (internal)
- `ExcludeEntity<TEntity>()` - generic
- `ExcludeEntity(string entityName)` - string overload
- `ExcludeProperty<TEntity, TProperty>(Expression<Func<TEntity, TProperty>>)` - strongly typed
- `ExcludeProperty(string entityName, string propertyName)` - string overload

### 5. `Configuration/ConventionTestOptions.cs`

Holds:

- `Dictionary<Type, RuleExclusionBuilder> RuleExclusions`
- `HashSet<ExclusionKey> GlobalExclusions`
- `IsExcluded<TRule>(entityName, propertyName?)` method that checks global exclusions first, then rule-specific exclusions

### 6. `Configuration/ConventionTestOptionsBuilder.cs`

Fluent builder with:

- `ForRule<TRule>(Action<RuleExclusionBuilder>)` - configure exclusions for specific rule
- `ExcludeEntityFromAllRules<TEntity>()` - generic
- `ExcludeEntityFromAllRules(string entityName)` - string overload
- `Build()` returns `ConventionTestOptions`

### 7. `Rules/IConventionRule.cs`

Interface with:

- `string Name { get; }`
- `IEnumerable<ConventionViolation> Validate(IEnumerable<DbContext> contexts, ConventionTestOptions options)`

### 8. `Rules/PrimaryKeysMustBeIntsRule.cs`

Validates single-property primary keys are `int` or `long`. Skips owned entities and composite keys.

### 9. `Rules/ProhibitGuidPrimaryKeysRule.cs`

Validates single-property primary keys are not `Guid`. Skips owned entities and composite keys.

### 10. `Rules/ProhibitNullableBooleansRule.cs`

Validates no properties have type `bool?`.

### 11. `Rules/ProhibitNullableStringsRule.cs`

Validates string properties are not nullable unless marked with `[Required]` attribute.

### 12. `Rules/StringsMustHaveMaxLengthRule.cs`

Validates all string properties have `GetMaxLength()` configured.

### 13. `Infrastructure/InMemoryDatabaseTest.cs`

Abstract generic base class `InMemoryDatabaseTest<TContext> where TContext : DbContext`:

- Uses xUnit's `IAsyncLifetime` for setup/teardown
- Manages `SqliteConnection` kept open for in-memory persistence
- `protected TContext Context { get; }`
- `protected DbContextOptions<TContext> ContextOptions { get; }`
- `protected abstract TContext CreateContext(DbContextOptions<TContext> options)`
- `InitializeAsync()` - opens connection, builds options, calls `CreateContext()`, calls `EnsureCreated()`
- `DisposeAsync()` - calls `EnsureDeleted()`, disposes context and connection

### 14. `DbContextConventionTests.cs`

Abstract base class:

- `private List<DbContext> _contexts`
- `private ConventionTestOptions _options`
- `protected abstract IEnumerable<DbContext> CreateContexts()`
- `protected virtual void Configure(ConventionTestOptionsBuilder builder) { }`
- Implements `IAsyncLifetime`:
  - `InitializeAsync()` - calls `CreateContexts()`, builds options via `Configure()`
  - `DisposeAsync()` - disposes all contexts
- Individual `[Fact]` test methods:
  - `PrimaryKeysMustBeInts()`
  - `PrimaryKeysCannotBeGuids()`
  - `PropertiesCannotBeNullableBooleans()`
  - `StringPropertiesCannotBeNullable()`
  - `StringPropertiesMustHaveMaxLength()`
- Private `RunRule<TRule>()` helper that instantiates rule, validates, asserts empty with FluentAssertions

---

## Consumer Usage Example

```csharp
public class MyDesignTests : DbContextConventionTests
{
    protected override IEnumerable<DbContext> CreateContexts()
    {
        var container = new DbContextDependencyContainer(/* ... */);

        var companyConnection = new SqliteConnection("DataSource=:memory:");
        companyConnection.Open();
        var companyOptions = new DbContextOptionsBuilder<CompanyDbContext>()
            .UseSqlite(companyConnection)
            .Options;
        var companyContext = new CompanyDbContext(companyOptions, container);
        companyContext.Database.EnsureCreated();

        var sharedConnection = new SqliteConnection("DataSource=:memory:");
        sharedConnection.Open();
        var sharedOptions = new DbContextOptionsBuilder<SharedDbContext>()
            .UseSqlite(sharedConnection)
            .Options;
        var sharedContext = new SharedDbContext(sharedOptions);
        sharedContext.Database.EnsureCreated();

        yield return companyContext;
        yield return sharedContext;
    }

    protected override void Configure(ConventionTestOptionsBuilder builder)
    {
        builder
            .ForRule<ProhibitNullableStringsRule>(rule => rule
                .ExcludeProperty<Employee, string?>(e => e.Department)
                .ExcludeProperty<Employee, string?>(e => e.JobTitle))
            .ForRule<StringsMustHaveMaxLengthRule>(rule => rule
                .ExcludeEntity<AuditLog>())
            .ExcludeEntityFromAllRules<__EFMigrationsHistory>();
    }
}
```

---

## Implementation Order

1. Create project file with dependencies
2. `ConventionViolation.cs`
3. `Configuration/ExclusionKey.cs`
4. `Configuration/RuleExclusionBuilder.cs`
5. `Configuration/ConventionTestOptions.cs`
6. `Configuration/ConventionTestOptionsBuilder.cs`
7. `Rules/IConventionRule.cs`
8. All rule implementations
9. `Infrastructure/InMemoryDatabaseTest.cs`
10. `DbContextConventionTests.cs`
