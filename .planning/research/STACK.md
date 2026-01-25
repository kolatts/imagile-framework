# Technology Stack

**Project:** Multi-Package .NET Framework Library (Imagile.Framework)
**Researched:** 2026-01-25
**Overall Confidence:** HIGH

## Executive Summary

This stack recommendation is based on .NET 10 LTS (released November 2025, supported until November 2028) for building multi-package framework libraries with NuGet distribution. The stack emphasizes zero-dependency architecture for Core packages, automated versioning with GitVersion, and proven patterns for audit logging and telemetry.

**Key decisions:**
- .NET 10 LTS as target framework (3-year support lifecycle)
- GitVersion.MsBuild 6.5.1 for automated semantic versioning
- Custom EF Core interceptors for audit logging (avoid heavy dependencies)
- BlazorApplicationInsights 3.3.0 for Blazor telemetry
- Central Package Management for multi-package version coordination

---

## Recommended Stack

### Core Framework

| Technology | Version | Purpose | Why | Confidence |
|------------|---------|---------|-----|-----------|
| .NET SDK | 10.0.100+ | Target framework | LTS release (Nov 2025-Nov 2028), 3-year support, native AOT improvements, enhanced MSBuild task interop | HIGH |
| Target Framework Moniker | net10.0 | Project targeting | Standard TFM for .NET 10, supports latest C# 14 features | HIGH |
| C# Language | 14 | Language version | Ships with .NET 10 SDK, includes latest language features | HIGH |

**Rationale:** .NET 10 was released November 11, 2025 as an LTS release. The 3-year support window (until November 2028) makes it the optimal choice for library development requiring stability. Using net10.0 as the target ensures maximum API surface area while maintaining compatibility with consuming applications.

**Source:** [Microsoft Learn - What's new in .NET 10 SDK](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/sdk), [.NET 10 Announcement](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/)

---

### Build & Packaging Infrastructure

| Technology | Version | Purpose | Why | Confidence |
|------------|---------|---------|-----|-----------|
| GitVersion.MsBuild | 6.5.1 | Automated semantic versioning | MSBuild integration, Git-based version calculation, generates AssemblyInfo and package versions automatically | HIGH |
| Microsoft.SourceLink.GitHub | 8.0.0 | Source debugging support | Embeds Git commit metadata in symbols, enables step-through debugging of NuGet packages | HIGH |
| Directory.Packages.props | N/A (MSBuild feature) | Centralized dependency management | Single source of truth for package versions across all projects in solution | HIGH |
| Directory.Build.props | N/A (MSBuild feature) | Shared project settings | Centralize common MSBuild properties (versioning, symbols, SourceLink config) | HIGH |

**Rationale for GitVersion over alternatives:**
- **GitVersion** (recommended): Feature-rich, supports branching strategies (GitFlow, GitHubFlow), infers versions from branch names and commit messages, integrates with CI/CD pipelines. Latest version 6.5.1 (Nov 2025).
- **MinVer** (not recommended for this project): Simpler but too minimal—only reads Git tags, no branch-based versioning, no YAML configuration. Better for simple projects but insufficient for multi-package frameworks.
- **Nerdbank.GitVersioning** (alternative): Official dotnet org tool, uses version.json file and Git height. Creates gaps in patch versions if not all commits are released. Config-file approach is more rigid than GitVersion's flexible branching strategies.

**Decision:** GitVersion.MsBuild provides the best balance of automation and flexibility for multi-package library development where different branches (develop, release, hotfix) need different versioning strategies.

**Sources:**
- [GitVersion.MsBuild on NuGet](https://www.nuget.org/packages/GitVersion.MsBuild/)
- [MinVer comparison discussion](https://github.com/adamralph/minver/issues/19)
- [Nerdbank.GitVersioning comparison](https://news.ycombinator.com/item?id=42261586)
- [Microsoft SourceLink documentation](https://learn.microsoft.com/en-us/visualstudio/debugger/how-to-improve-diagnostics-debugging-with-sourcelink)

---

### Entity Framework Core - Audit Logging

| Technology | Version | Purpose | Why | Confidence |
|------------|---------|---------|-----|-----------|
| Microsoft.EntityFrameworkCore | 10.0.0+ | Database access | Framework dependency for Imagile.Framework.EntityFrameworkCore package | HIGH |
| Custom SaveChangesInterceptor | N/A (pattern) | Audit logging implementation | Lightweight, zero additional dependencies, full control over audit behavior | MEDIUM-HIGH |

**Alternative approaches considered:**

| Library | Latest Version | Pros | Cons | Recommendation |
|---------|---------------|------|------|----------------|
| **Custom Interceptor** | N/A | Zero dependencies, full control, lightweight, leverages EF Core 5.0+ native interceptor pipeline | Requires implementation effort | **RECOMMENDED** |
| Audit.EntityFramework.Core | 32.0.0 (Jan 2026) | Mature (MIT), extensive storage providers, rich features, handles complex scenarios | Adds dependency (Audit.NET + Audit.EntityFramework.Abstractions), may be overkill for framework library | Consider if complex audit requirements emerge |
| Entity Framework Plus | Commercial | Comprehensive auditing, well-maintained | Commercial license required, vendor dependency | Not recommended for open framework |

**Rationale:** For a framework library, implementing a custom `ISaveChangesInterceptor` is preferred because:
1. **Zero dependencies** align with the Core package philosophy
2. **Full control** over audit behavior and data structure
3. **EF Core 5.0+ native support** makes interceptors a first-class pattern (introduced in EF Core 5.0, matured through 6-10)
4. **Lightweight** compared to external audit frameworks
5. Consuming applications can **extend or replace** the interceptor if needed

**Implementation pattern:** Use `SaveChangesInterceptor` base class, implement `SavingChangesAsync` to capture entity state before save, and `SavedChangesAsync` to finalize audit records after successful persistence.

**Confidence:** MEDIUM-HIGH because while the pattern is well-established, it requires careful implementation. If audit requirements become complex (e.g., need for separate audit database, multiple storage backends), consider Audit.EntityFramework.Core.

**Sources:**
- [Audit.EntityFramework.Core on NuGet](https://www.nuget.org/packages/Audit.EntityFramework.Core)
- [EF Core Interceptors - Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors)
- [Comprehensive Guide to Implementing Audit Logging in .NET with EF Core Interceptors](https://dev.to/hootanht/comprehensive-guide-to-implementing-audit-logging-in-net-with-ef-core-interceptors-1e83)
- [SaveChanges Interception for EF Core Auditing](https://www.woodruff.dev/tracking-every-change-using-savechanges-interception-for-ef-core-auditing/)

---

### Blazor - Application Insights Telemetry

| Technology | Version | Purpose | Why | Confidence |
|------------|---------|---------|-----|-----------|
| BlazorApplicationInsights | 3.3.0 | Blazor telemetry integration | Supports .NET 8/9/10, automatic page view tracking, ILoggerProvider integration (WebAssembly), programmatic configuration | HIGH |
| Microsoft.Extensions.Logging.Abstractions | 10.0.0+ | Logging abstractions | Framework dependency for Core package logging interfaces | HIGH |

**Rationale:** BlazorApplicationInsights 3.3.0 (Oct 2025) is the standard solution for Application Insights integration in Blazor applications. It provides:
- **Automatic page view tracking** on route changes
- **ILoggerProvider** integration that sends logs to Application Insights (WebAssembly only)
- **Programmatic configuration** including dynamic Connection String updates
- **.NET 8/9/10 support** with computed compatibility for .NET 10
- **MIT license** with active maintenance (206 GitHub stars, recent updates)

**Dependencies (for .NET 10):**
- Microsoft.AspNetCore.Components (≥ 9.0.10, computed compatible with 10.0)
- Microsoft.AspNetCore.Components.Web (≥ 9.0.10, computed compatible with 10.0)
- Microsoft.Extensions.Logging (≥ 9.0.10, computed compatible with 10.0)

**Alternative:** OpenTelemetry provides vendor-agnostic telemetry collection, but adds complexity and multiple package dependencies. For Azure-focused teams using Application Insights, BlazorApplicationInsights is simpler and more direct.

**Sources:**
- [BlazorApplicationInsights on NuGet](https://www.nuget.org/packages/BlazorApplicationInsights)
- [BlazorApplicationInsights GitHub Repository](https://github.com/IvanJosipovic/BlazorApplicationInsights)

---

### Declarative Attributes Framework

| Technology | Version | Purpose | Why | Confidence |
|------------|---------|---------|-----|-----------|
| System.ComponentModel.DataAnnotations | Built-in (.NET 10) | Validation attributes | Zero dependencies, framework built-in, widely understood, supports custom ValidationAttribute subclasses | HIGH |
| System.ComponentModel.DataAnnotations.Schema | Built-in (.NET 10) | EF Core mapping attributes | Framework built-in, standard for EF Core conventions | HIGH |

**Rationale:** For declarative attributes in a zero-dependency Core package:
- **System.ComponentModel.DataAnnotations** is part of the .NET BCL (Base Class Library), requiring no additional NuGet dependencies
- Provides standard attributes: `[Required]`, `[StringLength]`, `[EmailAddress]`, `[Range]`, etc.
- `ValidationAttribute` base class enables custom validation attributes
- Widely understood by .NET developers
- Supported in .NET 10 with full API surface

**Custom Attribute Pattern:**
```csharp
[AttributeUsage(AttributeTargets.Property)]
public class CustomValidationAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext context)
    {
        // Custom validation logic
    }
}
```

**Alternatives considered:**
- **FluentValidation**: Excellent library but adds external dependency, not suitable for zero-dependency Core package
- **Custom attribute system**: Reinventing the wheel when DataAnnotations is already in BCL

**Decision:** Use System.ComponentModel.DataAnnotations for Core package validation attributes. Consuming applications can layer FluentValidation or other frameworks on top if desired.

**Sources:**
- [System.ComponentModel.DataAnnotations - Microsoft Learn (.NET 10 view)](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations?view=net-10.0)
- [ValidationAttribute Class](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.validationattribute?view=net-10.0)
- [The Rich Set of Data Annotation and Validation Attributes in .NET](https://www.codemag.com/Article/2301031/The-Rich-Set-of-Data-Annotation-and-Validation-Attributes-in-.NET)

---

### Source Generators (Optional Enhancement)

| Technology | Version | Purpose | Why | Confidence |
|------------|---------|---------|-----|-----------|
| Microsoft.CodeAnalysis.CSharp | 4.13.0+ | Source generator SDK | Enables compile-time code generation for reducing boilerplate | MEDIUM |

**Use case:** If attribute-heavy patterns emerge (e.g., audit configuration, convention attributes), source generators can generate boilerplate implementation code at compile time.

**Pattern:** Incremental source generators implement `IIncrementalGenerator`, decorated with `[Generator]` attribute. They process syntax trees and generate additional C# files during compilation.

**Confidence:** MEDIUM because source generators add build complexity and require careful testing. Only adopt if clear ROI exists (significant boilerplate reduction).

**Sources:**
- [Source Generators Cookbook - Roslyn](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md)
- [Incremental Source Generators in .NET](https://posts.specterops.io/dotnet-source-generators-in-2024-part-1-getting-started-76d619b633f5)

---

## Multi-Package Configuration

### Project Structure

```
solution-root/
├── Directory.Build.props           # Shared MSBuild properties
├── Directory.Packages.props        # Central package version management
├── src/
│   ├── Imagile.Framework.Core/
│   ├── Imagile.Framework.EntityFrameworkCore/
│   └── Imagile.Framework.BlazorApplicationInsights/
└── tests/
    └── [test projects]
```

### Directory.Build.props Configuration

**Purpose:** Centralize common MSBuild properties across all projects.

**Key settings:**
```xml
<Project>
  <PropertyGroup>
    <!-- Versioning (GitVersion will override these) -->
    <VersionPrefix>1.0.0</VersionPrefix>

    <!-- Package metadata -->
    <Authors>Your Team</Authors>
    <Company>Your Company</Company>
    <Copyright>Copyright © Your Company 2026</Copyright>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/yourorg/yourrepo</PackageProjectUrl>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <RepositoryUrl>https://github.com/yourorg/yourrepo</RepositoryUrl>
    <RepositoryType>git</RepositoryType>

    <!-- Symbol packages -->
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>

    <!-- SourceLink -->
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>

    <!-- Nullable reference types -->
    <Nullable>enable</Nullable>

    <!-- Deterministic builds -->
    <Deterministic>true</Deterministic>
    <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
  </PropertyGroup>
</Project>
```

**Sources:**
- [Versioning .NET projects with Directory.Build.props](https://training.majorguidancesolutions.com/blog/versioning-net-projects-with-directory-build-props)
- [NuGet Package Authoring Best Practices](https://learn.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices)

### Directory.Packages.props Configuration

**Purpose:** Central Package Management (CPM) for consistent dependency versions across all projects.

**Key settings:**
```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <!-- Build-time packages (all projects) -->
    <GlobalPackageReference Include="GitVersion.MsBuild" Version="6.5.1">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </GlobalPackageReference>

    <GlobalPackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </GlobalPackageReference>

    <!-- EF Core packages -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.0" />

    <!-- Blazor packages -->
    <PackageVersion Include="BlazorApplicationInsights" Version="3.3.0" />
    <PackageVersion Include="Microsoft.AspNetCore.Components" Version="10.0.0" />
    <PackageVersion Include="Microsoft.AspNetCore.Components.Web" Version="10.0.0" />

    <!-- Logging -->
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />

    <!-- Testing -->
    <PackageVersion Include="xUnit" Version="2.9.0" />
    <PackageVersion Include="Moq" Version="4.20.0" />
  </ItemGroup>
</Project>
```

**Benefits:**
- Single source of truth for all dependency versions
- Eliminate version conflicts across multi-package solution
- Project files declare only `<PackageReference Include="PackageName" />` without Version attribute
- Easy to audit and update dependencies

**Sources:**
- [Central Package Management - Microsoft Learn](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
- [Directory.Packages.props - Tim Deschryver](https://timdeschryver.dev/blog/directorypackagesprops-a-solution-to-unify-your-nuget-package-versions)

---

## Installation & Setup

### Prerequisites

```bash
# Verify .NET 10 SDK installation
dotnet --version
# Should output: 10.0.100 or higher

# Verify GitVersion installation (optional, MSBuild package handles this)
dotnet tool list -g
```

### New Project Setup

```bash
# Create solution
dotnet new sln -n Imagile.Framework

# Create projects
dotnet new classlib -n Imagile.Framework.Core -f net10.0
dotnet new classlib -n Imagile.Framework.EntityFrameworkCore -f net10.0
dotnet new classlib -n Imagile.Framework.BlazorApplicationInsights -f net10.0

# Add projects to solution
dotnet sln add src/Imagile.Framework.Core
dotnet sln add src/Imagile.Framework.EntityFrameworkCore
dotnet sln add src/Imagile.Framework.BlazorApplicationInsights

# Create configuration files
# (Create Directory.Build.props and Directory.Packages.props at solution root)
```

### Project References

```bash
# EntityFrameworkCore package depends on Core
cd src/Imagile.Framework.EntityFrameworkCore
dotnet add reference ../Imagile.Framework.Core

# BlazorApplicationInsights package depends on Core
cd ../Imagile.Framework.BlazorApplicationInsights
dotnet add reference ../Imagile.Framework.Core
```

### Package Dependencies (in .csproj files)

**Imagile.Framework.Core:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <PackageId>Imagile.Framework.Core</PackageId>
    <Description>Zero-dependency base package for Imagile Framework</Description>
  </PropertyGroup>

  <ItemGroup>
    <!-- No dependencies - zero-dependency package -->
  </ItemGroup>
</Project>
```

**Imagile.Framework.EntityFrameworkCore:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <PackageId>Imagile.Framework.EntityFrameworkCore</PackageId>
    <Description>Entity Framework Core audit logging and conventions</Description>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Imagile.Framework.Core\Imagile.Framework.Core.csproj" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Relational" />
  </ItemGroup>
</Project>
```

**Imagile.Framework.BlazorApplicationInsights:**
```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <PackageId>Imagile.Framework.BlazorApplicationInsights</PackageId>
    <Description>Application Insights telemetry for Blazor applications</Description>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Imagile.Framework.Core\Imagile.Framework.Core.csproj" />
    <PackageReference Include="BlazorApplicationInsights" />
    <PackageReference Include="Microsoft.AspNetCore.Components" />
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
  </ItemGroup>
</Project>
```

---

## Building & Packing

### Local Development Build

```bash
# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Pack NuGet packages (GitVersion will calculate versions)
dotnet pack --configuration Release --output ./artifacts/packages
```

### CI/CD Integration

GitVersion integrates with all major CI/CD platforms:

**GitHub Actions:**
```yaml
- name: Install GitVersion
  uses: gittools/actions/gitversion/setup@v0.10.2
  with:
    versionSpec: '6.x'

- name: Determine Version
  uses: gittools/actions/gitversion/execute@v0.10.2

- name: Build and Pack
  run: dotnet pack --configuration Release
```

**Azure DevOps:**
```yaml
- task: UseGitVersion@5
  inputs:
    versionSpec: '6.x'

- task: DotNetCoreCLI@2
  inputs:
    command: 'pack'
    configuration: 'Release'
```

**Source:** [GitVersion Documentation](https://gitversion.net/docs/)

---

## Alternatives Considered

### Versioning Tools

| Tool | Version | Pros | Cons | Decision |
|------|---------|------|------|----------|
| **GitVersion.MsBuild** | 6.5.1 | Flexible branching strategies, MSBuild integration, rich feature set, CI/CD support | More complex configuration | **SELECTED** |
| MinVer | 7.0.0 | Simplest setup, tag-based only | No branch-based versioning, no commit message parsing | Too minimal |
| Nerdbank.GitVersioning | 3.6.x | Official dotnet tool, deterministic | Git height creates version gaps, rigid config file | Less flexible |

### Audit Logging

| Approach | Pros | Cons | Decision |
|----------|------|------|----------|
| **Custom Interceptor** | Zero dependencies, full control, lightweight | Implementation effort | **SELECTED** |
| Audit.EntityFramework.Core | Feature-rich, mature, multiple storage backends | Adds dependencies (not zero-dependency) | Fallback option |
| Entity Framework Plus | Comprehensive | Commercial license | Not suitable |

### Telemetry

| Tool | Pros | Cons | Decision |
|------|------|------|----------|
| **BlazorApplicationInsights** | Blazor-specific, simple setup, MIT license | Azure-centric | **SELECTED** for Azure teams |
| OpenTelemetry | Vendor-agnostic, CNCF standard | More complex, multiple packages | Alternative for multi-cloud |

---

## Version Summary

| Category | Technology | Version | Status |
|----------|-----------|---------|--------|
| **Framework** | .NET SDK | 10.0.100+ | GA (Nov 2025) |
| **Target** | net10.0 | N/A | Current LTS |
| **Versioning** | GitVersion.MsBuild | 6.5.1 | Stable (Nov 2025) |
| **SourceLink** | Microsoft.SourceLink.GitHub | 8.0.0 | Stable |
| **EF Core** | Microsoft.EntityFrameworkCore | 10.0.0+ | GA (Nov 2025) |
| **Audit** | Custom ISaveChangesInterceptor | N/A | EF Core 5.0+ pattern |
| **Telemetry** | BlazorApplicationInsights | 3.3.0 | Stable (Oct 2025) |
| **Attributes** | System.ComponentModel.DataAnnotations | Built-in | .NET 10 BCL |

---

## Confidence Assessment

| Area | Confidence | Rationale |
|------|-----------|-----------|
| .NET 10 LTS | HIGH | GA release November 2025, official Microsoft documentation, 3-year support |
| GitVersion | HIGH | Latest stable version 6.5.1, widely adopted, active maintenance |
| SourceLink | HIGH | Official Microsoft tool, mature (version 8.0.0), standard practice |
| Central Package Management | HIGH | Native MSBuild feature, documented best practice |
| Custom EF Core Interceptor | MEDIUM-HIGH | Well-documented pattern, requires implementation, fallback option available |
| BlazorApplicationInsights | HIGH | Latest version 3.3.0, .NET 10 compatible, active maintenance |
| DataAnnotations | HIGH | Built-in .NET BCL, zero dependencies, well-established |
| Multi-package approach | HIGH | Standard .NET practice, documented patterns, tooling support |

---

## Open Questions & Risks

### Low Risk

- **GitVersion configuration:** Need to define branching strategy (GitFlow vs GitHub Flow) - well-documented, low complexity
- **Symbol package hosting:** Need NuGet.org symbol server account for .snupkg files - standard process

### Medium Risk

- **Custom interceptor implementation:** Requires careful testing of audit logging logic - mitigated by comprehensive unit tests and fallback to Audit.EntityFramework.Core if complexity grows
- **BlazorApplicationInsights .NET 10 compatibility:** Package shows "computed compatibility" for .NET 10 - mitigated by testing, likely no issues given .NET 9 support

### Addressed

- **.NET 10 support window:** Confirmed LTS with 3-year support (Nov 2025 - Nov 2028)
- **Package versioning coordination:** Addressed via Central Package Management (Directory.Packages.props)

---

## Next Steps for Roadmap

Based on this stack research, the roadmap should structure phases as follows:

1. **Phase 1: Core Package Foundation**
   - Target: net10.0 with zero dependencies
   - Setup: Directory.Build.props, Directory.Packages.props, GitVersion configuration
   - Implement: Base attribute framework using System.ComponentModel.DataAnnotations

2. **Phase 2: EF Core Package - Audit Logging**
   - Implement: Custom SaveChangesInterceptor for audit logging
   - Depend on: Imagile.Framework.Core + EF Core 10.0 packages
   - Test: Unit tests for interceptor behavior, integration tests with EF Core

3. **Phase 3: Blazor Package - Telemetry**
   - Integrate: BlazorApplicationInsights 3.3.0
   - Depend on: Imagile.Framework.Core + Blazor packages
   - Test: Blazor WebAssembly and Server scenarios

4. **Phase 4: NuGet Publishing**
   - Configure: Symbol packages (.snupkg), SourceLink
   - Automate: CI/CD with GitVersion
   - Publish: NuGet.org with comprehensive README

**Research flags:** Phase 2 (custom interceptor implementation) may benefit from deeper technical research if audit requirements prove complex.

---

## Sources

### Official Microsoft Documentation
- [What's new in .NET 10 SDK](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/sdk)
- [Announcing .NET 10](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/)
- [Target frameworks in SDK-style projects](https://learn.microsoft.com/en-us/dotnet/standard/frameworks)
- [NuGet Package Authoring Best Practices](https://learn.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices)
- [Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
- [EF Core Interceptors](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors)
- [System.ComponentModel.DataAnnotations (.NET 10)](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations?view=net-10.0)
- [Source Link Documentation](https://learn.microsoft.com/en-us/visualstudio/debugger/how-to-improve-diagnostics-debugging-with-sourcelink)

### NuGet Packages
- [GitVersion.MsBuild 6.5.1](https://www.nuget.org/packages/GitVersion.MsBuild/)
- [Audit.EntityFramework.Core 32.0.0](https://www.nuget.org/packages/Audit.EntityFramework.Core)
- [BlazorApplicationInsights 3.3.0](https://www.nuget.org/packages/BlazorApplicationInsights)
- [MinVer 7.0.0](https://www.nuget.org/packages/MinVer/)

### Community Resources
- [Comprehensive Guide to Implementing Audit Logging in .NET with EF Core Interceptors](https://dev.to/hootanht/comprehensive-guide-to-implementing-audit-logging-in-net-with-ef-core-interceptors-1e83)
- [SaveChanges Interception for EF Core Auditing](https://www.woodruff.dev/tracking-every-change-using-savechanges-interception-for-ef-core-auditing/)
- [Versioning .NET projects with Directory.Build.props](https://training.majorguidancesolutions.com/blog/versioning-net-projects-with-directory-build-props)
- [Directory.Packages.props - Tim Deschryver](https://timdeschryver.dev/blog/directorypackagesprops-a-solution-to-unify-your-nuget-package-versions)
- [MinVer vs GitVersion comparison](https://github.com/adamralph/minver/issues/19)
- [Nerdbank.GitVersioning discussion](https://news.ycombinator.com/item?id=42261586)
- [BlazorApplicationInsights GitHub](https://github.com/IvanJosipovic/BlazorApplicationInsights)
- [Audit.NET GitHub](https://github.com/thepirat000/Audit.NET)
- [Source Generators Cookbook](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md)

---

**Research Complete:** 2026-01-25
**Confidence Level:** HIGH (verified with official Microsoft documentation and current NuGet package versions)
**Recommended for:** Multi-package .NET framework library development targeting .NET 10 LTS with NuGet distribution
