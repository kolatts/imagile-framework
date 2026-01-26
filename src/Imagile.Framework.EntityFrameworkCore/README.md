# Imagile.Framework.EntityFrameworkCore

EF Core audit logging with automatic timestamps, user tracking, property-level change tracking, and soft delete support. Provides opinionated interfaces and base DbContext for building auditable applications.

## Installation

```bash
dotnet add package Imagile.Framework.EntityFrameworkCore
```

## Features

- **Automatic Timestamps** - ITimestampedEntity interface with CreatedOn/ModifiedOn tracking
- **User Tracking** - IAuditableEntity interface for CreatedBy/ModifiedBy/DeletedBy
- **Property-Level Change Tracking** - IEntityChangeAuditable for detailed audit trails (old value → new value)
- **Soft Delete Support** - IsDeleted, DeletedOn, DeletedBy with automatic population
- **Tenant Isolation** - ITenantEntity interface for multi-tenant applications
- **ImagileDbContext Base Class** - Automatic audit processing in SaveChanges override

## Usage Examples

### Basic Timestamp Tracking

Use `ITimestampedEntity` for simple created/modified timestamp tracking:

```csharp
using Imagile.Framework.EntityFrameworkCore.Interfaces;

public class BlogPost : ITimestampedEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    // ITimestampedEntity - automatically managed
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
}

public class MyDbContext : ImagileDbContext<int, int>
{
    public MyDbContext(
        DbContextOptions<MyDbContext> options,
        IAuditContextProvider<int, int> auditContext)
        : base(options, auditContext)
    {
    }

    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
}

// Usage - timestamps are automatic
var post = new BlogPost { Title = "Hello World", Content = "..." };
context.BlogPosts.Add(post);
await context.SaveChangesAsync();
// post.CreatedOn and post.ModifiedOn now populated with UTC timestamp
```

### Full Audit with User Tracking and Soft Delete

Use `IAuditableEntity<TUserKey>` for comprehensive audit trails including who made changes:

```csharp
using Imagile.Framework.EntityFrameworkCore.Interfaces;

public class Customer : IAuditableEntity<int>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // ITimestampedEntity
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }

    // IAuditableEntity - user tracking
    public int? CreatedBy { get; set; }
    public int? ModifiedBy { get; set; }

    // IAuditableEntity - soft delete
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
    public int? DeletedBy { get; set; }
}

// Implement audit context provider (from HttpContext, claims, etc.)
public class HttpAuditContextProvider : IAuditContextProvider<int, int>
{
    private readonly IHttpContextAccessor _httpContext;

    public HttpAuditContextProvider(IHttpContextAccessor httpContext)
    {
        _httpContext = httpContext;
    }

    public int? UserId => GetUserIdFromClaims();
    public int? TenantId => GetTenantIdFromClaims();

    private int? GetUserIdFromClaims()
    {
        var claim = _httpContext.HttpContext?.User?.FindFirst("sub");
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    private int? GetTenantIdFromClaims()
    {
        var claim = _httpContext.HttpContext?.User?.FindFirst("tenant_id");
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }
}

// Register in DI
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditContextProvider<int, int>, HttpAuditContextProvider>();

// Usage - all audit fields populated automatically
var customer = new Customer { Name = "Acme Corp", Email = "contact@acme.com" };
context.Customers.Add(customer);
await context.SaveChangesAsync();
// customer.CreatedBy now contains current user ID from HttpContext

// Soft delete - automatically populates DeletedOn and DeletedBy
customer.IsDeleted = true;
await context.SaveChangesAsync();
// customer.DeletedOn and customer.DeletedBy now populated

// Apply global query filter to exclude soft-deleted entities
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Customer>().HasQueryFilter(c => !c.IsDeleted);
    base.OnModelCreating(modelBuilder);
}
```

### Property-Level Change Tracking

Use `IEntityChangeAuditable<TUserKey>` for detailed audit trails recording old value → new value changes:

```csharp
using Imagile.Framework.EntityFrameworkCore.Interfaces;
using Imagile.Framework.EntityFrameworkCore.Attributes;

public class Invoice : IEntityChangeAuditable<int>
{
    public int Id { get; set; }

    [Auditable]
    public decimal Amount { get; set; }

    [Auditable]
    public string Status { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty; // NOT audited (no [Auditable])

    // IEntityChangeAuditable implementation
    public int? ItemId => Id == 0 ? null : Id;
    public string? ParentEntityName => null;
    public int? ParentItemId => null;
    public string? EntityChangeDescription => $"Invoice #{Id}";

    // IAuditableEntity properties
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public int? CreatedBy { get; set; }
    public int? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
    public int? DeletedBy { get; set; }
}

// Configure your DbContext to include EntityChange and EntityChangeProperty
public class MyDbContext : ImagileDbContext<int, int>
{
    public MyDbContext(
        DbContextOptions<MyDbContext> options,
        IAuditContextProvider<int, int> auditContext)
        : base(options, auditContext)
    {
    }

    public DbSet<Invoice> Invoices => Set<Invoice>();

    // Required for property-level change tracking
    public DbSet<EntityChange> EntityChanges => Set<EntityChange>();
    public DbSet<EntityChangeProperty> EntityChangeProperties => Set<EntityChangeProperty>();
}

// Usage - property changes automatically tracked
var invoice = context.Invoices.Find(123);
invoice.Amount = 1500.00m; // Changed from 1000.00
invoice.Status = "Approved"; // Changed from "Pending"
invoice.Notes = "Updated notes"; // NOT tracked (no [Auditable])
await context.SaveChangesAsync();

// EntityChange record created with:
// - EntityName: "Invoice"
// - ItemId: 123
// - Description: "Invoice #123"
// - TransactionUnique: [generated GUID]
// - ModifiedBy: [current user ID]
// - ModifiedOn: [current UTC timestamp]

// EntityChangeProperty records created:
// - PropertyName: "Amount", OldValue: "1000.00", NewValue: "1500.00"
// - PropertyName: "Status", OldValue: "Pending", NewValue: "Approved"
// (Notes change NOT recorded - no [Auditable] attribute)
```

## Key Types

### Interfaces

- **ITimestampedEntity** - Basic timestamp tracking (CreatedOn, ModifiedOn)
- **IAuditableEntity<TUserKey>** - Full audit with user tracking and soft delete
- **IEntityChangeAuditable<TUserKey>** - Property-level change tracking
- **ITenantEntity<TTenantKey>** - Multi-tenant isolation
- **IAuditContextProvider<TUserKey, TTenantKey>** - Provides current user/tenant IDs

### Base Classes

- **ImagileDbContext<TUserKey, TTenantKey>** - Base DbContext with automatic audit processing in SaveChanges

### Entities

- **EntityChange** - Tracks entity-level changes (what entity, when, by whom)
- **EntityChangeProperty** - Tracks property-level changes (old value → new value)

### Attributes

- **[Auditable]** - Mark properties for detailed change tracking (used with IEntityChangeAuditable)

## Dependencies

Requires **Imagile.Framework.Core** package.

## License

MIT License - see LICENSE file for details.

## Contributing

Contributions are welcome! Please open an issue or pull request on GitHub.

## Repository

https://github.com/imagile/imagile-framework
