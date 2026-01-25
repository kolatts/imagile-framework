# Coding Conventions

**Analysis Date:** 2026-01-25

## Naming Patterns

**Files:**
- PascalCase for all file names: `InMemoryDatabaseTest.cs`, `ConventionViolation.cs`
- Rule class files end with "Rule": `PropertyNamesMustBePascalCaseRule.cs`, `BooleansMustStartWithPrefixRule.cs`
- Builder pattern classes end with "Builder": `ConventionTestOptionsBuilder.cs`, `RuleExclusionBuilder.cs`
- Infrastructure/base classes prefixed with intent: `InMemoryDatabaseTest.cs`

**Classes:**
- PascalCase with descriptive names
- Abstract base classes are abstract keywords: `DbContextConventionTests`, `InMemoryDatabaseTest<TContext>`
- Rule implementations declare the full rule name: `PropertyNamesMustBePascalCaseRule`, `StringsMustHaveMaxLengthRule`

**Properties:**
- PascalCase: `UserId`, `Name`, `Email`, `IsActive`, `CreatedDate`
- Boolean properties MUST start with prefix: `Is`, `Has`, `Are`, `Does` (e.g., `IsActive`, `IsPublished`)
- DateTime properties MUST end with "Date": `CreatedDate`, `ModifiedDate`, `PublishedDate`
- Foreign key properties MUST end with "Id": `AuthorId`, `BlogPostId`, `UserId`
- Enum properties MUST end with "Type": `RoleType`, `UserRoleType`
- Guid properties (non-keys) MUST end with "Unique"
- All properties in public entities use PascalCase

**Variables and Parameters:**
- Local variables use camelCase: `context`, `violation`, `builder`, `connection`
- Parameter names use camelCase: `entityName`, `propertyName`, `configure`
- Private backing fields use `_camelCase` with underscore prefix: `_contexts`, `_options`, `_connection`

**Types and Enums:**
- PascalCase for type names: `ConventionViolation`, `ExclusionKey`, `ConventionTestOptions`
- Record types use record keyword with PascalCase: `record ExclusionKey(string EntityName, string? PropertyName = null)`
- Enum values use PascalCase: `Admin`, `User`, `Guest`

## Code Style

**Formatting:**
- No explicit formatter specified; follows C# conventions
- File-scoped namespaces enabled: `namespace Imagile.EntityFrameworkCore.Tests;`
- Implicit usings enabled in .csproj: ImplicitUsings set to `enable`
- Nullable reference types enabled: Nullable set to `enable`
- Method signatures are on single line or broken by parameter for clarity

**Linting:**
- No .editorconfig file found; relies on implicit C# conventions
- Project follows .NET 10 conventions and C# 10+ features
- No explicit linter configuration detected

**Project Settings (from .csproj):**
```xml
<TargetFramework>net10.0</TargetFramework>
<ImplicitUsings>enable</ImplicitUsings>
<Nullable>enable</Nullable>
```

## Import Organization

**Order:**
1. System namespaces: `using System.Linq.Expressions;`
2. Microsoft.* namespaces: `using Microsoft.EntityFrameworkCore;`, `using Microsoft.Data.Sqlite;`
3. Project-specific namespaces: `using Imagile.EntityFrameworkCore.Tests;`, `using SampleApp.Data;`

**Path Aliases:**
- No custom path aliases detected
- Uses relative paths in project references

## Error Handling

**Patterns:**
- ArgumentException thrown for invalid expressions: `throw new ArgumentException("Property expression must be a simple member access expression", nameof(propertyExpression));` in `RuleExclusionBuilder.cs`
- Custom validation messages in FluentAssertions: `violations.Should().BeEmpty($"because all entities should comply with the rule: {rule.Name}");` in `DbContextConventionTests.cs`
- Null-coalescing checks for nullable properties: `propertyName is null ? ... : ...`

## Logging

**Framework:** Console/Default diagnostics via assertions

**Patterns:**
- No explicit logging framework used
- Error information conveyed through test assertions and messages
- ConventionViolation records provide formatted string representations: `ToString()` override in `ConventionViolation.cs`

## Comments

**When to Comment:**
- Method-level documentation via XML comments (see below)
- Inline comments used for complex logic or important implementation details
- Example: `// Skip owned entities` in `PrimaryKeysMustBeIntsRule.cs`
- Example: `// Skip composite keys (multiple properties)` in `PrimaryKeysMustBeIntsRule.cs`

**JSDoc/TSDoc (XML Comments in C#):**
- All public classes and methods have XML documentation
- Use `<summary>`, `<param>`, `<typeparam>`, and `<returns>` tags
- Example from `DbContextConventionTests.cs`:
```csharp
/// <summary>
/// Abstract base class for convention tests on DbContexts.
/// </summary>
public abstract class DbContextConventionTests : IAsyncLifetime
{
    /// <summary>
    /// Creates the DbContexts to test.
    /// </summary>
    /// <returns>The DbContexts to validate.</returns>
    protected abstract IEnumerable<DbContext> CreateContexts();
}
```

## Function Design

**Size:** Methods are compact and focused
- Single responsibility principle: each rule validates one convention
- Helper methods extracted for reusability: `GetPropertyName()` in `RuleExclusionBuilder.cs`

**Parameters:**
- Use generics extensively: `ForRule<TRule>()`, `ExcludeEntity<TEntity>()`
- Use expressions for type-safe exclusions: `Expression<Func<TEntity, TProperty>> propertyExpression`
- Short parameter lists with descriptive names

**Return Values:**
- IEnumerable<T> for collections (lazy evaluation): `IEnumerable<ConventionViolation>`
- List<T> for internal working collections
- Fluent builders return `this` or the builder instance for method chaining

## Module Design

**Exports:**
- All public classes are exported for consumption
- Infrastructure base classes in dedicated `Infrastructure` namespace: `InMemoryDatabaseTest.cs` in `Imagile.EntityFrameworkCore.Tests.Infrastructure`
- Rules in dedicated `Rules` namespace
- Configuration builders in `Configuration` namespace

**Namespace Organization:**
- Root: `Imagile.EntityFrameworkCore.Tests`
- Sub-namespaces by concern:
  - `.Infrastructure` - Base classes for testing
  - `.Rules` - Convention rule implementations
  - `.Configuration` - Fluent builder and options classes

**Barrel Files:**
- No barrel/index files used
- Each namespace directly exposes its classes

---

*Convention analysis: 2026-01-25*
