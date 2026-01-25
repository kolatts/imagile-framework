# Phase 02: Core Package - Research

**Researched:** 2026-01-25
**Domain:** .NET attribute design, NuGet package authoring
**Confidence:** HIGH

## Summary

Phase 2 involves creating a zero-dependency Core package containing declarative attributes extracted from imagile-app. The research reveals well-established patterns for attribute design in .NET, including best practices for sealed classes, proper AttributeUsage declarations, XML documentation, and AOT compatibility. The imagile-app codebase contains 11 custom attributes across various patterns (associations, requirements, metadata, validation markers) that can be extracted as a reusable foundation.

The standard approach is to create attribute base classes that follow .NET Framework Design Guidelines: sealed when possible for performance, proper AttributeUsage declarations, required arguments in constructors, optional arguments as properties, and comprehensive XML documentation for IntelliSense. The Core package should have zero dependencies and enable AOT/trimming analysis with IsAotCompatible and IsTrimmable metadata.

**Primary recommendation:** Extract attributes in logical groupings (association/relationship, metadata, validation markers), make all concrete attributes sealed unless designed for inheritance, use primary constructors for required arguments, and document all public APIs with XML comments following recommended tags.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| None | N/A | Zero dependencies | Core package provides foundation types with no external dependencies |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| .NET SDK | 10.0 | Build and target framework | Required for net10.0 target |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Zero dependencies | System.ComponentModel.DataAnnotations | Would add dependency baggage, violates design principle |
| Custom attributes | Existing framework attributes | Wouldn't provide domain-specific semantics needed (AssociatedAttribute, RequiresAttribute) |

**Installation:**
```bash
# No package installation needed - zero dependencies by design
# Consumers will install via:
dotnet add package Imagile.Framework.Core
```

## Architecture Patterns

### Recommended Project Structure
```
src/
├── Imagile.Framework.Core/
│   ├── Attributes/              # All attribute classes
│   │   ├── AssociatedAttribute.cs
│   │   ├── RequiresAttribute.cs
│   │   ├── IncludesAttribute.cs
│   │   ├── CategoryAttribute.cs
│   │   ├── CountAttribute.cs
│   │   ├── NativeNameAttribute.cs
│   │   ├── DoNotUpdateAttribute.cs
│   │   └── HostedAttribute.cs
│   └── Imagile.Framework.Core.csproj
```

### Pattern 1: Generic Association Attributes
**What:** Base attribute classes that establish declarative relationships between enum types using generics.
**When to use:** When you need to express "this enum value is associated with/requires/includes values from another enum type."
**Example:**
```csharp
// Source: imagile-app existing implementation
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class AssociatedAttribute<TEnum>(params TEnum[] associatedWith) : Attribute
    where TEnum : struct, Enum
{
    public TEnum[] Associated { get; } = associatedWith;
}
```

### Pattern 2: Primary Constructor for Required Arguments
**What:** Use C# 12+ primary constructors to accept required arguments, expose as properties.
**When to use:** For all attributes with required parameters.
**Example:**
```csharp
// Source: imagile-app pattern
[AttributeUsage(AttributeTargets.Field)]
public class CategoryAttribute(string category) : Attribute
{
    public string Category { get; } = category;
}
```

### Pattern 3: Sealed Concrete Attributes
**What:** Mark concrete attribute classes as sealed to improve reflection performance during attribute lookup.
**When to use:** When the attribute is not designed as a base class for inheritance.
**Example:**
```csharp
// Source: Microsoft Design Guidelines
[AttributeUsage(AttributeTargets.Property)]
public sealed class DoNotUpdateAttribute : Attribute
{
}
```

### Pattern 4: Optional Arguments via Properties
**What:** Required arguments in constructor, optional arguments as settable properties.
**When to use:** Following .NET Framework Design Guidelines for attributes.
**Example:**
```csharp
// Source: imagile-app RequiresAttribute
public class RequiresAttribute<TEnum> : AssociatedAttribute<TEnum>
    where TEnum : struct, Enum
{
    public RequiresAttribute(bool requireAll, params TEnum[] required) : base(required)
    {
        RequireAll = requireAll;
    }

    public RequiresAttribute(params TEnum[] required) : base(required) { }

    public bool RequireAll { get; init; }
}
```

### Pattern 5: Comprehensive XML Documentation
**What:** Triple-slash (///) XML comments on all public types and members using recommended tags.
**When to use:** All public APIs must be documented for IntelliSense and generated documentation.
**Example:**
```csharp
/// <summary>
/// Specifies that an enum value requires one or more values from another enum type.
/// Extends AssociatedAttribute with "require all" vs "require any" semantics.
/// </summary>
/// <typeparam name="TEnum">The type of enum this value requires</typeparam>
public class RequiresAttribute<TEnum> : AssociatedAttribute<TEnum>
    where TEnum : struct, Enum
{
    /// <summary>
    /// Gets the required enum values (alias for Associated).
    /// </summary>
    public TEnum[] Required => Associated;
}
```

### Anti-Patterns to Avoid
- **Not sealing concrete attributes:** Unsealed attributes have slower reflection lookup (CA1813 warning).
- **Using LicenseUrl metadata:** Deprecated in favor of PackageLicenseExpression for legal clarity.
- **Omitting AttributeUsage:** Results in CA1018 warning and unclear usage semantics.
- **Properties for required arguments:** Required arguments should be constructor parameters, not properties.
- **Missing XML documentation:** Without XML comments, consumers lose IntelliSense guidance.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Package metadata | Manual .nuspec files | MSBuild properties in .csproj | Modern .NET SDK integrates package metadata, automatic README inclusion |
| Documentation generation | Custom HTML generators | DocFX or Sandcastle | Industry-standard tools that consume XML comments |
| Semantic versioning | Manual version bumps | GitVersion (already configured) | Automated semantic versioning from git history |
| NuGet publishing | Manual nuget.exe commands | GitHub Actions workflow (already exists) | Automated CI/CD with proper authentication |

**Key insight:** The .NET SDK and MSBuild provide built-in support for modern NuGet package authoring. Leveraging these avoids reinventing package metadata, versioning, and publishing infrastructure.

## Common Pitfalls

### Pitfall 1: Not Marking Attributes as Sealed
**What goes wrong:** Attribute lookup via reflection is slower for unsealed attributes.
**Why it happens:** Developers forget or don't know about the performance benefit.
**How to avoid:** Follow CA1813 analyzer rule - seal all concrete attributes unless explicitly designed for inheritance.
**Warning signs:** Code analysis warning CA1813 "Avoid unsealed attributes."

### Pitfall 2: Missing or Incorrect AttributeUsage
**What goes wrong:** Attribute can be applied to wrong targets (e.g., class when only Field makes sense), or AllowMultiple semantics are unclear.
**Why it happens:** Developers skip AttributeUsage declaration.
**How to avoid:** Always declare [AttributeUsage] with specific AttributeTargets and explicit AllowMultiple value.
**Warning signs:** Code analysis warning CA1018 "Mark attributes with AttributeUsageAttribute."

### Pitfall 3: Adding Dependencies to Core Package
**What goes wrong:** Core package loses its "zero dependencies" value proposition.
**Why it happens:** Developer adds a "small" dependency for convenience.
**How to avoid:** Strictly enforce zero PackageReference items in Core.csproj. Use code reviews and CI validation.
**Warning signs:** PackageReference elements appearing in Imagile.Framework.Core.csproj.

### Pitfall 4: Inadequate XML Documentation
**What goes wrong:** Poor IntelliSense experience, unclear API contracts, consumers misuse attributes.
**Why it happens:** Developer treats XML comments as optional.
**How to avoid:** Enable `<GenerateDocumentationFile>true</GenerateDocumentationFile>` (already in Directory.Build.props), enforce completeness via code review.
**Warning signs:** Missing /// comments on public types/members, generic undocumented type parameters.

### Pitfall 5: AOT Incompatibility
**What goes wrong:** Attributes work in JIT scenarios but fail in Native AOT compilation.
**Why it happens:** Using reflection-heavy patterns or dynamic code generation in attribute constructors.
**How to avoid:** Keep attribute implementations simple (data-only), avoid reflection in constructors, enable `<IsAotCompatible>true</IsAotCompatible>` and `<EnableTrimAnalyzer>true</EnableTrimAnalyzer>` (already configured).
**Warning signs:** Build warnings about trim or AOT compatibility when analyzers are enabled.

## Code Examples

Verified patterns from imagile-app source code and official documentation:

### Generic Association Attribute (Base Pattern)
```csharp
// Source: /c/Code/imagile-app/Imagile.App.Domain/Attributes/AssociatedAttribute.cs
namespace Imagile.Framework.Core.Attributes;

/// <summary>
/// Specifies that an enum value is associated with one or more values from another enum type.
/// Used to create declarative relationships between enum types.
/// </summary>
/// <typeparam name="TEnum">The type of enum this value is associated with</typeparam>
/// <param name="associatedWith">The enum values this value is associated with</param>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class AssociatedAttribute<TEnum>(params TEnum[] associatedWith) : Attribute
    where TEnum : struct, Enum
{
    /// <summary>
    /// Gets the enum values this value is associated with.
    /// </summary>
    public TEnum[] Associated { get; } = associatedWith;
}
```

### Derived Attribute with Additional Semantics
```csharp
// Source: /c/Code/imagile-app/Imagile.App.Domain/Attributes/RequiresAttribute.cs
namespace Imagile.Framework.Core.Attributes;

/// <summary>
/// Specifies that an enum value requires one or more values from another enum type.
/// Extends AssociatedAttribute with "require all" vs "require any" semantics.
/// </summary>
/// <typeparam name="TEnum">The type of enum this value requires</typeparam>
public class RequiresAttribute<TEnum> : AssociatedAttribute<TEnum>
    where TEnum : struct, Enum
{
    /// <summary>
    /// Initializes a new instance with optional RequireAll semantics.
    /// </summary>
    /// <param name="requireAll">If true, all required values must be present. If false, at least one must be present.</param>
    /// <param name="required">The enum values that are required</param>
    public RequiresAttribute(bool requireAll, params TEnum[] required) : base(required)
    {
        RequireAll = requireAll;
    }

    /// <summary>
    /// Initializes a new instance with default RequireAll=false (any) semantics.
    /// </summary>
    /// <param name="required">The enum values that are required</param>
    public RequiresAttribute(params TEnum[] required) : base(required) { }

    /// <summary>
    /// Gets the required enum values (alias for Associated).
    /// </summary>
    public TEnum[] Required => Associated;

    /// <summary>
    /// Gets whether all required values must be present (true) or just one (false).
    /// Default is false (require any).
    /// </summary>
    public bool RequireAll { get; init; }
}
```

### Simple Sealed Marker Attribute
```csharp
// Source: /c/Code/imagile-app/Imagile.App.Domain/Attributes/DoNotUpdateAttribute.cs
namespace Imagile.Framework.Core.Attributes;

/// <summary>
/// Marks a property that should not be updated during reference data sync operations.
/// Properties with this attribute will only be set during initial insert.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DoNotUpdateAttribute : Attribute
{
}
```

### Metadata Attribute with String Value
```csharp
// Source: /c/Code/imagile-app/Imagile.App.Domain/Attributes/CategoryAttribute.cs
namespace Imagile.Framework.Core.Attributes;

/// <summary>
/// Specifies the category for an enum value.
/// Used for grouping and organizing related enum values.
/// </summary>
/// <param name="category">The category name</param>
[AttributeUsage(AttributeTargets.Field)]
public sealed class CategoryAttribute(string category) : Attribute
{
    /// <summary>
    /// Gets the category name.
    /// </summary>
    public string Category { get; } = category;
}
```

### Metadata Attribute with Integer Value
```csharp
// Source: /c/Code/imagile-app/Imagile.App.Domain/Attributes/CountAttribute.cs
namespace Imagile.Framework.Core.Attributes;

/// <summary>
/// Specifies a count value for an enum member. Can be used for various counting scenarios
/// such as number of items to generate, capacity limits, etc.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class CountAttribute : Attribute
{
    /// <summary>
    /// Gets the count value.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Initializes a new instance with the specified count value.
    /// </summary>
    /// <param name="value">The count value</param>
    public CountAttribute(int value)
    {
        Value = value;
    }
}
```

### Project File Configuration
```xml
<!-- Source: Existing EntityFrameworkCore.Testing.csproj pattern -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>true</IsPackable>

    <!-- NuGet Package Metadata -->
    <PackageId>Imagile.Framework.Core</PackageId>
    <RootNamespace>Imagile.Framework.Core</RootNamespace>
    <AssemblyName>Imagile.Framework.Core</AssemblyName>
    <Description>Zero-dependency declarative attributes for building opinionated .NET applications.</Description>
    <PackageTags>attributes;validation;metadata;declarative;enum</PackageTags>
    <PackageReadmeFile>README.md</PackageReadmeFile>
  </PropertyGroup>

  <!-- Zero dependencies - this section should remain empty -->
  <ItemGroup>
    <!-- No PackageReference elements - Core has zero dependencies -->
  </ItemGroup>

  <ItemGroup>
    <None Include="..\..\README.md" Pack="true" PackagePath="\" />
  </ItemGroup>
</Project>
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Four-part version numbers (1.0.0.0) | Semantic Versioning (1.0.0) | .NET 5+ | Clearer version semantics, NuGet best practice |
| packages.config | PackageReference | .NET Core 1.0+ | Better restore, transitive dependencies, Central Package Management support |
| .nuspec files | MSBuild properties in .csproj | .NET Core SDK | Simplified package authoring, single source of truth |
| LicenseUrl | PackageLicenseExpression | NuGet 4.9+ | Legal clarity, immutable license per version |
| Manual README on NuGet.org | PackageReadmeFile | NuGet 5.10+ | Embedded README in package, versioned documentation |
| Manual attribute documentation | XML doc comments with DocFX | Modern .NET | IntelliSense support, auto-generated API docs |

**Deprecated/outdated:**
- **LicenseUrl**: Deprecated, use PackageLicenseExpression (MIT, Apache-2.0, etc.)
- **Four-part versioning**: Use three-part Semantic Versioning
- **.nuspec files**: Use MSBuild properties in .csproj for modern SDK-style projects

## Open Questions

1. **Should validation-related attributes be in Core or separate Validation package?**
   - What we know: RequiresAttribute has validation semantics, DoNotUpdateAttribute is validation-like
   - What's unclear: Whether validation attributes belong in zero-dependency Core or should wait for a Validation package that might need dependencies
   - Recommendation: Include in Core if they're simple marker attributes with no validation logic, defer complex validation to future package

2. **Should domain-specific specializations (FeatureAttribute, IncludesPermissionsAttribute) be extracted?**
   - What we know: These inherit from base attributes but reference domain types (Permission.Ids, Feature.Ids)
   - What's unclear: Whether these are too application-specific for a reusable framework
   - Recommendation: Extract only the generic base attributes (AssociatedAttribute, IncludesAttribute) to Core, leave domain-specific specializations as examples in documentation

3. **Should HostedAttribute be included?**
   - What we know: Contains ApiUrl and WebUrl properties, seems environment-specific
   - What's unclear: Whether this pattern is reusable across different applications
   - Recommendation: Include if pattern is generic enough (environment configuration metadata), otherwise defer as example pattern

## Sources

### Primary (HIGH confidence)
- [Microsoft .NET Framework Design Guidelines - Attributes](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/attributes) - Official attribute design guidelines
- [Writing Custom Attributes - .NET](https://learn.microsoft.com/en-us/dotnet/standard/attributes/writing-custom-attributes) - Official custom attribute documentation
- [CA1813: Avoid unsealed attributes](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1813) - Performance guidance for sealed attributes
- [CA1018: Mark attributes with AttributeUsageAttribute](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1018) - Required AttributeUsage declaration
- [Package authoring best practices - NuGet](https://learn.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices) - Official NuGet package authoring guidance
- [Dependencies and .NET libraries](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/dependencies) - Guidance on minimizing dependencies
- [Recommended XML documentation tags](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags) - XML comment standards
- [Native AOT deployment overview](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/) - AOT compatibility requirements
- [What's new in the SDK and tooling for .NET 10](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/sdk) - .NET 10 SDK features including NuGet Audit pruning
- imagile-app source code (/c/Code/imagile-app/Imagile.App.Domain/Attributes/*.cs) - Existing attribute implementations to extract

### Secondary (MEDIUM confidence)
- [Add a README to Your NuGet Package - .NET Blog](https://devblogs.microsoft.com/dotnet/add-a-readme-to-your-nuget-package/) - README embedding best practices
- [How to make libraries compatible with native AOT - .NET Blog](https://devblogs.microsoft.com/dotnet/creating-aot-compatible-libraries/) - AOT-compatible library design

### Tertiary (LOW confidence)
- None identified - research primarily relied on official Microsoft documentation

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Zero dependencies is architectural decision, no stack research needed
- Architecture: HIGH - Patterns verified from both imagile-app source and official Microsoft guidelines
- Pitfalls: HIGH - All pitfalls sourced from official code analysis rules and Microsoft documentation

**Research date:** 2026-01-25
**Valid until:** 2027-01-25 (12 months - attribute design patterns are stable)

**Attributes to extract from imagile-app:**
1. AssociatedAttribute<TEnum> - Base association pattern
2. RequiresAttribute<TEnum> - Extends association with require-all/any semantics
3. IncludesAttribute<TEnum> - Self-referential inclusion pattern
4. CategoryAttribute - String metadata
5. CountAttribute - Integer metadata
6. NativeNameAttribute - String metadata for localization
7. DoNotUpdateAttribute - Marker attribute for validation
8. HostedAttribute - Environment metadata (needs evaluation)

**Domain-specific attributes to document as examples but not include:**
1. FeatureAttribute - References domain-specific Feature.Ids enum
2. IncludesPermissionsAttribute - References domain-specific Permission.Ids enum
3. DefaultSecurityRolesAttribute - References domain-specific SecurityRole.Types enum
