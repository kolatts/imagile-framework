---
phase: 05-configuration-abstractions
plan: 02
subsystem: configuration
tags: [azure, keyvault, configuration, tdd, framework]
requires: [05-01]
provides:
  - Key Vault reference replacement in IConfiguration
  - Recursive configuration traversal
  - @KeyVault(SecretName) syntax support
affects: []
decisions:
  - Use @KeyVault(SecretName) syntax matching Azure App Service for developer familiarity
  - Fail-fast on missing secrets (no silent failures)
  - Recursive traversal to handle nested configuration sections
tech-stack:
  added: []
  patterns:
    - Extension method pattern for IConfiguration
    - TDD with RED-GREEN-REFACTOR cycle
    - NSubstitute for mocking SecretClient
key-files:
  created:
    - src/Imagile.Framework.Configuration/Extensions/KeyVaultConfigurationExtensions.cs
    - tests/Imagile.Framework.Configuration.Tests/Extensions/KeyVaultConfigurationExtensionsTests.cs
  modified: []
metrics:
  duration: 242s
  completed: 2026-01-26
---

# Phase 5 Plan 02: Key Vault Reference Replacement Summary

**One-liner:** Extension method to recursively replace @KeyVault(SecretName) configuration values with actual Azure Key Vault secrets using fail-fast error handling

## What Was Built

Implemented `ReplaceKeyVaultReferences` extension method for `IConfiguration` that:

1. **Syntax Support:** Recognizes `@KeyVault(SecretName)` format matching Azure App Service conventions
2. **Recursive Traversal:** Processes all configuration sections including deeply nested values
3. **Selective Replacement:** Only replaces values matching exact syntax, leaves other values untouched
4. **Format Validation:** Ignores invalid formats (missing @, incomplete parentheses, etc.)
5. **Fail-Fast Behavior:** Propagates `RequestFailedException` when secrets are missing or inaccessible
6. **Null Safety:** Validates both configuration and secretClient parameters

## Test Coverage

Implemented 9 comprehensive tests covering:

- Simple value replacement
- Nested value replacement (e.g., `Database:Password`)
- Deep nesting (3+ levels: `A:B:C:Secret`)
- Non-KeyVault values remain unchanged
- Mixed configuration (some KeyVault, some plain)
- Invalid format handling (missing @, incomplete syntax)
- Null argument validation (both parameters)
- Exception propagation for missing secrets

All tests pass using NSubstitute mocks for `SecretClient`.

## TDD Execution

Followed strict RED-GREEN-REFACTOR cycle:

1. **RED Phase (commit 0f224a2):** Created 9 failing tests
2. **GREEN Phase (commit 3614bc2):** Implemented extension method to pass all tests
3. **REFACTOR Phase:** Not needed - code is clean and follows framework conventions

## Implementation Details

**Extension Method Signature:**
```csharp
public static void ReplaceKeyVaultReferences(
    this IConfiguration configuration,
    SecretClient secretClient)
```

**Algorithm:**
1. Validate parameters with `ArgumentNullException.ThrowIfNull`
2. Recursively traverse configuration sections via `GetChildren()`
3. For each section value:
   - Check if starts with `@KeyVault(` AND ends with `)`
   - Extract secret name: `Substring(10, Length - 11)`
   - Call `secretClient.GetSecret(secretName)` (exception propagates on failure)
   - Replace section value with secret value
4. Recurse into child sections

**XML Documentation:**
- Comprehensive summary and remarks
- Documented parameters and exceptions
- Included usage example with code block
- Listed all behavior characteristics

## Verification

✅ All 9 tests pass
✅ Extension method has comprehensive XML documentation
✅ Code follows framework naming conventions (no abbreviations)
✅ Tests cover simple, nested, deep nesting, invalid formats, and error cases
✅ Fail-fast behavior verified with exception propagation test

## Deviations from Plan

None - plan executed exactly as written.

## Decisions Made

| Decision | Rationale | Impact |
|----------|-----------|--------|
| Use @KeyVault(SecretName) syntax | Matches Azure App Service Key Vault references for developer familiarity | Developers can reuse knowledge from Azure PaaS experience |
| Fail-fast on missing secrets | Prevents silent configuration errors at startup | Catches misconfiguration early in deployment lifecycle |
| Recursive traversal | Supports nested configuration sections like `Database:ConnectionStrings:Primary` | Works with any configuration hierarchy depth |
| Extension method pattern | Follows .NET configuration conventions | Integrates seamlessly with existing IConfiguration usage |

## Files Modified

### Created Files

**src/Imagile.Framework.Configuration/Extensions/KeyVaultConfigurationExtensions.cs** (86 lines)
- Public static extension method `ReplaceKeyVaultReferences`
- Private recursive helper method
- Comprehensive XML documentation with examples

**tests/Imagile.Framework.Configuration.Tests/Extensions/KeyVaultConfigurationExtensionsTests.cs** (198 lines)
- 9 test cases covering all scenarios
- NSubstitute mocking helper method for `SecretClient`
- Tests for simple, nested, deep nesting, invalid formats, null arguments, and exception propagation

## Next Phase Readiness

**Ready for next plans:** Yes

**Blockers:** None

**Concerns:** None

**Notes:**
- Framework package ready for migration from imagile-app repository
- Tests demonstrate proper usage patterns for consuming applications
- Extension method is stateless and thread-safe
