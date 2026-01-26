# Imagile.Framework.Core

Zero-dependency declarative attributes for building opinionated .NET applications. Provides association, metadata, and validation marker attributes for creating type-safe, self-documenting code.

## Installation

```bash
dotnet add package Imagile.Framework.Core
```

## Features

- **Association Attributes** - Declare relationships between enum types
  - `[Associated<TEnum>]` - Associate enum value with values from another enum
  - `[Requires<TEnum>]` - Require one or more enum values (all or any semantics)
  - `[Includes<TEnum>]` - Include related enum values

- **Metadata Attributes** - Annotate enum values and properties
  - `[Category]` - Group enum values by category
  - `[Count]` - Associate numeric counts with enum values
  - `[NativeName]` - Map to external system names
  - `[Hosted]` - Mark enum values as hosted/managed

- **Validation Markers** - Control data operations
  - `[DoNotUpdate]` - Prevent property updates during sync/batch operations

## Usage Examples

### Association Attributes - Enum Relationships

Create declarative relationships between enum types using `[Associated<TEnum>]` and `[Requires<TEnum>]`:

```csharp
using Imagile.Framework.Core.Attributes;

public enum Permission
{
    Read,
    Write,
    Delete
}

public enum Feature
{
    // Associated: This feature relates to Read permission
    [Associated<Permission>(Permission.Read)]
    ViewDashboard,

    // Associated: This feature relates to Read AND Write permissions
    [Associated<Permission>(Permission.Read, Permission.Write)]
    EditProfile,

    // Requires: This feature needs Read OR Write permission (any)
    [Requires<Permission>(Permission.Read, Permission.Write)]
    ViewReports,

    // Requires: This feature needs BOTH Read AND Write permissions (all)
    [Requires<Permission>(true, Permission.Read, Permission.Write)]
    EditReports
}
```

**Requires vs Associated:**
- `[Associated<T>]` - Documents relationships (informational)
- `[Requires<T>]` - Enforces requirements (validation)
  - Default: Requires ANY of the specified values
  - `RequireAll=true`: Requires ALL specified values

### Metadata Attributes - Categorization and Naming

Organize and annotate enum values with metadata:

```csharp
public enum ReportType
{
    [Category("Financial")]
    [NativeName("balance_sheet_report")]
    BalanceSheet,

    [Category("Financial")]
    [NativeName("income_statement_report")]
    IncomeStatement,

    [Category("Operational")]
    [Count(12)] // Typical count: 12 monthly reports
    InventoryReport,

    [Category("Operational")]
    [Hosted] // Report is hosted/managed externally
    [NativeName("external_performance_dashboard")]
    PerformanceDashboard
}
```

**Use cases:**
- `[Category]` - Group related values in UI dropdowns or processing logic
- `[NativeName]` - Map to external API names, database values, or legacy system identifiers
- `[Count]` - Associate expected counts or limits with enum values
- `[Hosted]` - Mark values representing externally hosted/managed resources

### Validation Markers - Protecting Properties

Prevent specific properties from being updated during sync or batch operations:

```csharp
public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // Set once during creation, never updated
    [DoNotUpdate]
    public DateTime CreatedDate { get; set; }

    // Preserve original value from external system
    [DoNotUpdate]
    public string OriginalSku { get; set; } = string.Empty;

    // This CAN be updated
    public string CurrentSku { get; set; } = string.Empty;
}
```

**Common scenarios:**
- Timestamps set during creation (`CreatedDate`)
- Historical state preservation (`OriginalSku`, `InitialPrice`)
- External system identifiers that shouldn't change
- Computed values managed separately from sync operations

## Dependencies

**Zero external dependencies** - This package has no third-party dependencies and works with .NET 10+ applications.

## License

MIT License - see LICENSE file for details.

## Contributing

Contributions are welcome! Please open an issue or pull request on GitHub.

## Repository

https://github.com/imagile/imagile-framework
