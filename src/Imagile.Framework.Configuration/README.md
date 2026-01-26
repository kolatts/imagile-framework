# Imagile.Framework.Configuration

Azure configuration abstractions for .NET applications. Provides seamless integration with Azure Key Vault, automatic credential management for cloud and local development, and comprehensive configuration validation.

## Installation

```bash
dotnet add package Imagile.Framework.Configuration
```

## Quick Start

```csharp
using Imagile.Framework.Configuration.Extensions;

var builder = WebApplication.CreateBuilder(args);

// One-line setup: Key Vault + Validation
builder.Services.AddFrameworkConfiguration(builder.Configuration, config => config
    .WithKeyVault(new Uri("https://myvault.vault.azure.net/"))
    .WithValidation());

var app = builder.Build();
app.Run();
```

## Features

### AppTokenCredential - Cloud and Local Authentication

Automatically selects the appropriate Azure authentication method based on environment:

**Cloud (Azure):**
- WorkloadIdentityCredential (AKS with federated identity)
- ManagedIdentityCredential (App Service, Container Apps, Functions, VMs)

**Local Development:**
- AzureCliCredential (fastest, cross-platform)
- VisualStudioCredential (Visual Studio account)

No code changes needed when moving between environments.

### Key Vault Reference Replacement

Store secrets in Azure Key Vault and reference them in configuration using `@KeyVault(SecretName)` syntax:

**appsettings.json:**
```json
{
  "Database": {
    "Host": "localhost",
    "Password": "@KeyVault(DbPassword)"
  },
  "Api": {
    "Key": "@KeyVault(ApiKey)"
  }
}
```

The framework automatically replaces these references with actual secret values from Key Vault at startup.

### Configuration Validation

Validate configuration using data annotations with recursive validation and aggregated error messages:

```csharp
public class DatabaseSettings
{
    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; }

    [Required]
    public RetrySettings Retry { get; set; } = null!;
}

public class RetrySettings
{
    [Range(0, 10)]
    public int MaxAttempts { get; set; }
}

// Validate configuration
var settings = configuration.Get<DatabaseSettings>()!;
settings.ValidateRecursively(); // Throws ConfigurationValidationException with all errors
```

## Usage Examples

### Minimal Setup (Key Vault Only)

```csharp
builder.Services.AddFrameworkConfiguration(builder.Configuration, config => config
    .WithKeyVault(new Uri("https://myvault.vault.azure.net/")));
```

### Full Setup with User-Assigned Managed Identity

```csharp
builder.Services.AddFrameworkConfiguration(builder.Configuration, config => config
    .WithKeyVault(
        new Uri("https://myvault.vault.azure.net/"),
        builder.Configuration["AzureManagedIdentityClientId"])
    .WithValidation());
```

### Manual Usage Without Fluent API

```csharp
// Create credential
var credential = new AppTokenCredential(managedIdentityClientId: null); // null = local dev
builder.Services.AddSingleton<TokenCredential>(credential);

// Create Secret Client
var keyVaultUri = new Uri(builder.Configuration["Azure:KeyVaultUrl"]!);
var secretClient = new SecretClient(keyVaultUri, credential);
builder.Services.AddSingleton(secretClient);

// Replace Key Vault references
builder.Configuration.ReplaceKeyVaultReferences(secretClient);

// Validate configuration
var appSettings = builder.Configuration.Get<AppSettings>()!;
appSettings.ValidateRecursively();
```

### Validation Only (No Key Vault)

```csharp
builder.Services.AddFrameworkConfiguration(builder.Configuration, config => config
    .WithValidation());
```

## API Reference

### ServiceCollectionExtensions

- **`AddFrameworkConfiguration(IServiceCollection, IConfiguration, Action<FrameworkConfigurationBuilder>)`**
  Primary entry point for configuring Framework Configuration features.

### FrameworkConfigurationBuilder

- **`WithKeyVault(Uri keyVaultUri, string? managedIdentityClientId = null)`**
  Enable Key Vault integration with automatic reference replacement.

- **`WithKeyVault(string keyVaultUrl, string? managedIdentityClientId = null)`**
  Enable Key Vault integration using string URL.

- **`WithValidation()`**
  Enable configuration validation using data annotations.

### ConfigurationValidationExtensions

- **`ValidateRecursively(object obj)`**
  Validate an object and its nested properties. Throws `ConfigurationValidationException` on failure.

- **`TryValidateRecursive(object obj, List<ValidationResult> results)`**
  Validate without throwing. Returns `true` if valid, populates `results` with errors if invalid.

### AppTokenCredential

- **`AppTokenCredential(string? managedIdentityClientId = null)`**
  Create credential for Azure authentication. Automatically selects cloud or local credential chain.

### KeyVaultConfigurationExtensions

- **`ReplaceKeyVaultReferences(IConfiguration, SecretClient)`**
  Replace all `@KeyVault(SecretName)` references with actual secret values.

## License

MIT License - see LICENSE file for details.

## Contributing

Contributions are welcome! Please open an issue or pull request on GitHub.

## Repository

https://github.com/imagile/imagile-framework
