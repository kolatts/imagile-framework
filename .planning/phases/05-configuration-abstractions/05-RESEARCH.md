# Phase 5: Configuration Abstractions - Research

**Researched:** 2026-01-25
**Domain:** Azure Configuration Patterns - Key Vault Integration, Token Credentials, and Options Validation
**Confidence:** HIGH

## Summary

This phase extracts three proven configuration patterns from imagile-app into a new framework package (Imagile.Framework.Configuration). The patterns solve common Azure application configuration challenges: authentication token management, Key Vault secret references, and startup configuration validation.

The standard approach combines Azure.Identity for credential chains, Azure.Security.KeyVault.Secrets for secret retrieval, Microsoft.Extensions.Configuration for recursive configuration traversal, and Microsoft.Extensions.Options.DataAnnotations for eager validation. The imagile-app codebase provides production-tested implementations of AppTokenCredential (credential chain abstraction), ReplaceKeyVaultReferences (recursive configuration replacement), and validation patterns using ValidateOnStart.

The architecture is straightforward: migrate working code with minimal changes, wrap in fluent API for DI registration, maintain fail-fast semantics for configuration errors, and keep Azure App Service familiarity with @KeyVault() syntax.

**Primary recommendation:** Migrate AppTokenCredential and ReplaceKeyVaultReferences as-is (proven patterns), implement eager validation with aggregate exception reporting using ValidateOnStart, and package as a single Imagile.Framework.Configuration assembly with Azure dependencies included for simplicity.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Azure.Identity | 1.17.1 | Token credential implementations | Official Azure SDK authentication library with ChainedTokenCredential support |
| Azure.Security.KeyVault.Secrets | 4.8.0 | Key Vault secret retrieval | Official Azure SDK for Key Vault operations with SecretClient |
| Microsoft.Extensions.Configuration | 10.0.0 | Configuration abstraction | .NET standard for configuration access and manipulation |
| Microsoft.Extensions.Options | 10.0.0 | Options pattern infrastructure | .NET standard for strongly-typed configuration |
| Microsoft.Extensions.Options.DataAnnotations | 10.0.1 | Data annotation validation | Standard mechanism for validating options with [Required], [Range], etc. |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.0 | IServiceCollection extensions | For fluent API builder pattern (AddFrameworkConfiguration) |
| Azure.Core | (transitive) | TokenCredential base class | Inherited by AppTokenCredential, provided by Azure.Identity |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| ChainedTokenCredential | DefaultAzureCredential | DefaultAzureCredential tries many credentials (slower); ChainedTokenCredential gives precise control |
| @KeyVault() syntax | Azure App Configuration provider | App Configuration is heavier service; Key Vault is simpler for secrets-only scenarios |
| DataAnnotations validation | FluentValidation | DataAnnotations lighter dependency; FluentValidation more powerful but overkill for simple validation |
| Eager validation (ValidateOnStart) | Lazy validation (on first access) | Eager catches errors at startup (fail-fast); lazy allows app to start with bad config |
| Extension method on IConfiguration | Replace IConfigurationProvider | Extension method non-invasive; custom provider complex and breaks layering |

**Installation:**
```bash
# These will be in Directory.Packages.props for central management
dotnet add package Azure.Identity --version 1.17.1
dotnet add package Azure.Security.KeyVault.Secrets --version 4.8.0
dotnet add package Microsoft.Extensions.Configuration --version 10.0.0
dotnet add package Microsoft.Extensions.Options.DataAnnotations --version 10.0.1
```

## Architecture Patterns

### Recommended Project Structure
```
src/Imagile.Framework.Configuration/
├── Credentials/
│   └── AppTokenCredential.cs              # TokenCredential implementation
├── Extensions/
│   ├── ConfigurationExtensions.cs         # ReplaceKeyVaultReferences extension
│   └── ServiceCollectionExtensions.cs     # AddFrameworkConfiguration fluent API
├── Validation/
│   └── (future - currently using built-in ValidateDataAnnotations)
└── README.md
```

### Pattern 1: ChainedTokenCredential for Environment-Specific Auth
**What:** Encapsulate credential chain logic that varies by environment (cloud vs local) in a custom TokenCredential subclass
**When to use:** Applications that run locally (developer machine) and in Azure (Managed Identity/Workload Identity)
**Why:** Prevents credential sprawl across codebase; single credential instance adapts to environment
**Example:**
```csharp
// Source: imagile-app AppTokenCredential.cs (verified working pattern)
public class AppTokenCredential : TokenCredential
{
    private readonly TokenCredential _tokenCredential;

    public AppTokenCredential(string? managedIdentityClientId = null)
    {
        // Cloud: WorkloadIdentity (AKS) → ManagedIdentity (App Service/Container Apps)
        // Local: AzureCli (fastest) → VisualStudio
        _tokenCredential = !string.IsNullOrWhiteSpace(managedIdentityClientId)
            ? new ChainedTokenCredential(
                new WorkloadIdentityCredential(),
                new ManagedIdentityCredential(clientId: managedIdentityClientId))
            : new ChainedTokenCredential(
                new AzureCliCredential(),
                new VisualStudioCredential());
    }

    public override ValueTask<AccessToken> GetTokenAsync(
        TokenRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        return _tokenCredential.GetTokenAsync(requestContext, cancellationToken);
    }

    public override AccessToken GetToken(
        TokenRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        return _tokenCredential.GetToken(requestContext, cancellationToken);
    }
}
```

### Pattern 2: Recursive IConfiguration Traversal for In-Place Modification
**What:** Use GetChildren() recursively to visit all configuration sections and modify values in-place
**When to use:** Transforming configuration values after loading (e.g., replacing Key Vault references)
**Why:** Configuration system doesn't support tree traversal out of box; recursive pattern reaches all nested values
**Example:**
```csharp
// Source: imagile-app ConfigurationManagerExtensions.cs (verified working pattern)
public static void ReplaceKeyVaultReferences(
    this IConfiguration configuration,
    SecretClient secretClient)
{
    ArgumentNullException.ThrowIfNull(configuration);
    ArgumentNullException.ThrowIfNull(secretClient);

    ReplaceKeyVaultReferences(configuration.GetChildren(), secretClient);
}

private static void ReplaceKeyVaultReferences(
    IEnumerable<IConfigurationSection> configurationSections,
    SecretClient secretClient)
{
    foreach (var section in configurationSections)
    {
        // Check if value matches @KeyVault(SecretName) pattern
        if (section.Value?.StartsWith("@KeyVault(") == true &&
            section.Value.EndsWith(")"))
        {
            // Extract secret name: "@KeyVault(SecretName)" -> "SecretName"
            var secretName = section.Value.Substring(10, section.Value.Length - 11);

            // Fetch from Key Vault (throws if secret doesn't exist - fail fast)
            var secret = secretClient.GetSecret(secretName);

            // Replace value in-place
            section.Value = secret.Value.Value;
        }

        // Recurse into child sections
        ReplaceKeyVaultReferences(section.GetChildren(), secretClient);
    }
}
```

### Pattern 3: Fluent API Builder for DI Registration
**What:** Extension method on IServiceCollection that returns a builder object, enabling method chaining for configuration
**When to use:** Library registration that needs multiple optional configuration steps
**Why:** Clean syntax, discoverable via IntelliSense, follows .NET convention (AddLogging, AddOptions, etc.)
**Example:**
```csharp
// Fluent API usage (proposed for framework)
services.AddFrameworkConfiguration(config =>
{
    config.WithKeyVault(
        vaultUri: "https://my-vault.vault.azure.net/",
        credential: new AppTokenCredential(managedIdentityClientId: "...")
    )
    .WithValidation(); // Enables ValidateOnStart for all IOptions
});

// Implementation pattern
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFrameworkConfiguration(
        this IServiceCollection services,
        Action<FrameworkConfigurationBuilder> configure)
    {
        var builder = new FrameworkConfigurationBuilder(services);
        configure(builder);
        return services;
    }
}

public class FrameworkConfigurationBuilder
{
    private readonly IServiceCollection _services;

    public FrameworkConfigurationBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public FrameworkConfigurationBuilder WithKeyVault(
        Uri vaultUri,
        TokenCredential credential)
    {
        _services.AddSingleton(new SecretClient(vaultUri, credential));
        // Register extension method to be called during app startup
        return this;
    }

    public FrameworkConfigurationBuilder WithValidation()
    {
        // Enable ValidateOnStart for all registered options
        return this;
    }
}
```

### Pattern 4: Eager Validation with Aggregate Exception
**What:** Validate all IOptions<T> instances at startup using ValidateOnStart, collecting all errors before throwing
**When to use:** Applications that must fail fast if configuration is invalid (prevent partially-configured app from running)
**Why:** Catches all configuration errors together; better developer experience than failing on first validation issue
**Example:**
```csharp
// Standard pattern from Microsoft.Extensions.Options
services.AddOptions<DatabaseOptions>()
    .Bind(configuration.GetSection("Database"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

services.AddOptions<AzureOptions>()
    .Bind(configuration.GetSection("Azure"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Options classes with data annotations
public class DatabaseOptions
{
    [Required(ErrorMessage = "ConnectionString is required")]
    public string ConnectionString { get; set; } = "";

    [Range(1, 300, ErrorMessage = "Timeout must be between 1 and 300 seconds")]
    public int TimeoutSeconds { get; set; } = 30;
}

// Validation occurs during application startup
// Throws OptionsValidationException with all validation errors aggregated
```

### Pattern 5: Extension Methods on IConfiguration for Pipeline Operations
**What:** Extension methods that mutate configuration after providers load but before binding to options
**When to use:** Post-processing configuration values (Key Vault replacement, environment variable substitution, etc.)
**Why:** Keeps transformation logic separate from providers; operates on configuration tree, not individual values
**Example:**
```csharp
// Usage in Program.cs/Startup.cs
var builder = WebApplication.CreateBuilder(args);

// Load configuration from providers (appsettings.json, env vars, etc.)
var configuration = builder.Configuration;

// Create Key Vault client with AppTokenCredential
var credential = new AppTokenCredential(
    managedIdentityClientId: configuration["Azure:ManagedIdentityClientId"]);
var secretClient = new SecretClient(
    new Uri(configuration["Azure:KeyVaultUri"]!),
    credential);

// Replace @KeyVault() references before binding to options
configuration.ReplaceKeyVaultReferences(secretClient);

// Now bind to options - values already replaced
builder.Services.AddOptions<MyOptions>()
    .Bind(configuration.GetSection("MyOptions"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### Anti-Patterns to Avoid
- **Using DefaultAzureCredential in AppTokenCredential:** DefaultAzureCredential tries many credentials (EnvironmentCredential, WorkloadIdentity, ManagedIdentity, SharedTokenCache, VisualStudio, AzureCli, etc.) in sequence. Custom ChainedTokenCredential with only needed credentials is faster and more explicit.
- **Lazy Key Vault fetching (on-demand):** Fetching secrets lazily means app starts with broken config, errors surface later during request handling. Fail-fast at startup is better.
- **Silent fallback when Key Vault secret missing:** Returning null or default value when secret doesn't exist masks configuration errors. Throw exception immediately.
- **Attempting to remove configuration keys:** IConfiguration doesn't support removal. Setting values to null is the only option for "removal," but this has limitations.
- **Modifying IConfigurationSection.Value after binding:** Binding snapshots values; later modifications don't propagate to bound options. Must replace before binding.
- **Creating new AppTokenCredential for each SDK client:** Credential caching happens per instance; reuse single credential for all clients to benefit from cached tokens.

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Authentication across environments | Custom credential switching logic | ChainedTokenCredential with specific credentials | Azure.Identity handles token caching, refresh, retry logic, and thread safety |
| Key Vault secret caching | Manual cache with Dictionary<string, string> | Let Azure SDK handle caching | SecretClient caches automatically; manual cache doesn't handle expiration/refresh |
| Configuration validation errors | Throw individual exceptions | ValidateOnStart with OptionsValidationException | Built-in aggregates all errors; better DX seeing all issues at once |
| Token refresh timing | Manual timer/background task | Azure.Identity automatic refresh | TokenCredential proactively refreshes based on RefreshOn timestamp |
| Configuration tree traversal | Manually calling GetSection() repeatedly | Recursive GetChildren() pattern | Configuration structure can be arbitrarily nested; recursion handles all depths |
| Async credential acquisition | Wrapping GetToken in Task.Run | ValueTask<AccessToken> from Azure.Identity | TokenCredential uses ValueTask for efficient async; no allocation for sync paths |
| Error messages for missing secrets | Generic exception messages | RequestFailedException from Azure SDK | Includes HTTP status, error code, vault URI for debugging |

**Key insight:** Azure.Identity has sophisticated token management (proactive refresh, in-memory cache, thread-safety, retry with backoff). ChainedTokenCredential tries credentials in order and caches successful credential. The Key Vault SDK handles caching, versioning, and retries. Microsoft.Extensions.Options has built-in validation aggregation and lifecycle hooks. Don't rebuild these mature, tested systems.

## Common Pitfalls

### Pitfall 1: IConfigurationSection.Value Setter Limitations
**What goes wrong:** Setting section.Value works initially but doesn't persist or apply to all configuration providers
**Why it happens:** Configuration is designed to be read-only; Value setter works for in-memory changes but has limitations. Not all providers support mutation, and changes don't trigger reload events.
**How to avoid:** Understand that ReplaceKeyVaultReferences modifies in-memory configuration only. This works for the use case (startup transformation before binding) but won't work for runtime configuration changes.
**Warning signs:** Expecting configuration changes to persist to appsettings.json files or trigger IOptionsMonitor<T> reload
**Reference:** [GitHub Issue #61314](https://github.com/dotnet/runtime/issues/61314) - "Configuration is read-only, and the configuration pattern isn't designed to be programmatically writable."

### Pitfall 2: ValidateOnStart Only Validates Default Named Options
**What goes wrong:** Using named options with .AddOptions<T>("name") and ValidateOnStart, but validation only runs for last named option
**Why it happens:** Known limitation in Microsoft.Extensions.Options - ValidateOnStart doesn't aggregate validations across multiple named options properly
**How to avoid:** Document limitation if named options are used; consider validating in custom startup code if using named options extensively
**Warning signs:** Some named option configurations not validated at startup despite ValidateOnStart
**Reference:** [GitHub Issue #51171](https://github.com/dotnet/runtime/issues/51171) - "Eager options validation only validates the last named option"

### Pitfall 3: Not Reusing Credential Instances
**What goes wrong:** Creating new AppTokenCredential() for each Azure SDK client causes excessive token requests and potential throttling
**Why it happens:** Each TokenCredential instance has its own in-memory cache; ManagedIdentityCredential uses static cache, but others don't
**How to avoid:** Register AppTokenCredential as singleton in DI; inject same instance into all Azure SDK clients (SecretClient, BlobServiceClient, etc.)
**Warning signs:** High volume of token requests to Microsoft Entra ID, HTTP 429 throttling responses
**Reference:** [Authentication best practices](https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/best-practices) - "A high-volume app that doesn't reuse credentials may encounter HTTP 429 throttling responses"

### Pitfall 4: Forgetting WorkloadIdentityCredential for AKS
**What goes wrong:** AppTokenCredential works locally and in App Service but fails in Azure Kubernetes Service (AKS)
**Why it happens:** AKS with Workload Identity uses federated credentials, not managed identity directly; WorkloadIdentityCredential reads environment variables set by AKS webhook
**How to avoid:** Include WorkloadIdentityCredential BEFORE ManagedIdentityCredential in chain (WorkloadIdentity is preferred for AKS)
**Warning signs:** Authentication failures in AKS despite working in other Azure services
**Reference:** [Azure Workload Identity for AKS](https://learn.microsoft.com/en-us/azure/aks/workload-identity-overview) - "WorkloadIdentityCredential reads configuration from environment variables set by the webhook"

### Pitfall 5: Key Vault Secret Name Restrictions Not Validated
**What goes wrong:** ReplaceKeyVaultReferences extracts secret name from @KeyVault(SecretName) but doesn't validate against Key Vault naming rules
**Why it happens:** Key Vault secret names must match ^[0-9a-zA-Z-]+$ (alphanumeric and hyphens only); app may reference invalid names
**How to avoid:** Either validate secret name format before calling GetSecret, or let RequestFailedException surface the error (fail-fast approach)
**Warning signs:** Cryptic error messages like "The secret name 'My.Secret' does not match pattern"
**Reference:** Azure Key Vault naming rules - secret names must be alphanumeric and hyphens only, 1-127 characters

### Pitfall 6: @KeyVault() Case Sensitivity
**What goes wrong:** Configuration uses "@keyvault(SecretName)" (lowercase) and isn't replaced
**Why it happens:** Current implementation uses StartsWith("@KeyVault(") which is case-sensitive; appsettings.json keys are case-insensitive but values are case-sensitive
**How to avoid:** Document that @KeyVault() syntax is case-sensitive, or use StringComparison.OrdinalIgnoreCase in implementation
**Warning signs:** Key Vault references not replaced despite valid syntax; secrets remain as "@keyvault(...)" in bound options

### Pitfall 7: Validation Runs Before Key Vault Replacement
**What goes wrong:** ValidateOnStart fails because it sees "@KeyVault(...)" value instead of actual secret value
**Why it happens:** Order matters - must call ReplaceKeyVaultReferences BEFORE binding configuration to options with validation
**How to avoid:** In fluent API, ensure Key Vault replacement is triggered before any IOptions<T> validation occurs
**Warning signs:** Validation errors like "ConnectionString must be a valid URL" when value is still "@KeyVault(...)"

### Pitfall 8: Token Expiration Not Handled
**What goes wrong:** AppTokenCredential works initially but fails after 60-90 minutes with authentication errors
**Why it happens:** Access tokens expire; application assumes credential failures mean auth setup is broken, not token expiration
**How to avoid:** Understand that TokenCredential.GetTokenAsync automatically refreshes; if seeing auth failures, it's likely network/permission issue, not token expiration. Log credential exceptions with full details.
**Warning signs:** Intermittent auth failures that resolve after app restart; failures correlated with 60-90 minute intervals
**Reference:** Token lifetime is 60-90 minutes average; TokenCredential automatically refreshes proactively based on RefreshOn timestamp

## Code Examples

Verified patterns from official sources and existing implementation:

### AppTokenCredential Implementation (Complete)
```csharp
// Source: imagile-app AppTokenCredential.cs (production-tested)
using Azure.Core;
using Azure.Identity;

namespace Imagile.Framework.Configuration.Credentials;

/// <summary>
/// Provides environment-aware token credential that adapts authentication method
/// based on deployment context (cloud vs local development).
/// </summary>
/// <remarks>
/// In cloud environments (Azure), uses WorkloadIdentityCredential (for AKS) and
/// ManagedIdentityCredential (for App Service/Container Apps). In local development,
/// uses AzureCliCredential (fastest) and VisualStudioCredential.
///
/// The credential chain approach allows the same code to run across environments
/// without environment-specific switching logic in application code.
/// </remarks>
public class AppTokenCredential : TokenCredential
{
    private readonly TokenCredential _tokenCredential;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppTokenCredential"/> class.
    /// </summary>
    /// <param name="managedIdentityClientId">
    /// Optional client ID for user-assigned managed identity. When provided, uses
    /// cloud credential chain (WorkloadIdentity → ManagedIdentity). When null or
    /// empty, uses local development credential chain (AzureCli → VisualStudio).
    /// </param>
    public AppTokenCredential(string? managedIdentityClientId = null)
    {
        _tokenCredential = !string.IsNullOrWhiteSpace(managedIdentityClientId)
            ? new ChainedTokenCredential(
                new WorkloadIdentityCredential(),
                new ManagedIdentityCredential(clientId: managedIdentityClientId))
            : new ChainedTokenCredential(
                new AzureCliCredential(),
                new VisualStudioCredential());
    }

    /// <inheritdoc/>
    public override ValueTask<AccessToken> GetTokenAsync(
        TokenRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        return _tokenCredential.GetTokenAsync(requestContext, cancellationToken);
    }

    /// <inheritdoc/>
    public override AccessToken GetToken(
        TokenRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        return _tokenCredential.GetToken(requestContext, cancellationToken);
    }
}
```

### ReplaceKeyVaultReferences Implementation (Complete)
```csharp
// Source: imagile-app ConfigurationManagerExtensions.cs (production-tested)
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace Imagile.Framework.Configuration.Extensions;

/// <summary>
/// Extensions for IConfiguration to support Key Vault reference replacement.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Replaces Key Vault references in the configuration with values from the vault.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="secretClient">Secret client for accessing Key Vault.</param>
    /// <remarks>
    /// <para>
    /// Searches recursively through all configuration sections for values matching
    /// the pattern <c>@KeyVault(SecretName)</c> and replaces them with the actual
    /// secret value retrieved from Azure Key Vault.
    /// </para>
    /// <para>
    /// This method modifies configuration in-place and should be called after
    /// configuration providers load but before binding to options classes.
    /// </para>
    /// <para>
    /// If a secret doesn't exist or isn't accessible, the method throws an
    /// exception immediately (fail-fast behavior) to prevent the application
    /// from starting with incomplete configuration.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="configuration"/> or <paramref name="secretClient"/> is null.
    /// </exception>
    /// <exception cref="Azure.RequestFailedException">
    /// Thrown when a secret cannot be retrieved from Key Vault (does not exist,
    /// permission denied, or network error).
    /// </exception>
    /// <example>
    /// <code>
    /// // In appsettings.json:
    /// {
    ///   "Database": {
    ///     "ConnectionString": "@KeyVault(DbConnectionString)"
    ///   }
    /// }
    ///
    /// // In Program.cs:
    /// var configuration = builder.Configuration;
    /// var credential = new AppTokenCredential();
    /// var secretClient = new SecretClient(
    ///     new Uri(configuration["Azure:KeyVaultUri"]!),
    ///     credential);
    ///
    /// configuration.ReplaceKeyVaultReferences(secretClient);
    ///
    /// // Now Database:ConnectionString contains the actual connection string
    /// </code>
    /// </example>
    public static void ReplaceKeyVaultReferences(
        this IConfiguration configuration,
        SecretClient secretClient)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(secretClient);

        ReplaceKeyVaultReferences(configuration.GetChildren(), secretClient);
    }

    private static void ReplaceKeyVaultReferences(
        IEnumerable<IConfigurationSection> configurationSections,
        SecretClient secretClient)
    {
        foreach (var section in configurationSections)
        {
            // Check if value matches @KeyVault(SecretName) pattern
            if (section.Value?.StartsWith("@KeyVault(") == true &&
                section.Value.EndsWith(")"))
            {
                // Extract secret name from @KeyVault(SecretName)
                var secretName = section.Value.Substring(10, section.Value.Length - 11);

                // Fetch secret from Key Vault (throws if not found - fail fast)
                var secret = secretClient.GetSecret(secretName);

                // Replace configuration value in-place
                section.Value = secret.Value.Value;
            }

            // Recurse into child sections
            ReplaceKeyVaultReferences(section.GetChildren(), secretClient);
        }
    }
}
```

### Fluent API Registration Pattern
```csharp
// Proposed fluent API usage in consuming application
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFrameworkConfiguration(config =>
{
    // Configure Key Vault integration
    config.WithKeyVault(
        vaultUri: new Uri(builder.Configuration["Azure:KeyVaultUri"]!),
        credential: new AppTokenCredential(
            managedIdentityClientId: builder.Configuration["Azure:ManagedIdentityClientId"]
        )
    );

    // Enable eager validation for all IOptions
    config.WithValidation();
});

// Register options classes with validation
builder.Services.AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection("Database"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### Options Validation with Data Annotations
```csharp
// Options class with validation attributes
using System.ComponentModel.DataAnnotations;

namespace MyApp.Configuration;

public class DatabaseOptions
{
    [Required(ErrorMessage = "ConnectionString is required")]
    public string ConnectionString { get; set; } = "";

    [Range(1, 300, ErrorMessage = "Timeout must be between 1 and 300 seconds")]
    public int TimeoutSeconds { get; set; } = 30;

    [RegularExpression(@"^[0-9]+$", ErrorMessage = "MaxPoolSize must be a number")]
    public string MaxPoolSize { get; set; } = "100";
}

public class AzureOptions
{
    [Required]
    [Url(ErrorMessage = "KeyVaultUri must be a valid URL")]
    public string KeyVaultUri { get; set; } = "";

    // Optional - client ID for user-assigned managed identity
    public string? ManagedIdentityClientId { get; set; }
}
```

### Complete Integration Example
```csharp
// Program.cs showing complete integration pattern
var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// 1. Create credential (reuse across all Azure SDK clients)
var credential = new AppTokenCredential(
    managedIdentityClientId: configuration["Azure:ManagedIdentityClientId"]);

// 2. Create Key Vault client
var secretClient = new SecretClient(
    new Uri(configuration["Azure:KeyVaultUri"]!),
    credential);

// 3. Replace Key Vault references BEFORE binding to options
configuration.ReplaceKeyVaultReferences(secretClient);

// 4. Register options with validation (values already replaced)
builder.Services.AddOptions<DatabaseOptions>()
    .Bind(configuration.GetSection("Database"))
    .ValidateDataAnnotations()
    .ValidateOnStart(); // Throws OptionsValidationException at startup if invalid

builder.Services.AddOptions<AzureOptions>()
    .Bind(configuration.GetSection("Azure"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// 5. Register credential and Key Vault client for DI
builder.Services.AddSingleton<TokenCredential>(credential);
builder.Services.AddSingleton(secretClient);

var app = builder.Build();

// At this point, if any validation failed, app wouldn't reach here
// (ValidateOnStart would have thrown during builder.Build())
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| DefaultAzureCredential everywhere | Custom ChainedTokenCredential | Azure.Identity 1.4+ | DefaultAzureCredential tries 10+ credentials sequentially (slow); custom chain tries only 2-4 (fast) |
| ManagedIdentityCredential only | WorkloadIdentityCredential → ManagedIdentity | AKS Workload Identity GA (2023) | WorkloadIdentity is preferred for AKS; ManagedIdentity fallback for App Service/Container Apps |
| Custom IConfigurationProvider for Key Vault | Extension method with in-place replacement | Current | Provider approach complex and breaks layering; extension method simpler |
| Lazy validation (on access) | Eager validation (ValidateOnStart) | .NET 6 (2021) | Eager catches errors at startup (fail-fast); lazy allows broken app to start |
| Manual validation error messages | OptionsValidationException | .NET Core 2.1+ | Exception aggregates all validation errors with property names; better DX |
| VisualStudioCodeCredential | VisualStudioCredential (includes VS Code) | Azure.Identity 1.9+ | VisualStudioCredential now handles both Visual Studio and VS Code |
| Separate EnvironmentCredential | EnvironmentCredential not needed | Current | Managed Identity reads same environment variables; separate EnvironmentCredential redundant |

**Deprecated/outdated:**
- **VisualStudioCodeCredential:** Superseded by VisualStudioCredential which handles both VS and VS Code (Azure.Identity 1.9+)
- **Using GetSecret without version parameter:** Still works but GetSecretAsync is preferred for async scenarios
- **Custom configuration reload triggers:** IConfiguration doesn't support triggering reloads; immutable after startup for Key Vault scenario

## Open Questions

Things that couldn't be fully resolved:

1. **Fluent API method naming consistency**
   - What we know: .NET uses both With* (WithUrls, WithDeveloperExceptionPage) and Add*/Use* (AddLogging, UseRouting) patterns
   - What's unclear: Whether to use WithKeyVault/WithValidation or ConfigureKeyVault/ConfigureValidation
   - Recommendation: Use With* prefix (WithKeyVault, WithValidation) as it reads better in lambda: `config => config.WithKeyVault(...).WithValidation()`. Matches "builder" pattern semantics.

2. **Validation behavior for nested options**
   - What we know: ValidateDataAnnotations validates properties with data annotations; recursion depends on validator implementation
   - What's unclear: Whether nested complex properties (e.g., DatabaseOptions contains nested PoolingOptions) are validated recursively
   - Recommendation: Test during implementation; if not recursive by default, document limitation and suggest MiniValidation library for recursive scenarios
   - Reference: [Andrew Lock article on recursive validation](https://andrewlock.net/validating-nested-dataannotation-options-recursively-with-minivalidation/)

3. **Key Vault caching behavior**
   - What we know: SecretClient caches secrets; cache details not documented in public API
   - What's unclear: Cache duration, invalidation policy, memory implications for large secret counts
   - Recommendation: Document that caching is opaque (SDK handles it); for configuration secrets, caching is beneficial (secrets don't change frequently). If secrets change at runtime, app restart is required anyway.

4. **IConfiguration mutation side effects**
   - What we know: Setting section.Value works for in-memory modification; doesn't persist or trigger reload
   - What's unclear: Whether IOptionsMonitor<T> would see changes if configuration reloads after ReplaceKeyVaultReferences
   - Recommendation: Document that ReplaceKeyVaultReferences is startup-only operation; runtime secret rotation requires app restart or custom implementation

5. **Error message quality for missing secrets**
   - What we know: RequestFailedException includes HTTP status, error code, message
   - What's unclear: Whether error messages clearly indicate which configuration key referenced the missing secret
   - Recommendation: During implementation, consider wrapping RequestFailedException with additional context (e.g., "Failed to replace Key Vault reference at configuration path 'Database:ConnectionString' with secret 'DbSecret': [original error]")

## Sources

### Primary (HIGH confidence)
- imagile-app repository at C:\Code\imagile-app - examined AppTokenCredential.cs, ConfigurationManagerExtensions.cs, and production usage
- [Credential chains in Azure Identity library - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/credential-chains) - ChainedTokenCredential patterns
- [Authentication best practices with Azure Identity - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/best-practices) - credential reuse and performance
- [IConfiguration.GetChildren() - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.iconfiguration.getchildren?view=net-9.0-pp) - recursive configuration traversal
- [Options pattern in ASP.NET Core - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-10.0) - ValidateOnStart and validation patterns
- [ValidateDataAnnotations - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.optionsbuilderdataannotationsextensions.validatedataannotations?view=net-10.0-pp) - data annotation validation API

### Secondary (MEDIUM confidence)
- [NuGet Gallery - Azure.Identity 1.17.1](https://www.nuget.org/packages/Azure.Identity) - latest stable version
- [NuGet Gallery - Azure.Security.KeyVault.Secrets 4.8.0](https://www.nuget.org/packages/Azure.Security.KeyVault.Secrets) - latest stable version
- [NuGet Gallery - Microsoft.Extensions.Options.DataAnnotations 10.0.1](https://www.nuget.org/packages/Microsoft.Extensions.Options.DataAnnotations/) - latest stable version
- [Azure Workload Identity for AKS - Microsoft Learn](https://learn.microsoft.com/en-us/azure/aks/workload-identity-overview) - WorkloadIdentityCredential usage
- [Debugging configuration values in ASP.NET Core - Andrew Lock](https://andrewlock.net/debugging-configuration-values-in-aspnetcore/) - recursive configuration traversal examples
- [Service Collection Extension Pattern in .NET Core - Medium](https://dotnetfullstackdev.medium.com/service-collection-extension-pattern-in-net-core-with-item-services-6db8cf9dcfd6) - fluent API builder pattern
- [GitHub Issue #61314 - Configuration read-only discussion](https://github.com/dotnet/runtime/issues/61314) - IConfiguration mutation limitations

### Tertiary (LOW confidence)
- [GitHub Issue #51171 - Eager options validation bug](https://github.com/dotnet/runtime/issues/51171) - ValidateOnStart named options limitation
- [Common Azure Key Vault error codes - Microsoft Learn](https://learn.microsoft.com/en-us/azure/key-vault/general/common-error-codes) - Key Vault error handling patterns
- [Validating nested DataAnnotation options with MiniValidation - Andrew Lock](https://andrewlock.net/validating-nested-dataannotation-options-recursively-with-minivalidation/) - recursive validation workaround

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - verified versions from NuGet, official documentation, and production usage in imagile-app
- Architecture: HIGH - existing implementations in imagile-app provide complete reference; patterns verified with official docs
- Pitfalls: MEDIUM - documented from research and known issues; some pitfalls identified from GitHub issues and community articles
- Don't hand-roll: HIGH - Azure.Identity and Key Vault SDK capabilities well-documented in official sources

**Research date:** 2026-01-25
**Valid until:** 2026-02-25 (30 days - Azure SDK stable domain, but versions evolve)

**Key migration notes:**
- AppTokenCredential migrates as-is from imagile-app with namespace change only
- ReplaceKeyVaultReferences migrates as-is with namespace change and enhanced XML docs
- Validation pattern uses standard Microsoft.Extensions.Options.DataAnnotations - no custom code needed beyond registration
- Package brings Azure.Identity and Azure.Security.KeyVault.Secrets dependencies even when Key Vault features not used (acceptable trade-off per CONTEXT.md)
- Fluent API builder is new pattern not in imagile-app - research shows standard .NET approach
