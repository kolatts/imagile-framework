# Imagile.Framework.Storage

Azure Storage abstractions for .NET applications.

## Features

- **IQueueMessage** - Type-safe queue message contracts with convention-based naming
- **IBlobContainer** - Type-safe blob container contracts with convention-based naming
- **StorageAccountAttribute** - Multi-storage-account support via attributes
- **Convention-based initialization** - Automatic queue/container creation at startup

## Usage

```csharp
// Define a queue message
public class TenantVerificationMessage : IQueueMessage
{
    public static string DefaultQueueName => "tenant-verification";

    public int TenantId { get; set; }
}

// Use type-safe extension
var queue = queueServiceClient.GetQueueClient<TenantVerificationMessage>();
await queue.SendMessageAsync(JsonSerializer.Serialize(message));
```
