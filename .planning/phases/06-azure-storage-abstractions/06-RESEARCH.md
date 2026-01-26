# Phase 6: Azure Storage Abstractions - Research

**Researched:** 2026-01-25
**Domain:** Azure Storage Queue and Blob patterns - SDK integration, convention-based initialization, attribute-driven registration
**Confidence:** HIGH

## Summary

This phase creates reusable abstractions for Azure Queue Storage and Blob Storage, providing interface-based contracts while allowing consumers to work directly with Azure SDK clients. The standard approach uses Azure.Storage.Queues and Azure.Storage.Blobs SDKs with Microsoft.Extensions.Azure for dependency injection, static abstract interfaces for convention-based naming (C# 11 feature), and reflection-based scanning for automatic resource initialization.

The imagile-app codebase provides production-tested patterns: `IImagileQueueMessage` and `IImagineBlobContainer` interfaces with static abstract `DefaultQueueName`/`DefaultContainerName` properties, extension methods for type-safe client retrieval (`GetQueueClient<T>()`, `GetBlobContainerClient<T>()`), and `StorageSeeder` that uses reflection to discover all interface implementations and create queues/containers automatically.

The architecture leverages Azure SDK's built-in retry policies (exponential backoff with 5 retries, 0.8s delay by default), Microsoft.Extensions.Azure's named client pattern for multi-account scenarios, and AppTokenCredential from Phase 5 for authentication. Azure SDK exceptions (RequestFailedException) bubble up without wrapping, giving consumers direct access to error codes and details.

**Primary recommendation:** Adopt imagile-app patterns with framework-appropriate enhancements: keep static abstract interface pattern, add attribute-driven storage account association (`[StorageAccount("name")]`), implement centralized initialization helper that scans assemblies for interface implementations, provide fluent API for DI registration similar to Phase 5's configuration pattern, and leverage Microsoft.Extensions.Azure for named client management.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Azure.Storage.Queues | 12.21.0+ | Queue Storage client | Official Azure SDK with retry policies, Base64 encoding, DefaultAzureCredential support |
| Azure.Storage.Blobs | 12.26.0+ | Blob Storage client | Official Azure SDK with retry policies, streaming, directory operations via DataMovement |
| Microsoft.Extensions.Azure | 1.7.6+ | Azure client DI integration | Standard pattern for registering multiple Azure clients with named client support |
| Azure.Identity | 1.17.1 | Token credential chain | From Phase 5 - AppTokenCredential provides environment-aware auth |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.0 | IServiceCollection extensions | For fluent API builder pattern (AddStorageAbstractions) |
| Azure.Core | (transitive) | Common Azure types | Provides TokenCredential, RetryOptions, RequestFailedException |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Static abstract interfaces | Custom base class with virtual property | Static abstract is language-supported pattern (C# 11), no inheritance required |
| Microsoft.Extensions.Azure | Manual IAzureClientFactory implementation | Microsoft.Extensions.Azure is official Microsoft pattern, handles configuration and options |
| Convention-based scanning | Manual registration per queue/container | Scanning reduces boilerplate but adds reflection overhead at startup (acceptable) |
| Attribute-driven accounts | Named conventions (QueueMessage_AccountName) | Attributes are explicit, refactorable, type-safe |
| Exponential retry in SDK | Polly retry policy wrapper | SDK retry is built-in, tuned for Azure services, no extra dependency |

**Installation:**
```bash
# These will be in Directory.Packages.props for central management
dotnet add package Azure.Storage.Queues --version 12.21.0
dotnet add package Azure.Storage.Blobs --version 12.26.0
dotnet add package Microsoft.Extensions.Azure --version 1.7.6
# Azure.Identity 1.17.1 already referenced from Phase 5
```

## Architecture Patterns

### Recommended Project Structure
```
src/Imagile.Framework.Storage/
├── Attributes/
│   └── StorageAccountAttribute.cs            # [StorageAccount("name")] for multi-account
├── Interfaces/
│   ├── IQueueMessage.cs                       # Static abstract DefaultQueueName
│   └── IBlobContainer.cs                      # Static abstract DefaultContainerName
├── Extensions/
│   ├── StorageClientExtensions.cs             # GetQueueClient<T>(), GetBlobContainerClient<T>()
│   ├── ServiceCollectionExtensions.cs         # AddStorageAbstractions() fluent API
│   └── StorageInitializationExtensions.cs     # InitializeStorageResourcesAsync()
├── Initialization/
│   ├── StorageResourceScanner.cs              # Reflection-based interface discovery
│   └── StorageResourceInitializer.cs          # Creates queues/containers from scanned types
└── README.md
```

### Pattern 1: Static Abstract Interface for Convention-Based Naming
**What:** Use static abstract interface members (C# 11) to define required static properties on implementing types
**When to use:** Types need compile-time contract for metadata (queue names, container names) without inheritance
**Why:** Static abstract provides type-safe convention without base class, accessible via generic constraints
**Example:**
```csharp
// Source: imagile-app pattern (verified working)
namespace Imagile.Framework.Storage.Interfaces;

/// <summary>
/// Base interface for Azure Queue messages with a default queue name.
/// </summary>
public interface IQueueMessage
{
    /// <summary>
    /// Gets the default queue name for this message type.
    /// </summary>
    static abstract string DefaultQueueName { get; }
}

/// <summary>
/// Base interface for Azure Blob containers with a default container name.
/// </summary>
public interface IBlobContainer
{
    /// <summary>
    /// Gets the default container name for this container type.
    /// </summary>
    static abstract string DefaultContainerName { get; }
}

// Consumer implementation
public class TenantVerificationMessage : IQueueMessage
{
    public static string DefaultQueueName => "tenant-verification";

    public int TenantId { get; set; }
    public DateTime RequestedAt { get; set; }
}
```

### Pattern 2: Type-Safe Extension Methods for Client Retrieval
**What:** Extension methods on service clients that use generic type parameter to resolve queue/container name
**When to use:** Accessing queues or containers in a type-safe way without hardcoding names
**Why:** IntelliSense support, compile-time safety, centralizes naming convention
**Example:**
```csharp
// Source: imagile-app StorageClientExtensions.cs (verified working)
namespace Imagile.Framework.Storage.Extensions;

public static class StorageClientExtensions
{
    /// <summary>
    /// Gets a queue client for the specified message type using its default queue name.
    /// </summary>
    /// <typeparam name="T">The queue message type.</typeparam>
    /// <param name="client">The queue service client.</param>
    /// <returns>A queue client for the specified message type.</returns>
    public static QueueClient GetQueueClient<T>(this QueueServiceClient client)
        where T : IQueueMessage
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.GetQueueClient(T.DefaultQueueName);
    }

    /// <summary>
    /// Gets a blob container client for the specified container type using its default container name.
    /// </summary>
    /// <typeparam name="T">The blob container type.</typeparam>
    /// <param name="client">The blob service client.</param>
    /// <returns>A blob container client for the specified container type.</returns>
    public static BlobContainerClient GetBlobContainerClient<T>(this BlobServiceClient client)
        where T : IBlobContainer
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.GetBlobContainerClient(T.DefaultContainerName);
    }
}

// Usage in application code
public class TenantService
{
    private readonly QueueServiceClient _queueClient;

    public async Task QueueVerification(int tenantId)
    {
        var queue = _queueClient.GetQueueClient<TenantVerificationMessage>();
        await queue.SendMessageAsync(JsonSerializer.Serialize(new TenantVerificationMessage
        {
            TenantId = tenantId
        }));
    }
}
```

### Pattern 3: Reflection-Based Convention Scanning
**What:** Use reflection to discover all types implementing storage interfaces in specified assemblies
**When to use:** Automatic initialization of storage resources (queues, containers) at application startup
**Why:** Reduces boilerplate registration, ensures consistency, enables "convention over configuration"
**Example:**
```csharp
// Source: imagile-app StorageSeeder.cs (adapted for framework)
namespace Imagile.Framework.Storage.Initialization;

public static class StorageResourceScanner
{
    /// <summary>
    /// Scans assemblies for types implementing storage interfaces.
    /// </summary>
    /// <param name="assemblies">Assemblies to scan. If null, scans calling assembly.</param>
    /// <returns>Discovered queue and container types.</returns>
    public static StorageResources ScanForResources(params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
        {
            assemblies = new[] { Assembly.GetCallingAssembly() };
        }

        var queueTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IQueueMessage).IsAssignableFrom(t))
            .ToList();

        var containerTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IBlobContainer).IsAssignableFrom(t))
            .ToList();

        return new StorageResources(queueTypes, containerTypes);
    }

    /// <summary>
    /// Gets the queue name from a type implementing IQueueMessage.
    /// </summary>
    public static string GetQueueName(Type queueMessageType)
    {
        var property = queueMessageType.GetProperty("DefaultQueueName",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        return property?.GetValue(null) as string
            ?? throw new InvalidOperationException($"Type {queueMessageType.Name} does not have DefaultQueueName");
    }

    /// <summary>
    /// Gets the container name from a type implementing IBlobContainer.
    /// </summary>
    public static string GetContainerName(Type blobContainerType)
    {
        var property = blobContainerType.GetProperty("DefaultContainerName",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        return property?.GetValue(null) as string
            ?? throw new InvalidOperationException($"Type {blobContainerType.Name} does not have DefaultContainerName");
    }
}

public record StorageResources(List<Type> QueueTypes, List<Type> ContainerTypes);
```

### Pattern 4: Attribute-Driven Storage Account Association
**What:** Use custom attribute to associate queue/container types with named storage accounts
**When to use:** Application uses multiple storage accounts for different purposes (e.g., hot/cool, public/private)
**Why:** Explicit, refactorable, type-safe; avoids naming conventions or magic strings in code
**Example:**
```csharp
// Proposed pattern (not in imagile-app, but follows EF Core DbContext attribute pattern)
namespace Imagile.Framework.Storage.Attributes;

/// <summary>
/// Specifies which storage account a queue or container type should use.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class StorageAccountAttribute : Attribute
{
    /// <summary>
    /// Gets the storage account name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageAccountAttribute"/> class.
    /// </summary>
    /// <param name="name">The storage account name used during DI registration.</param>
    public StorageAccountAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
}

// Usage in application code
[StorageAccount("primary")]
public class TenantVerificationMessage : IQueueMessage
{
    public static string DefaultQueueName => "tenant-verification";
    public int TenantId { get; set; }
}

[StorageAccount("archive")]
public class AuditLogContainer : IBlobContainer
{
    public static string DefaultContainerName => "audit-logs";
    public string LogPath { get; set; }
}

// DI registration
services.AddStorageAbstractions(config =>
{
    config.AddStorageAccount("primary",
        connectionString: configuration["Storage:Primary"],
        credential: appTokenCredential);

    config.AddStorageAccount("archive",
        connectionString: configuration["Storage:Archive"],
        credential: appTokenCredential);

    config.ScanAssemblies(typeof(TenantVerificationMessage).Assembly);
});
```

### Pattern 5: Microsoft.Extensions.Azure for Named Clients
**What:** Use Microsoft.Extensions.Azure to register multiple storage account clients with names
**When to use:** Multi-storage-account scenarios where framework needs to resolve correct client per type
**Why:** Official Microsoft pattern, handles configuration/options/retry, integrates with IServiceProvider
**Example:**
```csharp
// Source: Microsoft.Extensions.Azure official docs + imagile-app pattern
services.AddAzureClients(builder =>
{
    // Default storage account (no name)
    builder.AddBlobServiceClient(connectionString);
    builder.AddQueueServiceClient(connectionString)
        .ConfigureOptions(options => options.MessageEncoding = QueueMessageEncoding.Base64);

    // Named storage accounts
    builder.AddBlobServiceClient(archiveConnectionString)
        .WithName("archive");
    builder.AddQueueServiceClient(archiveConnectionString)
        .WithName("archive")
        .ConfigureOptions(options => options.MessageEncoding = QueueMessageEncoding.Base64);

    // Global credential (if connection string doesn't include SAS/key)
    if (credential != null)
    {
        builder.UseCredential(credential);
    }

    // Global retry configuration
    builder.ConfigureDefaults(options =>
    {
        options.Retry.Delay = TimeSpan.FromSeconds(2);
        options.Retry.MaxRetries = 5;
        options.Retry.Mode = RetryMode.Exponential;
        options.Retry.MaxDelay = TimeSpan.FromSeconds(10);
    });
});

// Retrieving named clients
public class StorageClientResolver
{
    private readonly IAzureClientFactory<QueueServiceClient> _queueFactory;
    private readonly IAzureClientFactory<BlobServiceClient> _blobFactory;

    public QueueClient GetQueueClient<T>() where T : IQueueMessage
    {
        string? accountName = GetStorageAccountName<T>();
        var queueService = accountName != null
            ? _queueFactory.CreateClient(accountName)
            : _queueFactory.CreateClient("Default");

        return queueService.GetQueueClient(T.DefaultQueueName);
    }

    private static string? GetStorageAccountName<T>()
    {
        var attribute = typeof(T).GetCustomAttribute<StorageAccountAttribute>();
        return attribute?.Name;
    }
}
```

### Pattern 6: Centralized Initialization Helper
**What:** Single method call that discovers all queues/containers and creates them via CreateIfNotExistsAsync
**When to use:** Local development setup, deployment scripts, or startup initialization
**Why:** Reduces manual queue/container creation, ensures resources exist before app runs
**Example:**
```csharp
// Source: imagile-app StorageSeeder.cs (adapted for multi-account framework)
namespace Imagile.Framework.Storage.Extensions;

public static class StorageInitializationExtensions
{
    /// <summary>
    /// Initializes all storage resources (queues and containers) discovered by assembly scanning.
    /// Creates queues and containers if they don't exist, grouped by storage account.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Summary of created resources.</returns>
    public static async Task<InitializationResult> InitializeStorageResourcesAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var scanner = serviceProvider.GetRequiredService<StorageResourceScanner>();
        var queueFactory = serviceProvider.GetRequiredService<IAzureClientFactory<QueueServiceClient>>();
        var blobFactory = serviceProvider.GetRequiredService<IAzureClientFactory<BlobServiceClient>>();

        var resources = scanner.GetDiscoveredResources();
        var result = new InitializationResult();

        // Group by storage account name (null = default)
        var queuesByAccount = resources.QueueTypes
            .GroupBy(t => t.GetCustomAttribute<StorageAccountAttribute>()?.Name ?? "Default");

        var containersByAccount = resources.ContainerTypes
            .GroupBy(t => t.GetCustomAttribute<StorageAccountAttribute>()?.Name ?? "Default");

        // Initialize queues per account
        foreach (var group in queuesByAccount)
        {
            var queueClient = queueFactory.CreateClient(group.Key);
            foreach (var queueType in group)
            {
                string queueName = StorageResourceScanner.GetQueueName(queueType);
                var queue = queueClient.GetQueueClient(queueName);
                var response = await queue.CreateIfNotExistsAsync(cancellationToken);
                if (response != null)
                {
                    result.CreatedQueues.Add($"{group.Key}/{queueName}");
                }
            }
        }

        // Initialize containers per account
        foreach (var group in containersByAccount)
        {
            var blobClient = blobFactory.CreateClient(group.Key);
            foreach (var containerType in group)
            {
                string containerName = StorageResourceScanner.GetContainerName(containerType);
                var container = blobClient.GetBlobContainerClient(containerName);
                var response = await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
                if (response != null)
                {
                    result.CreatedContainers.Add($"{group.Key}/{containerName}");
                }
            }
        }

        return result;
    }
}

// Usage in Program.cs or CLI tool
var app = builder.Build();

// Initialize storage resources at startup (local development)
if (app.Environment.IsDevelopment())
{
    var result = await app.Services.InitializeStorageResourcesAsync();
    app.Logger.LogInformation("Created {QueueCount} queues, {ContainerCount} containers",
        result.CreatedQueues.Count, result.CreatedContainers.Count);
}
```

### Anti-Patterns to Avoid
- **Wrapping Azure SDK clients in custom interfaces:** Azure SDK clients (QueueClient, BlobContainerClient) are designed to be used directly. Wrapping them loses IntelliSense, breaks SDK updates, and adds complexity without benefit. Use interfaces for message/container contracts, not client wrappers.
- **Custom retry logic outside Azure SDK:** Azure SDK has built-in exponential backoff tuned for Azure services. Custom Polly policies add complexity and can conflict with SDK retries, causing excessive delays.
- **Creating new service clients per operation:** QueueServiceClient and BlobServiceClient are thread-safe and designed for reuse. Creating new instances loses connection pooling and credential caching.
- **Mixing connection string and credential auth:** If connection string contains account key/SAS, credential is ignored. Pick one approach: connection string with key (local), or service URI + credential (cloud). Don't configure both.
- **Not using Base64 encoding for queue messages:** By default, Azure Queue messages aren't Base64-encoded, which breaks with special characters. Always configure `MessageEncoding = QueueMessageEncoding.Base64`.
- **Hardcoding queue/container names in business logic:** Defeats the purpose of convention-based patterns. Use `GetQueueClient<T>()` or `GetBlobContainerClient<T>()` to leverage static abstract properties.
- **Scanning all loaded assemblies:** Reflection scanning can be expensive. Only scan assemblies that contain storage types, passed explicitly to `ScanAssemblies()`.

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Queue retry on failure | Custom visibility timeout extension | Azure SDK MaxRetries with exponential backoff | SDK retry is idempotent-aware, handles transient errors, tested at scale |
| Blob upload with progress | Custom chunked upload with callbacks | BlobClient.UploadAsync with streams | SDK handles optimal block sizing, parallelization, resume on failure |
| Message serialization | Custom Binary/XML serialization | System.Text.Json with Base64 encoding | JSON is debuggable, cross-platform, versionable; Base64 handles special chars |
| Queue message poisoning | Manual dead-letter queue logic | Azure Queue built-in DequeueCount | Messages automatically move to poison queue after MaxDequeueCount threshold |
| Connection string parsing | Regex or manual split | Azure SDK constructors accept full connection strings | SDK handles account endpoint extraction, key parsing, SAS token validation |
| Multi-account client resolution | Custom factory with dictionary | Microsoft.Extensions.Azure IAzureClientFactory | Official pattern, integrates with DI, handles configuration/options |
| Assembly scanning for types | Manual GetTypes() with loops | LINQ queries with IsAssignableFrom | Cleaner, functional, leverages LINQ optimizations |
| Credential selection by environment | if/else environment checks | ChainedTokenCredential (AppTokenCredential from Phase 5) | Already solved in Phase 5, reuse credential singleton |

**Key insight:** Azure Storage SDK has 15+ years of hardening across millions of applications. Built-in retry handles throttling (503), timeouts (500), transient network errors. Queue poisoning (DequeueCount) prevents infinite processing loops. Blob chunking optimizes throughput based on network conditions. Microsoft.Extensions.Azure provides official DI patterns used across Azure SDKs. Don't rebuild these mature systems.

## Common Pitfalls

### Pitfall 1: PopReceipt Required for Message Deletion
**What goes wrong:** Deleting queue message with only MessageId fails; message reappears in queue
**Why it happens:** Azure Queue requires both MessageId and PopReceipt for deletion; PopReceipt proves you're the consumer that dequeued the message
**How to avoid:** Store both properties from ReceiveMessagesAsync result; pass both to DeleteMessageAsync
**Warning signs:** Duplicate message processing, messages never leave queue despite "successful" deletion
**Reference:** [QueueMessage.PopReceipt - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/azure.storage.queues.models.queuemessage.popreceipt?view=azure-dotnet) - "Required to delete the message"

### Pitfall 2: Queue Message Encoding Default Changed in v12
**What goes wrong:** Messages with special characters corrupted when switching from legacy SDK to v12+
**Why it happens:** v12 removed automatic Base64 encoding; messages stored as UTF-8 by default
**How to avoid:** Configure `QueueMessageEncoding.Base64` explicitly when adding QueueServiceClient
**Warning signs:** XML/JSON parsing errors, garbled text with special characters, encoding exceptions
**Reference:** [Azure Queue Storage tutorial - Microsoft Learn](https://learn.microsoft.com/en-us/azure/storage/queues/storage-tutorial-queues) - "Starting with v12, automatic Base64 encoding was removed"

### Pitfall 3: UseCredential Overrides Connection String Auth
**What goes wrong:** Connection string with account key works locally but breaks in cloud with UseCredential
**Why it happens:** When UseCredential is set, clients ignore embedded keys in connection strings; expected to use TokenCredential
**How to avoid:** Use connection string with key for local (Azurite); use service URI + credential for cloud (managed identity)
**Warning signs:** Authentication failures in cloud despite working locally; "shared key credentials required" errors
**Reference:** [Microsoft.Extensions.Azure README - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/microsoft.extensions.azure-readme) - "UseCredential sets default for all clients"

### Pitfall 4: Static Abstract Interfaces Require C# 11
**What goes wrong:** Build errors "feature is not available in C# 10" when using static abstract properties
**Why it happens:** Static abstract interface members introduced in C# 11 (.NET 7+)
**How to avoid:** Ensure `<LangVersion>11</LangVersion>` or higher in project file; verify target framework is net7.0+
**Warning signs:** Compiler errors CS8652, CS8703 mentioning static interface members
**Reference:** [Static abstract interface members - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-11.0/static-abstracts-in-interfaces) - "Introduced in C# 11"

### Pitfall 5: CreateIfNotExistsAsync Doesn't Throw on Exists
**What goes wrong:** Expecting exception when queue/container exists; code assumes success means creation
**Why it happens:** CreateIfNotExistsAsync returns null (no response) when resource exists, non-null when created
**How to avoid:** Check return value; null = already existed, non-null Response = created now
**Warning signs:** Initialization logic incorrectly reports "created X resources" including existing ones
**Reference:** [BlobContainerClient.CreateIfNotExistsAsync - Azure SDK](https://learn.microsoft.com/en-us/dotnet/api/azure.storage.blobs.blobcontainerclient.createifnotexistsasync) - "Returns null if already exists"

### Pitfall 6: RequestFailedException Wraps All Service Errors
**What goes wrong:** Catching generic exceptions misses Azure-specific error codes (404, 409, 403)
**Why it happens:** Azure SDK throws RequestFailedException with Status, ErrorCode properties for service errors
**How to avoid:** Catch RequestFailedException specifically; check ErrorCode (BlobErrorCode, QueueErrorCode enums) or Status
**Warning signs:** Generic error handling that can't distinguish "not found" from "access denied"
**Reference:** [Azure Storage retry policy - Microsoft Learn](https://learn.microsoft.com/en-us/azure/storage/blobs/storage-retry-policy) - "RequestFailedException with error codes"

### Pitfall 7: Named Client "Default" vs Unnamed Registration
**What goes wrong:** IAzureClientFactory.CreateClient() without name returns different client than unnamed registration
**Why it happens:** Unnamed registration is automatically named "Default"; CreateClient() with no args uses "Default"
**How to avoid:** Explicitly use "Default" string when calling CreateClient if you want the unnamed registration
**Warning signs:** Client resolution works sometimes but fails when specific name passed
**Reference:** [Microsoft.Extensions.Azure README - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/microsoft.extensions.azure-readme) - "Clients without name are named 'Default'"

### Pitfall 8: Reflection Scanning Doesn't Find Types in Unloaded Assemblies
**What goes wrong:** Some queues/containers not discovered despite implementing interfaces
**Why it happens:** Assembly scanning only finds types in assemblies already loaded into AppDomain
**How to avoid:** Pass Assembly instances explicitly to ScanAssemblies(); don't rely on AppDomain.CurrentDomain.GetAssemblies()
**Warning signs:** Missing queues/containers in initialization; inconsistent results depending on execution order
**Reference:** [Assembly.GetTypes - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.assembly.gettypes) - "Only loaded assemblies visible"

## Code Examples

Verified patterns from official sources and existing implementations:

### Complete IQueueMessage Pattern (imagile-app)
```csharp
// Source: imagile-app production code
namespace Imagile.Framework.Storage.Interfaces;

/// <summary>
/// Base interface for Azure Queue messages with a default queue name.
/// </summary>
/// <remarks>
/// Implementing types must provide a static DefaultQueueName property that returns
/// the queue name for this message type. This enables type-safe queue client retrieval
/// via <see cref="StorageClientExtensions.GetQueueClient{T}(QueueServiceClient)"/>.
/// </remarks>
/// <example>
/// <code>
/// public class TenantVerificationMessage : IQueueMessage
/// {
///     public static string DefaultQueueName => "tenant-verification";
///
///     public int TenantId { get; set; }
///     public DateTime RequestedAt { get; set; }
/// }
/// </code>
/// </example>
public interface IQueueMessage
{
    /// <summary>
    /// Gets the default queue name for this message type.
    /// </summary>
    static abstract string DefaultQueueName { get; }
}

/// <summary>
/// Base interface for Azure Blob containers with a default container name.
/// </summary>
public interface IBlobContainer
{
    /// <summary>
    /// Gets the default container name for this container type.
    /// </summary>
    static abstract string DefaultContainerName { get; }
}
```

### Azure SDK Retry Configuration (Official Docs)
```csharp
// Source: https://learn.microsoft.com/en-us/azure/storage/blobs/storage-retry-policy
using Azure.Storage.Blobs;
using Azure.Core;
using Azure.Identity;

// Configure retry options
BlobClientOptions blobOptions = new BlobClientOptions()
{
    Retry = {
        Delay = TimeSpan.FromSeconds(2),
        MaxRetries = 5,
        Mode = RetryMode.Exponential,
        MaxDelay = TimeSpan.FromSeconds(10),
        NetworkTimeout = TimeSpan.FromSeconds(100)
    },
};

// Create client with retry policy
BlobServiceClient blobServiceClient = new BlobServiceClient(
    new Uri($"https://{accountName}.blob.core.windows.net"),
    new DefaultAzureCredential(),
    blobOptions);

// Same pattern for Queue Storage
QueueClientOptions queueOptions = new QueueClientOptions()
{
    MessageEncoding = QueueMessageEncoding.Base64,
    Retry = {
        Delay = TimeSpan.FromSeconds(2),
        MaxRetries = 5,
        Mode = RetryMode.Exponential,
        MaxDelay = TimeSpan.FromSeconds(10)
    }
};
```

### Microsoft.Extensions.Azure Multi-Account Registration
```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/microsoft.extensions.azure-readme
using Microsoft.Extensions.Azure;
using Azure.Storage.Queues;
using Azure.Storage.Blobs;

services.AddAzureClients(builder =>
{
    // Primary storage account (default)
    builder.AddBlobServiceClient(primaryConnectionString);
    builder.AddQueueServiceClient(primaryConnectionString)
        .ConfigureOptions(options => options.MessageEncoding = QueueMessageEncoding.Base64);

    // Archive storage account (named)
    builder.AddBlobServiceClient(archiveConnectionString)
        .WithName("archive");
    builder.AddQueueServiceClient(archiveConnectionString)
        .WithName("archive")
        .ConfigureOptions(options => options.MessageEncoding = QueueMessageEncoding.Base64);

    // Use credential for token-based auth (overrides connection string keys)
    builder.UseCredential(new AppTokenCredential(managedIdentityClientId));

    // Global retry configuration (applies to all clients)
    builder.ConfigureDefaults(options =>
    {
        options.Retry.Mode = RetryMode.Exponential;
        options.Retry.MaxRetries = 5;
    });
});

// Retrieve named clients
public class MyService
{
    private readonly IAzureClientFactory<QueueServiceClient> _queueFactory;

    public MyService(IAzureClientFactory<QueueServiceClient> queueFactory)
    {
        _queueFactory = queueFactory;
    }

    public async Task ProcessArchive()
    {
        var archiveQueue = _queueFactory.CreateClient("archive");
        var queue = archiveQueue.GetQueueClient("archive-jobs");
        await queue.SendMessageAsync("job data");
    }
}
```

### Queue Message Send/Receive/Delete Pattern
```csharp
// Source: https://learn.microsoft.com/en-us/azure/storage/queues/storage-tutorial-queues
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

// Send message
QueueClient queue = queueServiceClient.GetQueueClient<TenantVerificationMessage>();
await queue.SendMessageAsync(JsonSerializer.Serialize(new TenantVerificationMessage
{
    TenantId = 123,
    RequestedAt = DateTime.UtcNow
}));

// Receive messages (up to 32 at once for performance)
QueueMessage[] messages = await queue.ReceiveMessagesAsync(maxMessages: 32);

foreach (var message in messages)
{
    // Deserialize
    var payload = JsonSerializer.Deserialize<TenantVerificationMessage>(message.Body.ToString());

    try
    {
        // Process message
        await ProcessVerification(payload);

        // Delete on success (requires MessageId AND PopReceipt)
        await queue.DeleteMessageAsync(message.MessageId, message.PopReceipt);
    }
    catch (Exception ex)
    {
        // On failure, message becomes visible again after timeout
        // DequeueCount increments; after threshold, moves to poison queue automatically
        _logger.LogError(ex, "Failed to process message {MessageId}", message.MessageId);
    }
}
```

### Blob Container Upload/Download Pattern
```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/storage.blobs-readme
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

// Get container client
BlobContainerClient container = blobServiceClient.GetBlobContainerClient<TenantDocumentContainer>();

// Upload blob
BlobClient blob = container.GetBlobClient($"tenants/{tenantId}/document.pdf");
using FileStream uploadStream = File.OpenRead(localFilePath);
await blob.UploadAsync(uploadStream, overwrite: true);

// Download blob
BlobDownloadInfo download = await blob.DownloadAsync();
using FileStream downloadStream = File.OpenWrite(localFilePath);
await download.Content.CopyToAsync(downloadStream);

// List blobs with prefix
await foreach (BlobItem blobItem in container.GetBlobsAsync(prefix: $"tenants/{tenantId}/"))
{
    Console.WriteLine($"Blob: {blobItem.Name}, Size: {blobItem.Properties.ContentLength}");
}

// Delete blob
await blob.DeleteIfExistsAsync();
```

### Assembly Scanning and Initialization
```csharp
// Source: imagile-app StorageSeeder.cs (adapted)
using System.Reflection;

public static class StorageResourceScanner
{
    public static async Task InitializeAsync(
        QueueServiceClient queueClient,
        BlobServiceClient blobClient,
        params Assembly[] assemblies)
    {
        // Find all queue message types
        var queueTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IQueueMessage).IsAssignableFrom(t));

        foreach (var type in queueTypes)
        {
            // Get static property value via reflection
            var property = type.GetProperty("DefaultQueueName",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            if (property?.GetValue(null) is string queueName)
            {
                var queue = queueClient.GetQueueClient(queueName);
                await queue.CreateIfNotExistsAsync();
            }
        }

        // Find all blob container types
        var containerTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IBlobContainer).IsAssignableFrom(t));

        foreach (var type in containerTypes)
        {
            var property = type.GetProperty("DefaultContainerName",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            if (property?.GetValue(null) is string containerName)
            {
                var container = blobClient.GetBlobContainerClient(containerName);
                await container.CreateIfNotExistsAsync();
            }
        }
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| WindowsAzure.Storage (legacy SDK) | Azure.Storage.Queues/Blobs v12+ | 2020 (v12 GA) | New SDK has modern async, better performance, simpler API; legacy SDK deprecated |
| Custom queue/container name constants | Static abstract interface members | C# 11 (.NET 7, 2022) | Type-safe convention without base class; compile-time validation |
| Manual Base64 encoding | QueueMessageEncoding.Base64 option | v12 (2020) | Explicit opt-in; v11 did it automatically, v12 requires configuration |
| DefaultAzureCredential everywhere | Custom ChainedTokenCredential (AppTokenCredential) | Azure.Identity 1.4+ (2021) | Faster auth (2-4 attempts vs 10+); Phase 5 pattern |
| Manual IAzureClientFactory implementation | Microsoft.Extensions.Azure | 2019 (GA) | Official DI pattern, handles configuration/retry/options |
| CloudQueueMessage with string body | QueueMessage with BinaryData body | v12 (2020) | BinaryData more flexible (JSON, Binary, XML); backward compatible via ToString() |
| Manual retry with Polly | Azure SDK built-in exponential backoff | Always existed, improved in v12 | SDK retry tuned for Azure throttling; Polly adds complexity |

**Deprecated/outdated:**
- **WindowsAzure.Storage package:** Replaced by Azure.Storage.Queues/Blobs/Files/DataLake (modular SDKs)
- **CloudStorageAccount.Parse():** Use QueueServiceClient/BlobServiceClient constructors directly with connection string
- **StorageCredentials class:** Use TokenCredential from Azure.Identity (DefaultAzureCredential, AppTokenCredential)
- **Automatic Base64 encoding assumption:** v12 removed this; must configure QueueMessageEncoding.Base64 explicitly
- **SharedAccessSignature (SAS) manual construction:** Use BlobSasBuilder/QueueSasBuilder classes for type-safe SAS generation

## Open Questions

Things that couldn't be fully resolved:

1. **Attribute vs interface for storage account association**
   - What we know: [StorageAccount("name")] attribute is explicit and refactorable; interface (IStorageAccount) requires multiple inheritance
   - What's unclear: Whether attribute lookup via reflection is acceptable performance-wise for per-request operations
   - Recommendation: Use attribute for initialization-time lookup (cached), introduce IAzureClientFactory resolver that caches attribute results per type for runtime use

2. **Convention for queue/container name casing**
   - What we know: Azure Storage names are case-sensitive, must be lowercase; best practice is kebab-case (e.g., "tenant-verification")
   - What's unclear: Whether framework should enforce lowercase or allow consumer flexibility
   - Recommendation: Document convention (lowercase, kebab-case) but don't enforce; validation would break advanced scenarios (mixed-case legacy systems)

3. **Error handling strategy for initialization failures**
   - What we know: CreateIfNotExistsAsync can throw on network errors, permission issues
   - What's unclear: Should framework fail-fast (throw), continue (log errors), or offer both modes?
   - Recommendation: Fail-fast by default (throws aggregate exception with all failures); provide optional `continueOnError: true` parameter for partial initialization scenarios

4. **Progress reporting for blob operations**
   - What we know: CONTEXT.md says "no progress reporting" to keep simple
   - What's unclear: If large file uploads (>100MB) are expected, lack of progress could feel like hang
   - Recommendation: Phase 6 skips progress; future phase or consumer app can use BlobClient.OpenWrite() with custom stream wrapper if needed

5. **DequeueCount threshold and poison queue handling**
   - What we know: Azure Queue automatically increments DequeueCount; messages exceed threshold become invisible
   - What's unclear: Whether framework should provide helper to move to dead-letter queue or expose poison messages
   - Recommendation: CONTEXT.md deferred this; Phase 6 documents the behavior but doesn't provide abstraction; consumers work with Azure SDK's built-in behavior

## Sources

### Primary (HIGH confidence)
- imagile-app repository at C:\Code\imagile-app - examined IImagileQueueMessage, IImagineBlobContainer, StorageSeeder, StorageClientExtensions patterns
- [Azure Storage Blobs client library for .NET - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/storage.blobs-readme?view=azure-dotnet) - BlobServiceClient, retry configuration, authentication
- [Implement retry policy using Azure Storage SDK - Microsoft Learn](https://learn.microsoft.com/en-us/azure/storage/blobs/storage-retry-policy) - Retry defaults, exponential backoff configuration
- [Azure Queue Storage tutorial - Microsoft Learn](https://learn.microsoft.com/en-us/azure/storage/queues/storage-tutorial-queues) - QueueClient usage, message operations, Base64 encoding
- [Microsoft.Extensions.Azure README - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/microsoft.extensions.azure-readme?view=azure-dotnet) - Named clients, IAzureClientFactory, UseCredential pattern
- [Static abstract interface members - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-11.0/static-abstracts-in-interfaces) - C# 11 feature specification

### Secondary (MEDIUM confidence)
- [Performance checklist for Queue Storage - Microsoft Learn](https://learn.microsoft.com/en-us/azure/storage/queues/storage-performance-checklist) - Best practices for scalability
- [IHttpClientFactory named clients - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory) - Named client pattern comparison
- [QueueMessage.PopReceipt - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/azure.storage.queues.models.queuemessage.popreceipt?view=azure-dotnet) - PopReceipt requirement for deletion
- [Best practices for Azure Storage SDK integration - Anthony Simmon](https://anthonysimmon.com/best-practices-azure-storage-sdk-integration-in-dotnet-apps/) - DI patterns, multi-account scenarios
- [Best practices for Azure SDK with ASP.NET Core - Azure SDK Blog](https://devblogs.microsoft.com/azure-sdk/best-practices-for-using-azure-sdk-with-asp-net-core/) - Dependency injection recommendations

### Tertiary (LOW confidence)
- [Azure Storage Queues New Feature: Pop-Receipt on Add Message - Azure Blog](https://azure.microsoft.com/en-us/blog/azure-storage-queues-new-feature-pop-receipt-on-add-message/) - Historical feature addition
- [C# 11 Features: Static Abstract Members - Medium](https://medium.com/@drbarnabus/c-11-features-static-abstract-members-in-interfaces-b4f81cabcf83) - Community article on static abstract pattern
- [Scrutor assembly scanning - GitHub](https://github.com/khellang/Scrutor) - Third-party library for convention-based scanning (reference only, not using)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - verified from official Microsoft docs, NuGet versions, and production imagile-app usage
- Architecture: HIGH - imagile-app provides complete working implementation; patterns verified against official docs
- Pitfalls: HIGH - documented from official docs (Base64 encoding, PopReceipt), SDK changelogs (v12 breaking changes), and imagile-app experience
- Multi-account pattern: MEDIUM - Microsoft.Extensions.Azure official but attribute-driven association is proposed enhancement

**Research date:** 2026-01-25
**Valid until:** 2026-02-25 (30 days - Azure SDK stable, but versions evolve; C# language features stable)

**Key migration notes:**
- IQueueMessage and IBlobContainer interfaces migrate from imagile-app with namespace change (Imagile.App.Backend.Storage.Interfaces → Imagile.Framework.Storage.Interfaces)
- StorageClientExtensions migrate as-is with enhanced XML docs
- StorageSeeder pattern needs enhancement for multi-account support (attribute lookup) and aggregate error handling
- Microsoft.Extensions.Azure already used in imagile-app; framework extends with attribute-driven account association
- AppTokenCredential from Phase 5 reuses singleton pattern for all storage clients
- Static abstract interfaces require C# 11 (already configured in Directory.Build.props with LangVersion)
