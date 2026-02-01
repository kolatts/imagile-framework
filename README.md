# Imagile Framework

Production-ready .NET packages with zero-dependency foundations and Azure-native integrations.

[![NuGet Core](https://img.shields.io/nuget/v/Imagile.Framework.Core.svg)](https://www.nuget.org/packages/Imagile.Framework.Core) [![NuGet EF](https://img.shields.io/nuget/v/Imagile.Framework.EntityFrameworkCore.svg)](https://www.nuget.org/packages/Imagile.Framework.EntityFrameworkCore) [![NuGet EF Testing](https://img.shields.io/nuget/v/Imagile.Framework.EntityFrameworkCore.Testing.svg)](https://www.nuget.org/packages/Imagile.Framework.EntityFrameworkCore.Testing) [![NuGet Blazor](https://img.shields.io/nuget/v/Imagile.Framework.Blazor.ApplicationInsights.svg)](https://www.nuget.org/packages/Imagile.Framework.Blazor.ApplicationInsights) [![NuGet Configuration](https://img.shields.io/nuget/v/Imagile.Framework.Configuration.svg)](https://www.nuget.org/packages/Imagile.Framework.Configuration) [![NuGet Storage](https://img.shields.io/nuget/v/Imagile.Framework.Storage.svg)](https://www.nuget.org/packages/Imagile.Framework.Storage)

## Overview

The Imagile Framework is a collection of opinionated, well-tested NuGet packages designed to accelerate .NET development. From declarative attributes to Azure Storage abstractions, each package solves specific challenges while maintaining clean separation of concerns.

## Package Catalog

| Package | Description | Dependencies |
|---------|-------------|--------------|
| **[Imagile.Framework.Core]** | Zero-dependency declarative attributes for associations, metadata, and validation markers | None |
| **[Imagile.Framework.EntityFrameworkCore]** | Multi-tenant audit logging with automatic timestamp and user tracking | Core, EF Core 10.0 |
| **[Imagile.Framework.EntityFrameworkCore.Testing]** | Convention tests for DbContexts with fluent exclusion configuration | Core, EF Core 10.0, xUnit |
| **[Imagile.Framework.Blazor.ApplicationInsights]** | Client-side telemetry for Blazor WebAssembly with JavaScript interop | Core, Application Insights Web SDK |
| **[Imagile.Framework.Configuration]** | Azure Key Vault integration with automatic credential management and recursive validation | Core, Azure.Security.KeyVault.Secrets, Azure.Identity |
| **[Imagile.Framework.Storage]** | Type-safe Azure Storage abstractions with convention-based queue and blob container access | Configuration, Azure.Storage.Queues, Azure.Storage.Blobs |

[Imagile.Framework.Core]: https://www.nuget.org/packages/Imagile.Framework.Core
[Imagile.Framework.EntityFrameworkCore]: https://www.nuget.org/packages/Imagile.Framework.EntityFrameworkCore
[Imagile.Framework.EntityFrameworkCore.Testing]: https://www.nuget.org/packages/Imagile.Framework.EntityFrameworkCore.Testing
[Imagile.Framework.Blazor.ApplicationInsights]: https://www.nuget.org/packages/Imagile.Framework.Blazor.ApplicationInsights
[Imagile.Framework.Configuration]: https://www.nuget.org/packages/Imagile.Framework.Configuration
[Imagile.Framework.Storage]: https://www.nuget.org/packages/Imagile.Framework.Storage

## Architecture Overview

```
Core (zero dependencies)
  |
  +-- EntityFrameworkCore
  |     |
  |     +-- EntityFrameworkCore.Testing
  |
  +-- Blazor.ApplicationInsights
  |
  +-- Configuration
        |
        +-- Storage
```

**Dependency Flow:**
- **Core** provides foundational attributes with no external dependencies
- **EntityFrameworkCore** extends EF Core with audit logging patterns
- **EntityFrameworkCore.Testing** adds convention testing for DbContexts
- **Blazor.ApplicationInsights** enables telemetry in WebAssembly applications
- **Configuration** integrates Azure Key Vault and AppTokenCredential for cloud/local auth
- **Storage** builds on Configuration to provide type-safe Azure Storage access

## Quick Start

### Installation

Install packages individually based on your needs:

```bash
# Zero-dependency attributes
dotnet add package Imagile.Framework.Core

# Entity Framework audit logging
dotnet add package Imagile.Framework.EntityFrameworkCore

# EF Core convention testing
dotnet add package Imagile.Framework.EntityFrameworkCore.Testing

# Blazor WASM telemetry
dotnet add package Imagile.Framework.Blazor.ApplicationInsights

# Azure configuration and Key Vault
dotnet add package Imagile.Framework.Configuration

# Azure Storage abstractions
dotnet add package Imagile.Framework.Storage
```

### Example: Azure Configuration with Key Vault

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

**appsettings.json:**
```json
{
  "Database": {
    "Host": "localhost",
    "Password": "@KeyVault(DbPassword)"
  }
}
```

The `@KeyVault(SecretName)` syntax is automatically replaced with actual secret values from Azure Key Vault at startup.

### Example: Type-Safe Azure Storage

```csharp
using Imagile.Framework.Storage.Interfaces;
using Imagile.Framework.Storage.Extensions;

// Define queue message type
public class UserNotificationMessage : IQueueMessage
{
    public static string DefaultQueueName => "user-notifications";
    public int UserId { get; set; }
    public string Message { get; set; } = string.Empty;
}

// Register and initialize storage
builder.Services.AddStorageAbstractions(config =>
{
    config.AddStorageAccount(builder.Configuration.GetConnectionString("AzureStorage")!);
    config.ScanAssemblies(typeof(UserNotificationMessage).Assembly);
});

var app = builder.Build();
await app.Services.InitializeStorageResourcesAsync();

// Use type-safe queue access
var queue = queueServiceClient.GetQueueClient<UserNotificationMessage>();
await queue.SendMessageAsync(JsonSerializer.Serialize(new UserNotificationMessage
{
    UserId = 123,
    Message = "Welcome to Imagile Framework!"
}));
```

## Requirements

- **.NET 10.0** or higher

All packages target .NET 10 with nullable reference types enabled, native AOT compatibility, and trimming support.

## License

This project is licensed under the **MIT License**.

Copyright (c) 2026 Imagile. All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

## Contributing

Contributions are welcome! Please open an issue or pull request on the GitHub repository.

1. **Fork the repository**
2. **Create a feature branch** (`git checkout -b feature/amazing-feature`)
3. **Commit your changes** (`git commit -m 'feat: add amazing feature'`)
4. **Push to the branch** (`git push origin feature/amazing-feature`)
5. **Open a Pull Request**

### Code Conventions

- Follow existing code style and naming conventions
- Add XML documentation for all public APIs
- Include unit tests for new features
- Ensure all tests pass before submitting PR

## Repository

https://github.com/kolatts/imagile-framework
