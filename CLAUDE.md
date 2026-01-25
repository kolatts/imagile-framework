# CLAUDE.md

This file provides workflow guidance to Claude Code when working with code in this repository.

## Code Style and Conventions

### Naming Standards

**No Abbreviations:**
- Do NOT use abbreviations in class names, method names, or properties
- Examples of what to avoid:
  - ❌ `CookieMgr` → ✅ `CookieManager`
  - ❌ `AppCtx` → ✅ `ApplicationContext`
  - ❌ `ConfigSvc` → ✅ `ConfigurationService`
  - ❌ `UserRpt` → ✅ `UserReport`
  - ❌ `DbCtx` → ✅ `DbContext` (exception: EF Core convention)

**Clarity over Brevity:**
- Use full, descriptive names even if they're longer
- The only acceptable abbreviations are well-established ones like:
  - `Id` (Identifier)
  - `Dto` (Data Transfer Object)
  - `Api` (Application Programming Interface)
  - `Ui` (User Interface)
  - `Url` (Uniform Resource Locator)

### C# Conventions

**PascalCase:**
- Classes, interfaces, methods, properties, enums
- Examples: `ApplicationInsights`, `TelemetryClient`, `TrackEvent`

**camelCase:**
- Local variables, parameters, private fields (with `_` prefix for private fields)
- Examples: `telemetryItem`, `_jsRuntime`, `connectionString`

**File Organization:**
- One public type per file
- File name matches the type name exactly
- Organize related types into folders (Interfaces/, Models/, Extensions/, etc.)

### XML Documentation

**Required for all public APIs:**
```csharp
/// <summary>
/// Tracks a custom event to Application Insights.
/// </summary>
/// <param name="eventTelemetry">The event data to track.</param>
/// <remarks>
/// Use this method to track user actions, business events, or other custom metrics.
/// Events appear in the Application Insights portal under Custom Events.
/// </remarks>
/// <example>
/// <code>
/// await appInsights.TrackEvent(new EventTelemetry
/// {
///     Name = "ButtonClicked",
///     Properties = new Dictionary&lt;string, object?&gt;
///     {
///         { "buttonId", "submit" }
///     }
/// });
/// </code>
/// </example>
public async Task TrackEvent(EventTelemetry eventTelemetry)
```

**Documentation Standards:**
- `<summary>` - Brief description (1-2 sentences)
- `<param>` - Describe each parameter
- `<returns>` - Describe return value (if applicable)
- `<remarks>` - Additional context, usage notes, warnings
- `<example>` - Show realistic usage with `<code>` blocks
- `<exception>` - Document thrown exceptions

### Namespace Organization

**Framework Packages:**
- `Imagile.Framework.Core` - Zero-dependency foundational types
- `Imagile.Framework.EntityFrameworkCore` - EF Core extensions and patterns
- `Imagile.Framework.Blazor.ApplicationInsights` - Blazor WASM telemetry

**Folder-to-Namespace Mapping:**
- Each folder becomes part of the namespace
- Example: `Models/Context/UserContext.cs` → `Imagile.Framework.Blazor.ApplicationInsights.Models.Context`

## Git Workflow

### Commit Message Format

**Follow conventional commits with Co-Authored-By:**
```
<type>(<scope>): <description>

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
```

**Types:**
- `feat` - New feature
- `fix` - Bug fix
- `docs` - Documentation only
- `refactor` - Code restructuring without functional change
- `test` - Adding or updating tests
- `chore` - Maintenance tasks, build changes

**Examples:**
```
feat(blazor): add Application Insights telemetry support

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
```

```
fix(ef-core): correct audit timestamp timezone handling

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
```

### Pre-Commit Checklist

Before committing:
1. Build solution: `dotnet build Imagile.Framework.sln`
2. Ensure no compilation errors or warnings
3. Review git diff to confirm only intended changes staged
4. Verify commit message follows format

## Build and Test Commands

### Basic Operations

```bash
# Build entire solution
dotnet build Imagile.Framework.sln

# Build specific package
dotnet build src/Imagile.Framework.Core

# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Imagile.Framework.Core.Tests
```

### Package Operations

```bash
# Create NuGet packages (all projects marked IsPackable=true)
dotnet pack Imagile.Framework.sln -c Release -o ./artifacts

# Create package for specific project
dotnet pack src/Imagile.Framework.Core -c Release -o ./artifacts
```

### Local Package Testing

```bash
# Add local package source (one-time setup)
dotnet nuget add source C:\Code\imagile-framework\artifacts --name local-framework

# Reference local package in consuming project
dotnet add package Imagile.Framework.Core --version 0.0.1-alpha.1 --source local-framework
```

## Project Structure

```
imagile-framework/
├── src/
│   ├── Imagile.Framework.Core/                           # Zero-dependency package
│   ├── Imagile.Framework.EntityFrameworkCore/            # EF Core extensions
│   └── Imagile.Framework.Blazor.ApplicationInsights/     # Blazor WASM telemetry
├── tests/
│   ├── Imagile.Framework.Core.Tests/
│   ├── Imagile.Framework.EntityFrameworkCore.Tests/
│   └── Imagile.Framework.Blazor.ApplicationInsights.Tests/
├── .planning/                                             # GSD workflow artifacts
├── Directory.Build.props                                  # Shared build properties
├── Directory.Packages.props                               # Central Package Management
└── Imagile.Framework.sln
```

## Architecture Principles

### Package Design

**Core Package:**
- Zero external dependencies
- Contains only attributes, interfaces, and base classes
- Designed for maximum reusability

**Specialized Packages:**
- Reference Core package
- Reference external dependencies as needed (EF Core, Application Insights SDK, etc.)
- Follow dependency chain: Core → EntityFrameworkCore/Blazor packages

**Target Framework:**
- .NET 10 only (net10.0)
- No multi-targeting for simplicity
- Native AOT and trimming analysis enabled

### Interface-First Design

**Prefer interfaces for extensibility:**
```csharp
// Good - allows mocking and alternative implementations
public interface IAuditContextProvider<TUserKey, TTenantKey>
{
    TUserKey? UserId { get; }
    TTenantKey? TenantId { get; }
}

// Implementation can vary by application
public class HttpAuditContextProvider : IAuditContextProvider<int, int>
{
    // Uses HttpContext to get current user
}
```

### Generic Type Parameters

**Use generics for flexibility:**
```csharp
// Good - works with any key type (int, Guid, string)
public interface IAuditableEntity<TUserKey> : ITimestampedEntity
{
    TUserKey? CreatedBy { get; set; }
    TUserKey? ModifiedBy { get; set; }
}

// Consumers choose their key type
public class Customer : IAuditableEntity<Guid>
{
    // Uses Guid for user keys
}
```

## Testing Standards

### Test Project Naming

- Production: `Imagile.Framework.{PackageName}`
- Tests: `Imagile.Framework.{PackageName}.Tests`
- Example: `Imagile.Framework.Core.Tests`

### Test Organization

```csharp
public class ApplicationInsightsTests
{
    [Fact]
    public async Task TrackEvent_CallsCorrectJSMethod()
    {
        // Arrange
        var mockJs = new MockJSRuntime();
        var appInsights = new ApplicationInsights();
        appInsights.InitJSRuntime(mockJs);

        // Act
        await appInsights.TrackEvent(new EventTelemetry { Name = "TestEvent" });

        // Assert
        mockJs.Invocations.Should().ContainSingle()
            .Which.Identifier.Should().Be("appInsights.trackEvent");
    }
}
```

**Test Conventions:**
- Use xUnit for test framework
- Use FluentAssertions for assertions
- Method naming: `MethodName_Scenario_ExpectedBehavior`
- Arrange-Act-Assert pattern
- One logical assertion per test

## Quick Reference

### Common Commands

```bash
# Development cycle
dotnet build && dotnet test

# Create packages
dotnet pack -c Release

# Clean build artifacts
dotnet clean
rm -r artifacts/

# Restore packages
dotnet restore
```

### Version Management

**GitVersion Configuration:**
- Strategy: MainLine (ContinuousDeployment)
- Format: `{Major}.{Minor}.{Patch}-{PreReleaseLabel}.{CommitsSinceTag}`
- Example: `0.1.0-alpha.5`

**Versioning:**
- All packages share same version from GitVersion
- Tag releases: `git tag v1.0.0` to create official release
- Development builds: `0.x.y-alpha.z`

### Package References

**In consuming projects:**
```xml
<ItemGroup>
  <PackageReference Include="Imagile.Framework.Core" Version="0.1.0-alpha.5" />
  <PackageReference Include="Imagile.Framework.EntityFrameworkCore" Version="0.1.0-alpha.5" />
</ItemGroup>
```

## Quality Standards

### Code Analysis

- Enable nullable reference types
- Treat warnings as errors in Release builds
- Enable AOT and trimming compatibility analysis
- No compiler warnings allowed

### Documentation

- All public APIs require XML documentation
- Include usage examples in documentation
- Document edge cases and limitations
- Reference related types and methods

### Performance

- Avoid allocations in hot paths
- Use `ValueTask<T>` for frequently-called async methods
- Consider `Span<T>` and `Memory<T>` for array operations
- Profile before optimizing

## NuGet Publishing

**Package Metadata (defined in Directory.Build.props):**
- Authors: "Imagile"
- Company: "Imagile"
- Copyright: "© Imagile. All rights reserved."
- License: MIT
- Repository: https://github.com/imagile/imagile-framework
- Icon: Package icon embedded
- README: Per-package README.md included

**GitHub Actions Workflow:**
- Triggered by version tags: `v*.*.*`
- Builds, tests, packs, and publishes to NuGet.org
- Includes symbol packages (.snupkg) for debugging

## Migration Guidelines

When migrating code into framework packages:

1. **Update namespaces** from source to framework conventions
2. **Expand abbreviations** (CookieMgr → CookieManager)
3. **Enhance XML documentation** beyond original source
4. **Remove source-specific dependencies** if not applicable
5. **Preserve JsonPropertyName attributes** for serialization compatibility
6. **Update file organization** to match framework structure

## Resources

- **GSD Workflow:** `.planning/` directory contains all planning artifacts
- **Build Configuration:** `Directory.Build.props` for shared MSBuild properties
- **Package Management:** `Directory.Packages.props` for centralized versions
- **Architecture Decisions:** Documented in `.planning/STATE.md`
