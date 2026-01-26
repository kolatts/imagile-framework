# Phase 5: Configuration Abstractions - Context

**Gathered:** 2026-01-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Extract reusable configuration patterns from imagile-app into the framework as a new package (Imagile.Framework.Configuration). Includes:
- AppTokenCredential pattern for Azure authentication
- Key Vault reference replacement (@KeyVault(SecretName) syntax)
- Configuration validation with data annotations

This phase creates the package and migrates these three patterns. Additional configuration utilities are separate phases.

</domain>

<decisions>
## Implementation Decisions

### Package Structure
- **Single package**: Imagile.Framework.Configuration (not split into Core + Azure)
- **Dependencies**: Imagile.Framework.Core, Azure.Identity, Azure.Security.KeyVault.Secrets, Microsoft.Extensions.Configuration
- **DI registration**: Fluent API pattern - `services.AddFrameworkConfiguration(config => config.WithKeyVault(...).WithValidation(...))`
- Package brings Azure dependencies even when not using Key Vault features (acceptable trade-off for simpler consumption)

### AppTokenCredential Design
- Migrate as-is from imagile-app (no changes to credential chain logic)
- Cloud: ChainedTokenCredential(WorkloadIdentityCredential, ManagedIdentityCredential with optional client ID)
- Local: ChainedTokenCredential(AzureCliCredential, VisualStudioCredential)
- Constructor takes optional managedIdentityClientId parameter
- Inherits from Azure.Core.TokenCredential

### Key Vault Syntax
- **Syntax**: Keep `@KeyVault(SecretName)` only - no alternative formats
- **Error handling**: Fail fast at startup - throw exception immediately when secret is missing/inaccessible
- **Advanced features**: None - keep it simple:
  - No secret versioning support
  - No caching behavior (always gets latest)
  - No nested reference resolution
- Matches App Service Key Vault reference syntax for familiarity
- Extension method: `configuration.ReplaceKeyVaultReferences(secretClient)` - recursive through all sections

### Validation Approach
- **Mechanism**: Data annotations ([Required], [Range], custom attributes)
- **Timing**: Eager validation at startup when AddFrameworkConfiguration() is called
- **Error reporting**: Aggregate exception - collect ALL validation errors, throw single exception with detailed message
- **Utilities**: None - no base classes, no helper methods, no result types. Developers use standard IOptions<T> pattern.
- Uses Microsoft.Extensions.Options.DataAnnotations.ValidateDataAnnotations()

### Claude's Discretion
- Exact fluent API method naming (WithKeyVault vs UseKeyVault vs ConfigureKeyVault)
- XML documentation detail level
- Example code in documentation
- Unit test coverage approach

</decisions>

<specifics>
## Specific Ideas

- "AppTokenCredential should work exactly like it does in imagile-app - proven pattern"
- "Fail fast is important - don't want apps running with misconfiguration"
- "@KeyVault() syntax matches Azure App Service, so developers will recognize it"
- Extension methods in fluent API should be chainable for clean setup

</specifics>

<deferred>
## Deferred Ideas

None - discussion stayed within phase scope

</deferred>

---

*Phase: 05-configuration-abstractions*
*Context gathered: 2026-01-26*
