# Multi-Package .NET Library Architecture

**Domain:** Multi-package .NET Framework Library
**Researched:** 2026-01-25
**Confidence:** HIGH

## Executive Summary

Multi-package .NET libraries follow a **layered abstraction pattern** where a zero-dependency Core package provides fundamental abstractions and patterns, while specialized packages (EntityFrameworkCore, BlazorApplicationInsights) build upon Core with framework-specific implementations. This architecture enables:

- **Minimal dependency footprint** for consumers who only need core functionality
- **Pay-for-play model** where specialized packages bring their own dependencies
- **Clear separation of concerns** with unidirectional dependency flow
- **Independent versioning** while maintaining inter-package compatibility

Microsoft's own ecosystem (Microsoft.Extensions.*, Entity Framework Core) demonstrates this pattern successfully at scale.

## Recommended Architecture

### Package Structure

```
YourLibrary/
├── Core/                           # Zero-dependency foundation
│   ├── Abstractions/               # Interfaces, base classes
│   ├── Attributes/                 # Custom attributes
│   ├── Patterns/                   # Reusable patterns
│   └── Core.csproj                 # NO external dependencies
│
├── EntityFrameworkCore/            # EF Core integration
│   ├── Implementations/            # EF-specific implementations
│   ├── Extensions/                 # EF extension methods
│   └── EntityFrameworkCore.csproj  # References: Core + EF deps
│
├── EntityFrameworkCore.Tests/      # Test package
│   └── EntityFrameworkCore.Tests.csproj
│
└── BlazorApplicationInsights/      # Blazor integration
    ├── Components/                 # Blazor components
    ├── Services/                   # Blazor services
    └── BlazorApplicationInsights.csproj  # References: Core + Blazor deps
```

### Package Dependency Graph

```
┌─────────────────────────────────────────┐
│                                         │
│  Core (zero dependencies)               │
│  - Abstractions                         │
│  - Base patterns                        │
│  - Attributes                           │
│                                         │
└──────────────┬──────────────────────────┘
               │ Referenced by
               │
       ┌───────┴────────┐
       │                │
       ▼                ▼
┌──────────────┐  ┌─────────────────────┐
│              │  │                     │
│ EF Core      │  │ BlazorAppInsights  │
│ + EF deps    │  │ + Blazor deps      │
│              │  │                     │
└──────┬───────┘  └─────────────────────┘
       │
       │ Referenced by (test only)
       ▼
┌──────────────┐
│ EF.Tests     │
│ + test deps  │
└──────────────┘
```

**Dependency Direction:** ALWAYS flows inward to Core. No reverse dependencies.

### Build Order

Based on MSBuild dependency resolution, projects build in this order:

1. **Core** (no dependencies - builds first)
2. **EntityFrameworkCore** (depends on Core)
3. **BlazorApplicationInsights** (depends on Core)
4. **EntityFrameworkCore.Tests** (depends on EntityFrameworkCore and Core)

MSBuild automatically determines build order from ProjectReference items. SDK-style projects handle this correctly without manual configuration.

## Component Boundaries

### Core Package Responsibilities

| Responsibility | What Lives Here | What Does NOT |
|---------------|-----------------|---------------|
| **Abstractions** | Interfaces (IRepository, IValidator, etc.) | Concrete implementations |
| **Base Classes** | Abstract base classes for common patterns | Framework-specific implementations |
| **Attributes** | Custom attributes for convention metadata | Attribute processors (belong in consumers) |
| **Utilities** | Pure functions with no external dependencies | Framework-specific helpers |
| **Models** | DTOs, value objects, domain models | Entity framework entities with mappings |

**Critical Rule:** Core MUST have ZERO external NuGet dependencies. Use shared source packages if small internal utilities are needed.

```xml
<!-- Core.csproj - NO PackageReference items except analyzers/tools -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <!-- No external dependencies -->
</Project>
```

### EntityFrameworkCore Package Responsibilities

| Responsibility | What Lives Here | References |
|---------------|-----------------|------------|
| **EF Implementations** | Concrete repository implementations using EF | Core abstractions |
| **DbContext Extensions** | Extension methods for DbContext configuration | Core patterns |
| **EF-Specific Patterns** | Query optimizations, change tracking patterns | EF Core packages |
| **Conventions** | EF model conventions implementing Core attributes | Core + EF Core |

```xml
<!-- EntityFrameworkCore.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- Reference Core package -->
    <ProjectReference Include="..\Core\Core.csproj" />

    <!-- EF dependencies -->
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
  </ItemGroup>
</Project>
```

### BlazorApplicationInsights Package Responsibilities

| Responsibility | What Lives Here | References |
|---------------|-----------------|------------|
| **Blazor Components** | Reusable Blazor components | Core abstractions |
| **JavaScript Interop** | JSInterop for Application Insights | Blazor packages |
| **Service Registration** | DI extensions for Blazor apps | Core + Blazor packages |
| **Telemetry** | Blazor-specific telemetry implementations | Core patterns |

```xml
<!-- BlazorApplicationInsights.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- Reference Core package -->
    <ProjectReference Include="..\Core\Core.csproj" />

    <!-- Blazor dependencies -->
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="8.0.0" />
  </ItemGroup>
</Project>
```

### EntityFrameworkCore.Tests Package Responsibilities

| Responsibility | What Lives Here | References |
|---------------|-----------------|------------|
| **Unit Tests** | Test EF implementations in isolation | EF package + test frameworks |
| **Integration Tests** | Test with real database | EF package + test database |
| **Test Utilities** | Shared test fixtures, mocks | Test frameworks only |

## Data Flow Patterns

### Pattern 1: Consumer Uses Only Core

```csharp
// Consumer's code
using YourLibrary.Core;

public class MyService
{
    private readonly IRepository<Entity> _repo;

    // Consumer provides their own implementation
    public MyService(IRepository<Entity> repo)
    {
        _repo = repo;
    }
}
```

**Package dependency:** Consumer installs only `YourLibrary.Core` NuGet package.

### Pattern 2: Consumer Uses EF Integration

```csharp
// Consumer's code
using YourLibrary.Core;
using YourLibrary.EntityFrameworkCore;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // EF package provides implementation
        services.AddDbContext<MyDbContext>();
        services.AddRepository<Entity, EfRepository<Entity>>();
    }
}
```

**Package dependencies:** Consumer installs:
- `YourLibrary.EntityFrameworkCore` (which brings Core transitively)
- EF Core packages (transitive from your package)

### Pattern 3: Consumer Uses Blazor Integration

```razor
<!-- Consumer's Blazor component -->
@using YourLibrary.Core
@using YourLibrary.BlazorApplicationInsights

<AppInsightsTracker />

@code {
    // Uses Core abstractions + Blazor implementation
}
```

**Package dependencies:** Consumer installs:
- `YourLibrary.BlazorApplicationInsights` (which brings Core transitively)
- Blazor packages (transitive from your package)

## Cross-Package Abstractions

### Rule: Interfaces in Core, Implementations in Specialized Packages

This is the **Dependency Inversion Principle** applied at package level.

```csharp
// Core package - Define abstraction
namespace YourLibrary.Core.Abstractions
{
    public interface IConventionValidator
    {
        ValidationResult Validate(object entity);
    }
}

// EntityFrameworkCore package - Implement for EF
namespace YourLibrary.EntityFrameworkCore.Validation
{
    public class EfConventionValidator : IConventionValidator
    {
        private readonly DbContext _context;

        public ValidationResult Validate(object entity)
        {
            // EF-specific validation logic
        }
    }
}

// BlazorApplicationInsights package - Implement for Blazor
namespace YourLibrary.BlazorApplicationInsights.Validation
{
    public class BlazorConventionValidator : IConventionValidator
    {
        private readonly IJSRuntime _js;

        public ValidationResult Validate(object entity)
        {
            // Blazor-specific validation logic
        }
    }
}
```

**Benefits:**
- Core defines contract
- Specialized packages provide framework-specific implementations
- Consumers can create their own implementations
- No circular dependencies

### Shared Patterns via Base Classes

When multiple specialized packages need common implementation logic:

```csharp
// Core package - Abstract base with common logic
namespace YourLibrary.Core.Patterns
{
    public abstract class BaseRepository<T> : IRepository<T>
    {
        // Common implementation
        public virtual void Validate(T entity)
        {
            // Shared validation logic
        }

        // Abstract methods for specialized implementation
        public abstract Task<T> GetByIdAsync(int id);
        public abstract Task SaveAsync(T entity);
    }
}

// EntityFrameworkCore package - Specialize for EF
namespace YourLibrary.EntityFrameworkCore.Repositories
{
    public class EfRepository<T> : BaseRepository<T>
    {
        private readonly DbContext _context;

        public override async Task<T> GetByIdAsync(int id)
        {
            return await _context.Set<T>().FindAsync(id);
        }
    }
}
```

## NuGet Package Organization

### Package Metadata Best Practices

Each package should have clear metadata in its `.csproj` file:

```xml
<PropertyGroup>
  <!-- Package Identity -->
  <PackageId>YourLibrary.Core</PackageId>
  <Version>1.0.0</Version>

  <!-- Metadata -->
  <Title>Your Library - Core</Title>
  <Description>Zero-dependency core abstractions and patterns for YourLibrary</Description>
  <Authors>Your Name</Authors>
  <PackageTags>conventions;patterns;core</PackageTags>

  <!-- Documentation -->
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <DocumentationFile>bin\$(Configuration)\$(TargetFramework)\$(AssemblyName).xml</DocumentationFile>

  <!-- NuGet Settings -->
  <PackageReadmeFile>README.md</PackageReadmeFile>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <RepositoryUrl>https://github.com/yourusername/yourlibrary</RepositoryUrl>
</PropertyGroup>

<ItemGroup>
  <None Include="..\..\README.md" Pack="true" PackagePath="\" />
</ItemGroup>
```

### Package Naming Convention

Follow Microsoft's pattern:

| Package | Naming Pattern | Example |
|---------|---------------|---------|
| **Core** | `[CompanyOrProduct].[LibraryName]` | `YourLibrary.Core` |
| **Extensions** | `[CompanyOrProduct].[LibraryName].[Technology]` | `YourLibrary.EntityFrameworkCore` |
| **Integrations** | `[CompanyOrProduct].[LibraryName].[Technology]` | `YourLibrary.BlazorApplicationInsights` |
| **Tests** | `[PackageName].Tests` | `YourLibrary.EntityFrameworkCore.Tests` |

DO NOT publish test packages to NuGet - they're for local development only.

### Dependency Version Specifications

**For inter-package references (within your library):**

```xml
<!-- EntityFrameworkCore.csproj references Core -->
<ItemGroup>
  <!-- Use ProjectReference during development -->
  <ProjectReference Include="..\Core\Core.csproj" />

  <!-- When packed, becomes PackageReference with version range -->
  <!-- SDK automatically converts to: Version="[1.0.0, 2.0.0)" -->
</ItemGroup>
```

**For external dependencies:**

```xml
<ItemGroup>
  <!-- Specify MINIMUM version only, no upper bound -->
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />

  <!-- DO NOT use exact versions: Version="[8.0.0]" ❌ -->
  <!-- DO NOT use upper bounds: Version="[8.0.0, 9.0.0)" ❌ -->
</ItemGroup>
```

**Rationale:** Minimum version ensures compatibility without constraining consumers. NuGet's "lowest applicable version" resolution prevents diamond dependency conflicts.

## Build and Versioning Strategies

### Strategy 1: Unified Versioning (Recommended for Tightly-Coupled Packages)

All packages share the same version number and release together.

**Advantages:**
- Simple to understand and support
- No version compatibility matrix
- Consumers always get tested combinations

**Implementation:**

```xml
<!-- Directory.Build.props at solution root -->
<Project>
  <PropertyGroup>
    <Version>1.2.3</Version>
    <AssemblyVersion>1.2.3</AssemblyVersion>
    <FileVersion>1.2.3</FileVersion>
  </PropertyGroup>
</Project>
```

All projects inherit the same version. Update once, applies everywhere.

**When to use:** When packages are tightly coupled and changes often affect multiple packages.

### Strategy 2: Independent Versioning

Each package has its own version and release cycle.

**Advantages:**
- Can update individual packages without releasing everything
- Clear signal about what changed

**Disadvantages:**
- Version compatibility matrix complexity
- Testing burden (all combinations)
- Consumer confusion

**Implementation:**

```xml
<!-- Core.csproj -->
<PropertyGroup>
  <Version>2.0.0</Version>
</PropertyGroup>

<!-- EntityFrameworkCore.csproj -->
<PropertyGroup>
  <Version>1.5.0</Version>
</PropertyGroup>
<ItemGroup>
  <!-- Specify compatible version range -->
  <PackageReference Include="YourLibrary.Core" Version="[2.0.0, 3.0.0)" />
</ItemGroup>
```

**When to use:** When packages are loosely coupled and have independent release cycles.

**Recommendation for your project:** Use **unified versioning**. Your packages (Core, EntityFrameworkCore, BlazorApplicationInsights) are framework integration layers that should evolve together.

### Central Package Management (CPM)

Use `Directory.Packages.props` to manage external dependency versions centrally:

```xml
<!-- Directory.Packages.props at solution root -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <!-- External dependencies - version defined once -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageVersion Include="Microsoft.AspNetCore.Components.Web" Version="8.0.0" />

    <!-- Test dependencies -->
    <PackageVersion Include="xUnit" Version="2.6.0" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
  </ItemGroup>
</Project>
```

Then project files reference without version:

```xml
<!-- EntityFrameworkCore.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Core\Core.csproj" />

  <!-- Version comes from Directory.Packages.props -->
  <PackageReference Include="Microsoft.EntityFrameworkCore" />
</ItemGroup>
```

**Benefits:**
- Single source of truth for external dependency versions
- Prevents version drift across projects
- Easier to update dependencies
- Required for .NET SDK 7.0+ / Visual Studio 2022 17.4+

### Build Automation

#### Local Development Build

```bash
# Restore all packages (respects Directory.Packages.props)
dotnet restore

# Build solution (automatic build order)
dotnet build YourLibrary.sln

# Pack all packages
dotnet pack YourLibrary.sln --configuration Release --output ./nupkg
```

MSBuild automatically:
1. Analyzes ProjectReference dependencies
2. Builds projects in correct order (Core → EntityFrameworkCore → BlazorApplicationInsights)
3. Converts ProjectReference to PackageReference in .nupkg files

#### CI/CD Pipeline (Example: GitHub Actions)

```yaml
name: Build and Pack

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore --configuration Release

    - name: Test
      run: dotnet test --no-build --configuration Release --verbosity normal

    - name: Pack
      run: dotnet pack --no-build --configuration Release --output ./nupkg

    - name: Push to NuGet (on tag)
      if: startsWith(github.ref, 'refs/tags/')
      run: dotnet nuget push ./nupkg/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json
```

**Key points:**
- `dotnet restore` reads Directory.Packages.props automatically
- `dotnet build` handles build order via ProjectReference graph
- `dotnet pack` creates individual .nupkg files for each project
- `dotnet test` runs tests in EntityFrameworkCore.Tests

### Semantic Versioning (SemVer)

Follow Semantic Versioning for all packages:

```
MAJOR.MINOR.PATCH
  │     │     │
  │     │     └─── Bug fixes, no API changes
  │     └──────── New features, backward compatible
  └────────────── Breaking changes
```

**Examples:**
- `1.0.0` → `1.0.1`: Bug fix in Core validation logic
- `1.0.1` → `1.1.0`: Add new interface to Core
- `1.1.0` → `2.0.0`: Remove deprecated method (breaking change)

**Pre-release versions:**
- `1.0.0-beta.1`: Beta release
- `1.0.0-rc.1`: Release candidate

**For unified versioning:** Bump all packages to same version even if some didn't change. Clear signal that "this is the current release set."

## Architecture Patterns to Follow

### Pattern 1: Core-Extensions Model

**What:** Core package defines abstractions; extension packages provide implementations.

**Example:**
```
Core                  → IRepository<T> interface
EntityFrameworkCore   → EfRepository<T> : IRepository<T>
```

**When to use:** Always. This is the foundation pattern.

### Pattern 2: Attribute-Based Conventions

**What:** Core defines attributes; specialized packages read and process them.

**Example:**
```csharp
// Core package
[AttributeUsage(AttributeTargets.Property)]
public class RequiredAttribute : Attribute { }

// EntityFrameworkCore package
public class ConventionModelBuilder
{
    public void ApplyConventions(ModelBuilder builder)
    {
        // Read RequiredAttribute and configure EF
    }
}
```

**When to use:** When you need declarative metadata that multiple packages can interpret differently.

### Pattern 3: Factory Pattern for Cross-Package Creation

**What:** Core defines factory interface; specialized packages implement.

**Example:**
```csharp
// Core
public interface IValidatorFactory
{
    IValidator<T> Create<T>();
}

// EntityFrameworkCore
public class EfValidatorFactory : IValidatorFactory
{
    public IValidator<T> Create<T>()
    {
        return new EfValidator<T>(_context);
    }
}
```

**When to use:** When specialized packages need to create instances of Core abstractions.

### Pattern 4: Extension Methods for Fluent APIs

**What:** Extension packages add extension methods to Core types.

**Example:**
```csharp
// Core
public class ValidationBuilder { }

// EntityFrameworkCore
public static class EfValidationExtensions
{
    public static ValidationBuilder UseEntityFramework(
        this ValidationBuilder builder,
        DbContext context)
    {
        // Configure EF-specific validation
        return builder;
    }
}

// Consumer usage
var validator = new ValidationBuilder()
    .UseEntityFramework(dbContext)  // From EF package
    .Build();
```

**When to use:** For optional configuration APIs in specialized packages.

## Anti-Patterns to Avoid

### Anti-Pattern 1: Reverse Dependencies (Core Depends on Extensions)

**What goes wrong:**
```csharp
// Core package references EntityFrameworkCore ❌
using YourLibrary.EntityFrameworkCore;

namespace YourLibrary.Core
{
    public class CoreService
    {
        // This creates reverse dependency
        private EfRepository _repo;
    }
}
```

**Why it's bad:** Breaks the abstraction model. Core now has EF dependencies, defeating the purpose of separation.

**Prevention:** Core ONLY depends on .NET base class libraries. Extensions depend on Core.

**Detection:** Run `dotnet list package --include-transitive` on Core project. If you see EF or Blazor packages, you have a problem.

### Anti-Pattern 2: Exact Version Constraints

**What goes wrong:**
```xml
<!-- EntityFrameworkCore.csproj ❌ -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="[8.0.0]" />
```

**Why it's bad:** Forces consumers to use exact version 8.0.0, preventing updates to 8.0.1 (which might fix critical bugs).

**Consequences:**
- Consumers can't update dependencies
- NuGet dependency resolution fails when two packages require different exact versions
- Security patches can't be applied

**Prevention:** Always use minimum version: `Version="8.0.0"` (no brackets).

**Detection:** Search `.csproj` files for `Version="[` patterns.

### Anti-Pattern 3: Circular Package Dependencies

**What goes wrong:**
```
Core references EntityFrameworkCore
EntityFrameworkCore references Core
```

**Why it's bad:** Impossible to build, creates deadlock in dependency resolution.

**Prevention:**
- Establish clear dependency direction (always toward Core)
- Use interfaces in Core, implementations in extensions
- Never have extension packages reference each other

**Detection:** Run `dotnet build` - MSBuild will error on circular references.

### Anti-Pattern 4: Shared Concrete Types Across Package Boundaries

**What goes wrong:**
```csharp
// EntityFrameworkCore package
public class EfValidationResult { }

// BlazorApplicationInsights package
public class BlazorService
{
    // Depends on EF package ❌
    public void Process(EfValidationResult result) { }
}
```

**Why it's bad:** Creates coupling between extension packages. BlazorApplicationInsights now depends on EntityFrameworkCore.

**Prevention:**
- Define shared types in Core as interfaces or abstract classes
- Extension packages implement/extend Core types
- Extension packages NEVER reference each other

**Correct approach:**
```csharp
// Core package
public interface IValidationResult { }

// EntityFrameworkCore package
public class EfValidationResult : IValidationResult { }

// BlazorApplicationInsights package
public class BlazorService
{
    // Depends only on Core ✓
    public void Process(IValidationResult result) { }
}
```

### Anti-Pattern 5: Publishing Test Packages to NuGet

**What goes wrong:** Including `.Tests` packages in NuGet publish.

**Why it's bad:**
- Consumers don't need test packages
- Increases noise in package search
- May expose internal test utilities

**Prevention:**
```xml
<!-- EntityFrameworkCore.Tests.csproj -->
<PropertyGroup>
  <!-- Prevent accidental pack -->
  <IsPackable>false</IsPackable>
</PropertyGroup>
```

**Detection:** Check CI/CD pipeline. If `dotnet pack` creates `*.Tests.nupkg` files, add `IsPackable>false</IsPackable>`.

### Anti-Pattern 6: Mixing Multi-Targeting with Multiple Packages Incorrectly

**What goes wrong:** Creating separate packages for different frameworks instead of multi-targeting.

**Wrong approach:**
```
YourLibrary.Core.NetStandard
YourLibrary.Core.Net8
```

**Right approach:**
```xml
<!-- Core.csproj - Single package, multiple targets -->
<PropertyGroup>
  <TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>
</PropertyGroup>
```

**Why:** NuGet automatically selects correct target framework. Multiple packages create confusion.

### Anti-Pattern 7: Vague Package Names

**What goes wrong:** Generic names like `YourLibrary.Common`, `YourLibrary.Utilities`, `YourLibrary.Helpers`

**Why it's bad:**
- Unclear responsibility boundaries
- Becomes dumping ground for random code
- Indicates poor architecture

**Prevention:** Name packages after technology integration: `EntityFrameworkCore`, `BlazorApplicationInsights`, or responsibility: `Validation`, `Serialization`.

## Migration Path from Single Package

Your current state: Single `Tests` package with conventions.

Recommended migration steps:

### Phase 1: Extract Core (Breaking Change, Major Version Bump)

1. Create `Core` project
2. Move abstractions, attributes, base classes to Core
3. Update `EntityFrameworkCore.Tests` to reference Core
4. Version: `Core 2.0.0` (breaking change)

**Build order:** Core → EntityFrameworkCore.Tests

### Phase 2: Split EF Implementation (Non-Breaking for Core)

1. Create `EntityFrameworkCore` project
2. Move EF-specific implementations from Tests to EntityFrameworkCore
3. EntityFrameworkCore references Core
4. EntityFrameworkCore.Tests references EntityFrameworkCore + Core
5. Version: `Core 2.0.0`, `EntityFrameworkCore 1.0.0`

**Build order:** Core → EntityFrameworkCore → EntityFrameworkCore.Tests

### Phase 3: Add Blazor Package (Non-Breaking)

1. Create `BlazorApplicationInsights` project
2. Implement Blazor-specific features using Core abstractions
3. BlazorApplicationInsights references Core
4. Version: `Core 2.0.0`, `EntityFrameworkCore 1.0.0`, `BlazorApplicationInsights 1.0.0`

**Build order:** Core → (EntityFrameworkCore || BlazorApplicationInsights) → EntityFrameworkCore.Tests

Note: EntityFrameworkCore and BlazorApplicationInsights can build in parallel (no dependency).

### Migration Script Example

```bash
# Create new project structure
dotnet new classlib -n YourLibrary.Core -o src/Core
dotnet new classlib -n YourLibrary.EntityFrameworkCore -o src/EntityFrameworkCore
dotnet new classlib -n YourLibrary.BlazorApplicationInsights -o src/BlazorApplicationInsights

# Add project references
dotnet add src/EntityFrameworkCore/YourLibrary.EntityFrameworkCore.csproj reference src/Core/YourLibrary.Core.csproj
dotnet add src/BlazorApplicationInsights/YourLibrary.BlazorApplicationInsights.csproj reference src/Core/YourLibrary.Core.csproj
dotnet add src/EntityFrameworkCore.Tests/YourLibrary.EntityFrameworkCore.Tests.csproj reference src/EntityFrameworkCore/YourLibrary.EntityFrameworkCore.csproj

# Add to solution
dotnet sln add src/Core/YourLibrary.Core.csproj
dotnet sln add src/EntityFrameworkCore/YourLibrary.EntityFrameworkCore.csproj
dotnet sln add src/BlazorApplicationInsights/YourLibrary.BlazorApplicationInsights.csproj
```

## Build Order Implications

### Automatic vs. Manual Build Order

**Automatic (Recommended):**

MSBuild analyzes ProjectReference items and builds in correct order automatically.

```bash
dotnet build YourLibrary.sln
```

MSBuild determines:
1. Core has no dependencies → build first
2. EntityFrameworkCore depends on Core → build after Core
3. BlazorApplicationInsights depends on Core → build after Core
4. EntityFrameworkCore.Tests depends on EntityFrameworkCore → build last

**Manual (Not Needed):**

You can specify explicit build order in solution file, but with ProjectReference, this is unnecessary and error-prone.

### Parallel Builds

EntityFrameworkCore and BlazorApplicationInsights can build in parallel because they don't depend on each other:

```bash
# MSBuild automatically parallelizes independent projects
dotnet build -m  # -m enables parallel build (default in SDK projects)
```

Build timeline:
```
T0: Core starts
T1: Core completes
T2: EntityFrameworkCore starts | BlazorApplicationInsights starts
T3: EntityFrameworkCore completes | BlazorApplicationInsights completes
T4: EntityFrameworkCore.Tests starts
T5: EntityFrameworkCore.Tests completes
```

### CI/CD Optimization

Use build graph to visualize and optimize:

```bash
# Generate build graph
dotnet build -graph

# Or use MSBuild directly
msbuild YourLibrary.sln /graph
```

**Cache optimization:** In CI/CD, cache build outputs per project. Unchanged projects skip rebuild.

```yaml
# GitHub Actions example
- name: Cache build outputs
  uses: actions/cache@v3
  with:
    path: |
      **/bin
      **/obj
    key: ${{ runner.os }}-build-${{ hashFiles('**/*.csproj') }}
```

## Validation Checklist

Before finalizing multi-package architecture:

- [ ] Core package has ZERO external NuGet dependencies
- [ ] All extension packages reference Core via ProjectReference
- [ ] No circular dependencies between packages
- [ ] All packages have `GenerateDocumentationFile>true</GenerateDocumentationFile>`
- [ ] Directory.Packages.props manages external dependency versions
- [ ] All packages use SemVer (MAJOR.MINOR.PATCH)
- [ ] Test packages have `IsPackable>false</IsPackable>`
- [ ] Package names follow convention: `[Product].[Technology]`
- [ ] Each package has clear README.md describing purpose
- [ ] All PackageReference items specify minimum version only (no upper bound)
- [ ] `dotnet pack` creates separate .nupkg for each package
- [ ] Inter-package references convert to PackageReference in .nupkg files
- [ ] Build succeeds with `dotnet build` (automatic build order)

## Confidence Assessment

| Area | Confidence | Source |
|------|-----------|--------|
| Package structure patterns | **HIGH** | Microsoft.Extensions.* official architecture, Microsoft Learn documentation |
| Dependency management | **HIGH** | NuGet official docs (Central Package Management, dependency resolution) |
| Build order automation | **HIGH** | MSBuild official docs, verified limitations with solution dependencies |
| Versioning strategies | **HIGH** | SemVer best practices, NuGet versioning guidelines |
| Anti-patterns | **MEDIUM** | Community articles (2025), personal experience patterns, some verified with Microsoft docs |
| Migration approach | **MEDIUM** | Inferred from patterns, not explicitly documented for this exact scenario |

## Sources

### Official Microsoft Documentation (HIGH confidence)
- [Dependencies and .NET libraries - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/dependencies)
- [Multi-targeting for NuGet Packages - Microsoft Learn](https://learn.microsoft.com/en-us/nuget/create-packages/supporting-multiple-target-frameworks)
- [Central Package Management - Microsoft Learn](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
- [NuGet Package Dependency Resolution - Microsoft Learn](https://learn.microsoft.com/en-us/nuget/concepts/dependency-resolution)
- [How MSBuild builds projects - Microsoft Learn](https://learn.microsoft.com/en-us/visualstudio/msbuild/build-process-overview)
- [Architectural principles - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles)

### NuGet Package Management (HIGH confidence)
- [Implementing NuGet Central Package Management (CPM) in a .NET Solution using Visual Studio 2026 - Medium](https://sachidevop.medium.com/implementing-nuget-central-package-management-cpm-in-a-net-solution-using-visual-studio-2026-c93f207edcb6)
- [Central Package Management in .NET - Milan Jovanovic](https://www.milanjovanovic.tech/blog/central-package-management-in-net-simplify-nuget-dependencies)
- [NuGet Best Practices and Versioning for .NET Developers - Medium](https://medium.com/@sweetondonie/nuget-best-practices-and-versioning-for-net-developers-cedc8ede5f16)

### Multi-Package Architecture Examples (HIGH confidence)
- [Introducing Microsoft.Extensions.AI Preview - .NET Blog](https://devblogs.microsoft.com/dotnet/introducing-microsoft-extensions-ai-preview/)
- [Microsoft.Extensions.AI libraries - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai)
- [Producing Multiple Related NuGet Packages from a Single Git Repo - Mark Heath](https://markheath.net/post/multiple-nuget-single-repo)

### Build and Versioning (HIGH confidence)
- [Best Practices for Managing Shared Libraries in .NET Applications at Scale - InfoQ](https://www.infoq.com/articles/shared-libraries-dotnet-enterprise/)
- [Best Practices for Versioning NuGet Packages in the Enterprise - Inedo](https://blog.inedo.com/nuget/package-versioning)

### Anti-Patterns and Common Mistakes (MEDIUM confidence)
- [Circular Dependency in .NET 7 - Jon D Jones](https://www.jondjones.com/programming/aspnet-core/how-to/circular-dependency-hell-in-net-7/)
- [How to resolve .NET reference and NuGet package version conflicts - Michael's Coding Spot](https://michaelscodingspot.com/how-to-resolve-net-reference-and-nuget-package-version-conflicts/)
- [Common .NET Core Anti-Patterns and How to Avoid Them - Medium](https://medium.com/@robhutton8/common-net-core-anti-patterns-and-how-to-avoid-them-533b9812b6d5)
- [The shared code fallacy: Why internal libraries can be an anti-pattern](https://www.ben-morris.com/the-shared-code-fallacy-why-internal-libraries-are-an-anti-pattern/)

### MSBuild Dependencies (MEDIUM-HIGH confidence)
- [MSBuild does not honor solution Project Dependencies - GitHub Issue](https://github.com/dotnet/msbuild/issues/8099)
- [Incorrect solution build ordering when using MSBuild.exe - Visual Studio Blog](https://devblogs.microsoft.com/visualstudio/incorrect-solution-build-ordering-when-using-msbuild-exe/)

---

**Last Updated:** 2026-01-25
