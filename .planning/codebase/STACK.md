# Technology Stack

**Analysis Date:** 2026-01-25

## Languages

**Primary:**
- C# 10+ - Core library implementation, convention rules, and configuration
- YAML - GitHub Actions workflows and GitVersion configuration

## Runtime

**Environment:**
- .NET 10.0 (LTS) - Primary target framework with multi-version CI support
- .NET 9.0.x - Also tested in CI/CD pipeline

**Package Manager:**
- NuGet - Standard .NET package management
- Lockfile: Present (implicit in .csproj PackageReference elements)

## Frameworks

**Core:**
- Entity Framework Core 10.0.0 - ORM framework for database context conventions and validation
- Microsoft.Data.Sqlite 10.0.0 - SQLite ADO.NET provider

**Testing:**
- xUnit 2.9.3 - Unit testing framework used across main library and sample tests
- FluentAssertions 6.12.0 - Fluent assertion library for readable test assertions
- Microsoft.NET.Test.Sdk 17.12.0 - .NET testing infrastructure (sample tests only)
- xunit.runner.visualstudio 3.0.0 - Visual Studio test adapter for xUnit

**Build/Dev:**
- GitVersion 5.x - Semantic versioning automation for NuGet package versioning
- GitHub Actions - CI/CD and automated NuGet publishing

## Key Dependencies

**Critical:**
- Microsoft.EntityFrameworkCore 10.0.0 - Provides DbContext APIs and model metadata inspection
- Humanizer.Core 3.0.1 - String pluralization and case conversion utilities for naming convention validation

**Infrastructure:**
- Microsoft.EntityFrameworkCore.Sqlite 10.0.0 - SQLite provider for in-memory testing
- FluentAssertions 6.12.0 - Test assertion framework for rule validation

## Configuration

**Environment:**
- Target Framework: `.NET 10.0` (set in `.csproj` files)
- Implicit Usings: Enabled (reduces namespace declarations)
- Nullable Reference Types: Enabled globally

**Build:**
- Solution file: `Imagile.EntityFrameworkCore.sln`
- Main project: `Imagile.EntityFrameworkCore.Tests/Imagile.EntityFrameworkCore.Tests.csproj`
- Sample projects: `Samples/SampleApp.Data/SampleApp.Data.csproj` and `Samples/SampleApp.Tests/SampleApp.Tests.csproj`
- Version control: `GitVersion.yml` (mainline mode with semantic versioning)

## Platform Requirements

**Development:**
- .NET SDK 10.0.x or 9.0.x minimum
- Visual Studio 2022 or compatible IDE supporting C# 10+
- Git for version control

**Production:**
- NuGet package distribution via `api.nuget.org`
- Deployment: Package published as `Imagile.EntityFrameworkCore.Tests` NuGet package
- Minimum .NET 10.0 required for consumers

---

*Stack analysis: 2026-01-25*
