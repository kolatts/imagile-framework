# Imagile.Framework.Storage

Azure Storage abstractions for .NET applications. Provides type-safe queue and blob container access with convention-based naming, automatic resource initialization, and multi-storage-account support.

## Installation

```bash
dotnet add package Imagile.Framework.Storage
```

## Features

- **IQueueMessage** - Type-safe queue message contracts with convention-based naming
- **IBlobContainer** - Type-safe blob container contracts with convention-based naming
- **StorageAccountAttribute** - Multi-storage-account support via attributes
- **Convention-based initialization** - Automatic queue/container creation at startup
- **Type-safe extension methods** - Retrieve queue/container clients using generic types
- **DI integration** - Register storage accounts and scan assemblies for automatic discovery

## Usage Examples

### Type-Safe Queue Access

Define queue message types and access queues without hardcoded names:

```csharp
using Imagile.Framework.Storage.Interfaces;
using Imagile.Framework.Storage.Extensions;

// Define a queue message type
public class TenantVerificationMessage : IQueueMessage
{
    public static string DefaultQueueName => "tenant-verification";

    public int TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class UserNotificationMessage : IQueueMessage
{
    public static string DefaultQueueName => "user-notifications";

    public int UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
}

// Use type-safe extension to get queue client
var queue = queueServiceClient.GetQueueClient<TenantVerificationMessage>();
await queue.SendMessageAsync(JsonSerializer.Serialize(new TenantVerificationMessage
{
    TenantId = 123,
    Email = "admin@example.com"
}));

// Receive messages
var messages = await queue.ReceiveMessagesAsync(maxMessages: 10);
foreach (var message in messages.Value)
{
    var msg = JsonSerializer.Deserialize<TenantVerificationMessage>(message.MessageText);
    // Process message
    await queue.DeleteMessageAsync(message.MessageId, message.PopReceipt);
}
```

### Type-Safe Blob Container Access

Define blob container types and access containers without hardcoded names:

```csharp
using Imagile.Framework.Storage.Interfaces;
using Imagile.Framework.Storage.Extensions;

// Define blob container types
public class TenantDocumentContainer : IBlobContainer
{
    public static string DefaultContainerName => "tenant-documents";
}

public class UserAvatarContainer : IBlobContainer
{
    public static string DefaultContainerName => "user-avatars";
}

// Use type-safe extension to get container client
var container = blobServiceClient.GetBlobContainerClient<TenantDocumentContainer>();
var blob = container.GetBlobClient($"tenants/{tenantId}/invoice-{invoiceId}.pdf");

// Upload document
using var fileStream = File.OpenRead("invoice.pdf");
await blob.UploadAsync(fileStream, overwrite: true);

// Download document
var downloadResult = await blob.OpenReadAsync();
using var memoryStream = new MemoryStream();
await downloadResult.CopyToAsync(memoryStream);

// List blobs in container
await foreach (var blobItem in container.GetBlobsAsync(prefix: $"tenants/{tenantId}/"))
{
    Console.WriteLine($"Found: {blobItem.Name}");
}
```

### Multi-Storage-Account Support with StorageAccountAttribute

Use multiple Azure Storage accounts in a single application:

```csharp
using Imagile.Framework.Storage.Attributes;
using Imagile.Framework.Storage.Interfaces;

// Primary storage account (default - no attribute)
public class UserNotificationMessage : IQueueMessage
{
    public static string DefaultQueueName => "user-notifications";
    public int UserId { get; set; }
}

// Archive storage account (named "archive")
[StorageAccount("archive")]
public class AuditLogContainer : IBlobContainer
{
    public static string DefaultContainerName => "audit-logs";
}

// Long-term storage account (named "longterm")
[StorageAccount("longterm")]
public class BackupContainer : IBlobContainer
{
    public static string DefaultContainerName => "backups";
}

// DI registration in Program.cs
builder.Services.AddStorageAbstractions(config =>
{
    // Default (unnamed) storage account
    config.AddStorageAccount(builder.Configuration.GetConnectionString("AzureStorage")!);

    // Named storage accounts
    config.AddStorageAccount("archive", builder.Configuration.GetConnectionString("ArchiveStorage")!);
    config.AddStorageAccount("longterm", builder.Configuration.GetConnectionString("LongTermStorage")!);

    // Scan assemblies for IQueueMessage and IBlobContainer types
    config.ScanAssemblies(typeof(UserNotificationMessage).Assembly);
});

// Automatic resource initialization at startup
var app = builder.Build();
await app.Services.InitializeStorageResourcesAsync();
```

### Automatic Queue and Container Creation

Initialize all queues and containers automatically at application startup:

```csharp
using Imagile.Framework.Storage.Initialization;

// In Program.cs after building the app
var app = builder.Build();

// Initialize all discovered storage resources
var result = await app.Services.InitializeStorageResourcesAsync();

// Log initialization results
Console.WriteLine($"Queues created: {result.QueuesCreated.Count}");
Console.WriteLine($"Containers created: {result.ContainersCreated.Count}");

foreach (var queue in result.QueuesCreated)
{
    Console.WriteLine($"  - Queue: {queue}");
}

foreach (var container in result.ContainersCreated)
{
    Console.WriteLine($"  - Container: {container}");
}

if (result.Errors.Any())
{
    Console.WriteLine($"Errors: {result.Errors.Count}");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  - {error}");
    }
}

await app.RunAsync();
```

## API Reference

### Interfaces

- **IQueueMessage** - Marker interface for queue message types with static `DefaultQueueName` property
- **IBlobContainer** - Marker interface for blob container types with static `DefaultContainerName` property

### Attributes

- **StorageAccountAttribute** - Associates a queue/container type with a named storage account

### Extension Methods

- **GetQueueClient&lt;T&gt;()** - Get type-safe QueueClient for IQueueMessage type
- **GetBlobContainerClient&lt;T&gt;()** - Get type-safe BlobContainerClient for IBlobContainer type
- **AddStorageAbstractions()** - Configure storage accounts and assembly scanning
- **InitializeStorageResourcesAsync()** - Create all discovered queues and containers

### DI Configuration

- **StorageBuilder.AddStorageAccount()** - Register default or named storage account
- **StorageBuilder.ScanAssemblies()** - Scan assemblies for IQueueMessage and IBlobContainer types

## Dependencies

Requires **Imagile.Framework.Configuration** package.

## License

MIT License - see LICENSE file for details.

## Contributing

Contributions are welcome! Please open an issue or pull request on GitHub.

## Repository

https://github.com/imagile/imagile-framework
