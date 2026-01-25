# Imagile.Framework.EntityFrameworkCore.Testing - Claude Code Guide

This document provides guidance for working with the Imagile.Framework.EntityFrameworkCore.Testing library using Claude Code.

## Project Structure

```
imagile-framework/
├── src/
│   └── Imagile.Framework.EntityFrameworkCore.Testing/  # Main library project
│       ├── Configuration/                              # Fluent configuration classes
│       │   ├── ConventionTestOptions.cs
│       │   ├── ConventionTestOptionsBuilder.cs
│       │   ├── ExclusionKey.cs
│       │   └── RuleExclusionBuilder.cs
│       ├── Infrastructure/                             # Base classes for testing
│       │   └── InMemoryDatabaseTest.cs
│       ├── Rules/                                      # Convention rule implementations
│       │   ├── IConventionRule.cs
│       │   ├── [Design Rules]
│       │   └── [Naming Rules]
│       ├── ConventionViolation.cs
│       └── DbContextConventionTests.cs
└── README.md
```

## Convention Rules

### Design Rules
- **PrimaryKeysMustBeIntsRule**: Primary keys must be int or long
- **ProhibitGuidPrimaryKeysRule**: Primary keys cannot be Guid
- **ProhibitNullableBooleansRule**: No nullable booleans (bool?)
- **ProhibitNullableStringsRule**: String properties must be required
- **StringsMustHaveMaxLengthRule**: All strings must have max length configured

### Naming Rules
- **TableNamesMustBePluralRule**: Table names must be plural (Users, not User)
- **TableNamesMustBePascalCaseRule**: Table names must be PascalCase
- **PropertyNamesMustBePascalCaseRule**: Property names must be PascalCase
- **ForeignKeysMustEndWithIdRule**: Foreign keys must end with "Id"
- **PrimaryKeyMustBeEntityNameIdRule**: Primary key must be EntityNameId format
- **DateTimesMustEndWithDateRule**: DateTime properties must end with "Date"
- **BooleansMustStartWithPrefixRule**: Booleans must start with Is/Has/Are/Does
- **GuidsMustEndWithUniqueRule**: Guid properties (non-keys) must end with "Unique"
- **EnumsMustEndWithTypeRule**: Enum properties (non-keys) must end with "Type"

## Adding New Rules

To add a new convention rule:

1. Create a new class in `src/Imagile.Framework.EntityFrameworkCore.Testing/Rules/`
2. Implement the `IConventionRule` interface
3. Add a test method in `DbContextConventionTests.cs`
4. Update the README.md with the new rule documentation

Example:

```csharp
namespace Imagile.Framework.EntityFrameworkCore.Testing.Rules;

public class MyCustomRule : IConventionRule
{
    public string Name => "My custom rule description";

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
                var entityName = entityType.ClrType.Name;

                // Check if entity is excluded
                if (options.IsExcluded<MyCustomRule>(entityName))
                {
                    continue;
                }

                // Your validation logic here
                // Add violations as needed:
                // violations.Add(new ConventionViolation(contextName, entityName, propertyName));
            }
        }

        return violations;
    }
}
```

## Building and Publishing

### Building Locally

```bash
cd src/Imagile.Framework.EntityFrameworkCore.Testing
dotnet build -c Release
```

Or build the entire solution:

```bash
dotnet build Imagile.Framework.sln -c Release
```

### Creating NuGet Package

```bash
dotnet pack src/Imagile.Framework.EntityFrameworkCore.Testing -c Release
```

### Publishing to NuGet

The project includes a GitHub Actions workflow that automatically publishes to NuGet when a version tag is pushed:

```bash
git tag v1.0.0
git push origin v1.0.0
```

Alternatively, use the manual workflow dispatch in GitHub Actions.

## Dependencies

- **.NET 10**: Latest .NET version
- **Microsoft.EntityFrameworkCore 10.0.0**: EF Core framework
- **Microsoft.EntityFrameworkCore.Sqlite 10.0.0**: SQLite provider for testing
- **xunit 2.9.3**: Testing framework
- **FluentAssertions 6.12.0**: Fluent assertion library
- **Humanizer.Core 3.0.1**: String manipulation (pluralization, PascalCase, etc.)

## Code Style and Conventions

- Use C# 10+ features (file-scoped namespaces, required properties, etc.)
- Nullable reference types enabled
- Follow PascalCase for class/property names
- Use clear, descriptive names for rules and methods
- Include XML documentation comments for public APIs
- Keep rules focused on a single concern
- Use fluent configuration patterns

## Common Tasks

### Excluding Entities from Rules

Use the `Configure` method in your test class:

```csharp
protected override void Configure(ConventionTestOptionsBuilder builder)
{
    builder
        .ForRule<StringsMustHaveMaxLengthRule>(rule => rule
            .ExcludeEntity<AuditLog>())
        .ExcludeEntityFromAllRules("__EFMigrationsHistory");
}
```

## Troubleshooting

### Test Failures

1. Check the violation message for specific entities/properties
2. Either fix the entity to comply with the rule, or add an exclusion
3. Review the rule implementation if behavior seems incorrect

### NuGet Package Not Building

1. Ensure .NET 10 SDK is installed
2. Check that all dependencies are restored: `dotnet restore`
3. Review build output for specific errors

## Future Enhancements

Potential additions to the library:
- Index naming conventions
- Navigation property naming rules
- Column type validation rules
- Constraint naming conventions
- Schema validation rules
