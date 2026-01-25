# Imagile.Framework.EntityFrameworkCore.Testing

Opinionated convention tests for Entity Framework Core DbContexts with fluent exclusion configuration.

## Features

- **Convention Testing**: Automated tests to enforce database design conventions
- **Fluent Configuration**: Easy-to-use fluent API for excluding entities and properties from rules
- **Multiple DbContext Support**: Test multiple DbContexts in a single test class
- **In-Memory Testing**: Includes a base class for easy in-memory SQLite database testing

## Requirements

- .NET 10.0 or higher

## Installation

```bash
dotnet add package Imagile.Framework.EntityFrameworkCore.Testing
```

## Built-in Convention Rules

### Design Rules

#### PrimaryKeysMustBeIntsRule
Validates that single-property primary keys are of type `int` or `long`. Skips owned entities and composite keys.

#### ProhibitGuidPrimaryKeysRule
Validates that single-property primary keys are not of type `Guid`. Skips owned entities and composite keys.

#### ProhibitNullableBooleansRule
Validates that no properties have type `bool?` (nullable boolean).

#### ProhibitNullableStringsRule
Validates that string properties are not nullable unless explicitly configured as required.

#### StringsMustHaveMaxLengthRule
Validates that all string properties have a maximum length configured via `GetMaxLength()`.

### Naming Rules

#### TableNamesMustBePluralRule
Validates that table names are plural (e.g., "Users", not "User").

#### TableNamesMustBePascalCaseRule
Validates that table names are PascalCase.

#### PropertyNamesMustBePascalCaseRule
Validates that property names are PascalCase.

#### ForeignKeysMustEndWithIdRule
Validates that foreign key properties end with "Id".

#### PrimaryKeyMustBeEntityNameIdRule
Validates that single-property primary keys follow the format `EntityNameId` (e.g., `UserId` for a `User` entity).

#### DateTimesMustEndWithDateRule
Validates that DateTime and DateTimeOffset properties end with "Date" (e.g., `CreatedDate`, `ModifiedDate`).

#### BooleansMustStartWithPrefixRule
Validates that Boolean properties start with "Is", "Has", "Are", or "Does" (e.g., `IsActive`, `HasChildren`).

#### GuidsMustEndWithUniqueRule
Validates that Guid properties (except primary and foreign keys) end with "Unique" (e.g., `UserUnique`).

#### EnumsMustEndWithTypeRule
Validates that Enum properties (except primary and foreign keys) end with "Type" (e.g., `StatusType`, `RoleType`).

## Usage

### Basic Usage

Create a test class that inherits from `DbContextConventionTests`:

```csharp
using Imagile.Framework.EntityFrameworkCore.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public class MyDesignTests : DbContextConventionTests
{
    protected override IEnumerable<DbContext> CreateContexts()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<MyDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new MyDbContext(options);
        context.Database.EnsureCreated();

        yield return context;
    }
}
```

### Configuring Exclusions

You can exclude entities or properties from specific rules or all rules:

```csharp
public class MyDesignTests : DbContextConventionTests
{
    protected override IEnumerable<DbContext> CreateContexts()
    {
        // ... create contexts
    }

    protected override void Configure(ConventionTestOptionsBuilder builder)
    {
        builder
            // Exclude specific properties from a rule
            .ForRule<ProhibitNullableStringsRule>(rule => rule
                .ExcludeProperty<Employee, string?>(e => e.Department)
                .ExcludeProperty<Employee, string?>(e => e.JobTitle))

            // Exclude entire entities from a rule
            .ForRule<StringsMustHaveMaxLengthRule>(rule => rule
                .ExcludeEntity<AuditLog>())

            // Exclude an entity from ALL rules
            .ExcludeEntityFromAllRules<__EFMigrationsHistory>();
    }
}
```

### String-based Exclusions

You can also use string-based exclusions if you don't have compile-time access to the entity types:

```csharp
protected override void Configure(ConventionTestOptionsBuilder builder)
{
    builder
        .ForRule<ProhibitNullableStringsRule>(rule => rule
            .ExcludeProperty("Employee", "Department")
            .ExcludeEntity("AuditLog"))
        .ExcludeEntityFromAllRules("__EFMigrationsHistory");
}
```

### Multiple DbContexts

Test multiple DbContexts in a single test class:

```csharp
protected override IEnumerable<DbContext> CreateContexts()
{
    // First context
    var connection1 = new SqliteConnection("DataSource=:memory:");
    connection1.Open();
    var context1 = new CompanyDbContext(
        new DbContextOptionsBuilder<CompanyDbContext>()
            .UseSqlite(connection1)
            .Options);
    context1.Database.EnsureCreated();
    yield return context1;

    // Second context
    var connection2 = new SqliteConnection("DataSource=:memory:");
    connection2.Open();
    var context2 = new SharedDbContext(
        new DbContextOptionsBuilder<SharedDbContext>()
            .UseSqlite(connection2)
            .Options);
    context2.Database.EnsureCreated();
    yield return context2;
}
```

## In-Memory Database Testing

The package includes an `InMemoryDatabaseTest<TContext>` base class for easy in-memory SQLite testing:

```csharp
using Imagile.Framework.EntityFrameworkCore.Testing.Infrastructure;
using Microsoft.EntityFrameworkCore;

public class MyRepositoryTests : InMemoryDatabaseTest<MyDbContext>
{
    protected override MyDbContext CreateContext(DbContextOptions<MyDbContext> options)
    {
        return new MyDbContext(options);
    }

    [Fact]
    public async Task MyTest()
    {
        // Context is already initialized and ready to use
        var entity = new MyEntity { Name = "Test" };
        Context.MyEntities.Add(entity);
        await Context.SaveChangesAsync();

        // Assertions...
    }
}
```

## License

MIT
