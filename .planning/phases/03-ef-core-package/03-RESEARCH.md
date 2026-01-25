# Phase 3: EF Core Package - Research

**Researched:** 2026-01-25
**Domain:** EF Core audit logging with automatic change tracking, soft delete, and multi-tenancy
**Confidence:** HIGH

## Summary

This research investigated EF Core 10.0 audit logging patterns, focusing heavily on the Arcoro.One repository implementation which provides a proven production-ready pattern for property-level change tracking. The standard approach uses SaveChanges override (not interceptors) to create EntityChange and EntityChangeProperty records, with opt-in via interface implementation and attribute-based property selection.

**Key architectural discoveries from Arcoro.One:**
- SaveChanges override pattern (not ISaveChangesInterceptor) for capturing changes before database write
- Two-table design: EntityChange (header) + EntityChangeProperty (property-level details)
- Transaction-scoped grouping via TransactionUnique (Guid) to group related changes
- Attribute-based opt-in ([EntityChangeAuditable]) for property-level tracking
- Temporary value handling for auto-generated primary keys (ItemId populated after SaveChanges)
- Parent/child relationship tracking via ParentEntityName and ParentItemId
- Soft delete detection integrated into change tracking (IsDeleted property monitoring)

**EF Core 10.0 enhancements critical for this phase:**
- Named query filters enable separate control of soft delete vs. tenant filtering
- Change tracker generics (Entries<TEntity>()) work with interfaces for clean filtering
- DateTimeOffset with configurable precision (0-7) for consistent timezone handling

**Primary recommendation:** Use SaveChanges override pattern (proven in Arcoro.One) over SaveChangesInterceptor for audit logging, as it provides simpler state management, easier testing, and avoids interceptor lifecycle complexity. Implement three-level interface hierarchy with attribute-based property opt-in.

## Standard Stack

The established libraries/tools for EF Core audit logging:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.EntityFrameworkCore | 10.0.x | Core EF framework | Official ORM, named query filters, change tracking |
| Microsoft.EntityFrameworkCore.Relational | 10.0.x | Relational database support | Required for fluent API, metadata access |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.ComponentModel.Annotations | Latest | Data annotations attributes | Creating custom attributes like [IgnoreAudit] |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Custom SaveChanges override | ISaveChangesInterceptor | Interceptor adds complexity (state management, lifecycle), SaveChanges override simpler for audit use case |
| Custom audit implementation | Audit.EntityFramework.Core | Third-party library (32.0.0) feature-rich but adds dependency, less control over schema |
| Shadow properties | Explicit properties on interfaces | Shadow properties hide from domain but harder to test, interface properties explicit and testable |

**Installation:**
```bash
# Core package only needs EF Core references
dotnet add package Microsoft.EntityFrameworkCore --version 10.0.0
dotnet add package Microsoft.EntityFrameworkCore.Relational --version 10.0.0
```

## Architecture Patterns

### Recommended Project Structure
```
src/Imagile.Framework.EntityFrameworkCore/
├── Interfaces/                    # Public contracts
│   ├── ITimestampedEntity.cs      # Base: CreatedOn, ModifiedOn
│   ├── IAuditableEntity.cs        # Adds: CreatedBy, ModifiedBy, soft delete
│   ├── IEntityChangeAuditable.cs  # Adds: property-level change tracking
│   ├── ITenantEntity.cs           # Separate: TenantId
│   └── IAuditContextProvider.cs   # User/tenant context abstraction
├── Attributes/
│   ├── AuditableAttribute.cs      # Mark properties for change tracking
│   └── IgnoreAuditAttribute.cs    # Exclude specific properties
├── Entities/
│   ├── EntityChange.cs            # Change header (who, when, what entity)
│   └── EntityChangeProperty.cs    # Property-level changes (old/new values)
├── DbContext/
│   └── ImagileDbContext.cs        # Base class with audit logic in SaveChanges override
└── Extensions/
    └── AuditExtensions.cs         # Helper methods (Restore(), GetChangeHistory())
```

### Pattern 1: Interface Hierarchy with Generic Type Parameters

**What:** Three-level interface hierarchy where each level adds more audit capability, with generic TUserKey and TTenantKey for flexibility.

**When to use:** All audit scenarios - developers choose interface level based on needs.

**Example:**
```csharp
// Source: Arcoro.One pattern adapted for generic types
public interface ITimestampedEntity<TTenantKey>
{
    DateTimeOffset CreatedOn { get; set; }
    DateTimeOffset ModifiedOn { get; set; }
}

public interface IAuditableEntity<TUserKey, TTenantKey> : ITimestampedEntity<TTenantKey>
{
    TUserKey? CreatedBy { get; set; }
    TUserKey? ModifiedBy { get; set; }
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedOn { get; set; }
    TUserKey? DeletedBy { get; set; }
}

public interface IEntityChangeAuditable<TUserKey, TTenantKey> : IAuditableEntity<TUserKey, TTenantKey>
{
    int? ItemId { get; }  // Primary key for EntityChange tracking
    string? ParentEntityName { get; }
    int? ParentItemId { get; }
    string? EntityChangeDescription { get; }
}
```

### Pattern 2: SaveChanges Override (Not Interceptor)

**What:** Override SaveChanges/SaveChangesAsync in base DbContext to capture changes before database write.

**When to use:** Always for audit logging - simpler than interceptors, easier state management.

**Example:**
```csharp
// Source: Arcoro.One CompanyDbContext pattern
public abstract class ImagileDbContext<TUserKey, TTenantKey> : DbContext
{
    protected IAuditContextProvider<TUserKey, TTenantKey> AuditContext { get; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var transactionUnique = Guid.NewGuid();

        // 1. Capture entity changes BEFORE SaveChanges
        var entityChanges = GetEntityChangesBeforeSave(transactionUnique);

        // 2. Populate timestamp and user fields
        PopulateAuditFields();

        // 3. Call base SaveChanges (writes entities to DB)
        var result = await base.SaveChangesAsync(cancellationToken);

        // 4. Now ItemIds are populated, save EntityChange records
        await SaveEntityChangesAfterSave(entityChanges);

        return result;
    }

    protected virtual List<(EntityChange Change, IEntityChangeAuditable Entity)>
        GetEntityChangesBeforeSave(Guid transactionUnique)
    {
        return ChangeTracker
            .Entries<IEntityChangeAuditable<TUserKey, TTenantKey>>()
            .Where(x => ShouldCreateEntityChange(x))
            .Select(entry => (
                new EntityChange(transactionUnique, entry, AuditContext),
                entry.Entity
            ))
            .ToList();
    }
}
```

### Pattern 3: Attribute-Based Property Tracking

**What:** Use custom [Auditable] attribute to mark which properties should have change history recorded.

**When to use:** IEntityChangeAuditable entities - selective property tracking.

**Example:**
```csharp
// Source: Arcoro.One EntityChangeAuditableAttribute pattern
[AttributeUsage(AttributeTargets.Property)]
public class AuditableAttribute : Attribute
{
    public bool HideValueChanges { get; }

    public AuditableAttribute(bool hideValueChanges = false)
    {
        HideValueChanges = hideValueChanges;
    }
}

public class Person : IAuditableEntity<int, int>
{
    [Auditable]
    public string FirstName { get; set; }

    [Auditable]
    public string LastName { get; set; }

    [Auditable(hideValueChanges: true)]  // Track changes but hide values
    public string PasswordHash { get; set; }

    public string InternalNotes { get; set; }  // No attribute = not tracked
}
```

### Pattern 4: Named Query Filters (EF Core 10.0)

**What:** Define multiple named filters per entity, enable/disable independently.

**When to use:** Multi-tenant applications with soft delete - control filters separately.

**Example:**
```csharp
// Source: EF Core 10.0 named query filters pattern
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Multiple named filters on same entity
    modelBuilder.Entity<Invoice>()
        .HasQueryFilter("SoftDelete", i => !i.IsDeleted)
        .HasQueryFilter("Tenant", i => i.TenantId == _auditContext.TenantId);

    // Can disable individually
    // context.Invoices.IgnoreQueryFilters("Tenant")  // See all tenants, still filter soft-deleted
    // context.Invoices.IgnoreQueryFilters()          // See everything (security risk!)
}
```

### Pattern 5: Temporary Value Handling

**What:** Handle auto-generated primary keys that aren't available until after SaveChanges.

**When to use:** EntityChange records that need to reference the entity's primary key.

**Example:**
```csharp
// Source: Arcoro.One EntityChange constructor pattern
public class EntityChange
{
    public EntityChange(Guid transactionUnique, EntityEntry<IEntityChangeAuditable> entry,
        IAuditContextProvider auditContext)
    {
        TransactionUnique = transactionUnique;

        if (entry.State == EntityState.Added)
        {
            // Primary key not yet generated - save temporary properties
            TemporaryProperties = entry.Properties.Where(x => x.IsTemporary).ToList();
        }
        else
        {
            // Key already exists
            ItemId = (int?)entry.Properties.Single(x => x.Metadata.IsPrimaryKey()).CurrentValue;
        }

        // Capture property changes
        EntityChangeFields = entry.Properties
            .Where(x => x.IsModified && HasAuditableAttribute(x.Metadata.PropertyInfo))
            .Select(x => new EntityChangeProperty
            {
                PropertyName = x.Metadata.PropertyInfo.Name,
                OriginalValue = FormatValue(x.OriginalValue),
                NewValue = FormatValue(x.CurrentValue)
            })
            .ToList();
    }

    // After SaveChanges, populate ItemId from temporary values
    public void PopulateItemId()
    {
        if (TemporaryProperties.Any())
        {
            var keyProperty = TemporaryProperties.Single(x => x.Metadata.IsPrimaryKey());
            ItemId = (int?)keyProperty.CurrentValue;
        }
    }

    [NotMapped]
    public List<PropertyEntry> TemporaryProperties { get; set; } = new();
}
```

### Anti-Patterns to Avoid

- **Using ISaveChangesInterceptor for audit logging:** Adds complexity (state management, lifecycle), interceptors better for cross-cutting logging/telemetry not requiring state
- **Tracking all properties without opt-in:** Creates noise, storage bloat; use attribute-based opt-in
- **Storing audit data as JSON:** Makes querying impossible; use separate EntityChangeProperty table
- **Shadow properties for audit fields:** Harder to test, less discoverable; explicit interface properties better for domain contracts
- **Single query filter combining soft delete + tenant:** EF Core 10.0 named filters allow independent control
- **Calling IgnoreQueryFilters() without careful thought:** Security risk in multi-tenant apps, can leak data across tenants

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Full audit framework | Custom audit logging from scratch | Audit.EntityFramework.Core 32.0+ | Production-tested, handles edge cases (owned entities, many-to-many, value objects) |
| Change value formatting | String.Format() on property values | DisplayFormatAttribute + reflection | Handles dates, decimals, enums consistently; extensible |
| Soft delete queries | Manual .Where(x => !x.IsDeleted) | Named query filters | Automatic, impossible to forget, centralized |
| Tenant isolation | Manual .Where(x => x.TenantId == current) | Named query filters | Prevents accidental data leaks, enforced at model level |
| Transaction grouping | DateTime-based grouping | Guid.NewGuid() per SaveChanges | Avoids clock skew issues, truly unique, thread-safe |

**Key insight:** Audit logging has many edge cases (temporary values, navigation properties, owned entities, value conversions). If building custom, study Arcoro.One pattern closely or use Audit.NET library.

## Common Pitfalls

### Pitfall 1: Recursive SaveChanges Deadlock

**What goes wrong:** SaveChanges override adds EntityChange records to context and calls SaveChanges again, creating infinite recursion.

**Why it happens:** EntityChange entities trigger ChangeTracker entries, which trigger audit logic, which adds more entities...

**How to avoid:**
- Track EntityChanges in memory during first SaveChanges
- Only add them to DbSet AFTER base.SaveChanges() completes
- Disable audit tracking for EntityChange and EntityChangeProperty entity types

**Warning signs:** Stack overflow exception, SaveChanges never returning, context disposed errors.

**Example from Arcoro.One:**
```csharp
// CORRECT: Capture changes BEFORE save, persist AFTER save
var entityChanges = GetEntityChangesBeforeSave(transactionUnique);
var result = await base.SaveChangesAsync(cancellationToken);
await SaveEntityChangesAfterSave(entityChanges);  // Now safe to call SaveChanges again

protected virtual async Task SaveEntityChangesAfterSave(
    List<(EntityChange Change, IEntityChangeAuditable Entity)> entityChanges)
{
    if (!entityChanges.Any()) return;

    // Populate ItemIds now that entities are saved
    foreach (var (change, entity) in entityChanges)
    {
        change.ItemId = entity.ItemId;
    }

    EntityChanges.AddRange(entityChanges.Select(x => x.Change));
    await base.SaveChangesAsync();  // Second save for audit records only
}
```

### Pitfall 2: IgnoreQueryFilters Security Hole

**What goes wrong:** Developer calls `.IgnoreQueryFilters()` to see soft-deleted records, accidentally exposes data from other tenants.

**Why it happens:** IgnoreQueryFilters disables ALL filters - both soft delete AND tenant isolation.

**How to avoid:**
- Use EF Core 10.0 named filters: `.IgnoreQueryFilters("SoftDelete")` (keeps tenant filter active)
- Code review any IgnoreQueryFilters usage
- Create wrapper method that only disables safe filters
- Document when cross-tenant access is intentional (admin screens, background jobs)

**Warning signs:** User reports seeing other tenant's data, security audit flags data access violations.

**Example:**
```csharp
// DANGEROUS - disables both soft delete AND tenant filter
var allInvoices = context.Invoices.IgnoreQueryFilters().ToList();  // SECURITY RISK!

// SAFE - named filters in EF Core 10.0
var deletedInvoices = context.Invoices
    .IgnoreQueryFilters("SoftDelete")  // Still respects tenant filter
    .ToList();
```

### Pitfall 3: DateTimeOffset Precision Mismatch

**What goes wrong:** DateTimeOffset values saved to database lose sub-second precision, causing audit timestamp comparisons to fail.

**Why it happens:** SQL Server DateTimeOffset default precision is 7, but some databases (or configurations) use lower precision (0-6).

**How to avoid:**
- Set precision explicitly in OnModelCreating: `SetDateTimeOffsetDefaultPrecision(0)` for second-level precision
- Use consistent precision across all DateTimeOffset columns
- Document precision choice (0 = seconds, 3 = milliseconds, 7 = 100 nanoseconds)
- Test timestamp equality with precision tolerance

**Warning signs:** Timestamp comparisons fail in tests, "CreatedOn != ModifiedOn" for unchanged entities.

**Example from Arcoro.One:**
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Set consistent precision for all DateTimeOffset columns
    modelBuilder.SetDateTimeOffsetDefaultPrecision(0);  // Second-level precision
}
```

### Pitfall 4: ChangeTracker.Entries() Mutation During Iteration

**What goes wrong:** InvalidOperationException when iterating ChangeTracker.Entries() and modifying entity state.

**Why it happens:** Modifying entities during iteration invalidates the enumerator.

**How to avoid:**
- Call `.ToList()` before iterating: `ChangeTracker.Entries<T>().ToList().ForEach(...)`
- Capture changes in memory before modifying state
- Avoid adding/removing entities during SaveChanges override

**Warning signs:** "Collection was modified; enumeration operation may not execute" exceptions.

**Example:**
```csharp
// WRONG - modifying during iteration
foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
{
    entry.Entity.ModifiedOn = DateTimeOffset.UtcNow;  // May cause InvalidOperationException
}

// CORRECT - materialize list first
var entries = ChangeTracker.Entries<IAuditableEntity>().ToList();
foreach (var entry in entries)
{
    entry.Entity.ModifiedOn = DateTimeOffset.UtcNow;
}
```

### Pitfall 5: Shadow Properties Testing Difficulty

**What goes wrong:** Unit tests can't easily set/verify shadow properties (CreatedOn, ModifiedOn) without full DbContext.

**Why it happens:** Shadow properties don't exist on entity classes, only in EF metadata.

**How to avoid:**
- Use explicit interface properties instead of shadow properties
- Shadow properties hide from domain but harder to test
- For this project: interfaces chosen over shadow properties per CONTEXT.md decisions

**Warning signs:** Tests require full database integration, can't test audit logic in isolation.

## Code Examples

Verified patterns from Arcoro.One and EF Core documentation:

### Capturing Property Changes with Reflection
```csharp
// Source: Arcoro.One EntityChange constructor
// Captures property-level changes using metadata and attributes
public EntityChange(Guid transactionUnique, EntityEntry<IEntityChangeAuditable> entry,
    ICurrentUserProvider currentUserProvider)
{
    TransactionUnique = transactionUnique;
    TableName = entry.Metadata.GetTableName();
    ItemName = entry.Metadata.ClrType.Name;
    OperationType = entry.State switch
    {
        EntityState.Added => EntityChangeOperation.Create,
        EntityState.Modified => EntityChangeOperation.Update,
        EntityState.Deleted => EntityChangeOperation.Delete,
        _ => throw new ArgumentException($"Cannot create entity change for {entry.State}")
    };

    // Capture properties marked with [EntityChangeAuditable] attribute
    EntityChangeFields = entry.Properties
        .Where(x =>
            (x.IsModified || OperationType == EntityChangeOperation.Create) &&
            !x.Metadata.IsPrimaryKey() &&
            x.Metadata.PropertyInfo?.GetCustomAttributes(typeof(EntityChangeAuditableAttribute), true).Length > 0
        )
        .Select(x => new EntityChangeProperty
        {
            PropertyName = x.Metadata.PropertyInfo?.Name ?? x.Metadata.GetColumnName(),
            ColumnName = x.Metadata.GetColumnName(),
            OriginalValue = FormatValue(x.OriginalValue, x.Metadata),
            NewValue = FormatValue(x.CurrentValue, x.Metadata),
            AreValueChangesHidden = x.Metadata.PropertyInfo!
                .GetCustomAttributes<EntityChangeAuditableAttribute>()
                .First().HideValueChanges
        })
        .ToList();
}

private static string? FormatValue(object? value, IPropertyBase metadata)
{
    if (value == null) return null;

    var propertyInfo = metadata.PropertyInfo;
    if (propertyInfo != null)
    {
        var format = propertyInfo.GetCustomAttributes<DisplayFormatAttribute>().FirstOrDefault();
        if (format?.DataFormatString != null)
        {
            return string.Format(CultureInfo.GetCultureInfo("en-US"), format.DataFormatString, value);
        }
    }

    // Handle Date columns specially
    if (metadata.GetColumnType() == "Date" && value is DateOnly dateValue)
    {
        return dateValue.ToString("d", CultureInfo.GetCultureInfo("en-US"));
    }

    return value.ToString();
}
```

### IAuditContextProvider Interface
```csharp
// Source: Adapted from Arcoro.One ICurrentUserProvider
// Generic interface for audit context (user, tenant, correlation)
public interface IAuditContextProvider<TUserKey, TTenantKey>
{
    TUserKey? UserId { get; }
    TTenantKey? TenantId { get; }
    Guid? CorrelationId { get; }
    bool IsValid { get; }
}

// Example implementation for ASP.NET Core with int keys
public class HttpContextAuditProvider : IAuditContextProvider<int, int>
{
    private readonly IHttpContextAccessor _httpContext;

    public int? UserId => _httpContext.HttpContext?.User.GetUserId();
    public int? TenantId => _httpContext.HttpContext?.User.GetTenantId();
    public Guid? CorrelationId => _httpContext.HttpContext?.TraceIdentifier != null
        ? Guid.Parse(_httpContext.HttpContext.TraceIdentifier)
        : null;
    public bool IsValid => UserId.HasValue && TenantId.HasValue;
}
```

### Soft Delete Detection in Change Tracking
```csharp
// Source: Arcoro.One EntityChange.CheckSoftDelete pattern
// Detects soft delete by monitoring IsDeleted property changes
private void CheckSoftDelete(EntityEntry<IEntityChangeAuditable> entry)
{
    if (entry.Entity is ISoftDeletableEntity softDeletableEntity)
    {
        var isDeletedChange = EntityChangeFields
            .FirstOrDefault(x => x.PropertyName == nameof(ISoftDeletableEntity.IsDeleted));

        if (isDeletedChange != null)
        {
            // Remove IsDeleted from tracked properties
            EntityChangeFields.Remove(isDeletedChange);

            // If IsDeleted changed from false to true, treat as Delete operation
            if (isDeletedChange.OriginalValue != isDeletedChange.NewValue)
            {
                OperationType = EntityChangeOperation.Delete;
            }
        }
    }
}
```

### Restore Soft-Deleted Entity
```csharp
// Extension method to un-delete soft-deleted entities
public static class SoftDeleteExtensions
{
    public static void Restore<TUserKey, TTenantKey>(
        this IAuditableEntity<TUserKey, TTenantKey> entity,
        IAuditContextProvider<TUserKey, TTenantKey> auditContext)
    {
        entity.IsDeleted = false;
        entity.DeletedOn = null;
        entity.DeletedBy = default;
        entity.ModifiedOn = DateTimeOffset.UtcNow;
        entity.ModifiedBy = auditContext.UserId;
    }
}
```

### Named Query Filters Configuration
```csharp
// Source: EF Core 10.0 named query filters
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Apply to all IAuditableEntity types
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (typeof(IAuditableEntity<,>).IsAssignableFrom(entityType.ClrType))
        {
            // Soft delete filter
            var isDeletedProperty = entityType.FindProperty(nameof(IAuditableEntity<int, int>.IsDeleted));
            if (isDeletedProperty != null)
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, isDeletedProperty.PropertyInfo);
                var condition = Expression.Not(property);
                var lambda = Expression.Lambda(condition, parameter);

                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter("SoftDelete", lambda);
            }
        }

        if (typeof(ITenantEntity<>).IsAssignableFrom(entityType.ClrType))
        {
            // Tenant filter - evaluated dynamically
            var tenantIdProperty = entityType.FindProperty(nameof(ITenantEntity<int>.TenantId));
            if (tenantIdProperty != null)
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, tenantIdProperty.PropertyInfo);
                var currentTenant = Expression.Property(
                    Expression.Constant(_auditContext),
                    nameof(IAuditContextProvider<int, int>.TenantId)
                );
                var condition = Expression.Equal(property, currentTenant);
                var lambda = Expression.Lambda(condition, parameter);

                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter("Tenant", lambda);
            }
        }
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Single query filter per entity | Named query filters | EF Core 10.0 (2024) | Can disable soft delete filter while keeping tenant filter active |
| ISaveChangesInterceptor for audit | SaveChanges override | EF Core 5.0+ | SaveChanges override simpler for stateful operations like audit logging |
| DateTime UTC | DateTimeOffset with precision | EF Core 3.0+ | Proper timezone handling, configurable precision (0-7) |
| Shadow properties for audit | Explicit interface properties | Current best practice | Better testability, clearer contracts |
| Reflection-heavy change detection | ChangeTracker.Entries<T>() with generics | EF Core 2.0+ | Strongly-typed, better performance |

**Deprecated/outdated:**
- **DbContext.SavingChanges event:** Limited, doesn't provide EntityEntry access; use SaveChanges override instead
- **Single combined query filter:** `HasQueryFilter(e => !e.IsDeleted && e.TenantId == current)` - use named filters in EF Core 10.0
- **DateTime for audit timestamps:** Use DateTimeOffset for timezone awareness
- **Property.CurrentValue.ToString() for all types:** Use DisplayFormatAttribute + reflection for consistent formatting

## Open Questions

Things that couldn't be fully resolved:

1. **Best cascade strategy for soft-deleted parent entities**
   - What we know: Arcoro.One uses ClientCascade for some relationships, allowing mixed hard/soft delete
   - What's unclear: Default behavior when developer doesn't specify (cascade soft delete or hard delete children?)
   - Recommendation: Make it configurable per relationship, provide clear documentation and examples for both strategies

2. **Correlation ID generation strategy**
   - What we know: TransactionUnique used in Arcoro.One, but correlation across multiple SaveChanges calls not explored
   - What's unclear: Should correlation ID span multiple SaveChanges (e.g., HTTP request scope) or be per-SaveChanges?
   - Recommendation: Support both - TransactionUnique per-save (Guid.NewGuid()), CorrelationId from IAuditContextProvider for request-level grouping

3. **EntityChange table partitioning/archival strategy**
   - What we know: EntityChange table grows indefinitely in Arcoro.One
   - What's unclear: Best practices for archival, partitioning, retention policies at package level
   - Recommendation: Document as concern, provide extension points for custom archival, don't build into package (varies by use case)

4. **Performance impact of change tracking on large batches**
   - What we know: Each entity creates EntityChange + multiple EntityChangeProperty records
   - What's unclear: Batch insert optimizations, bulk operations bypass
   - Recommendation: Provide opt-out mechanism (AuditableOperations list configurable), document bulk operation considerations

## Sources

### Primary (HIGH confidence)

**Arcoro.One Repository (LOCAL):**
- `C:\Code\Arcoro.One\arcoro-one\Arcoro.One.Data.Company\Interfaces\IEntityChangeAuditable.cs` - Interface design
- `C:\Code\Arcoro.One\arcoro-one\Arcoro.One.Data.Company\Entities\EntityChange.cs` - Change tracking constructor pattern
- `C:\Code\Arcoro.One\arcoro-one\Arcoro.One.Data.Company\Entities\EntityChangeProperty.cs` - Property-level storage
- `C:\Code\Arcoro.One\arcoro-one\Arcoro.One.Data.Company\CompanyDbContext.cs` - SaveChanges override pattern
- `C:\Code\Arcoro.One\arcoro-one\Arcoro.One.Data.Company\Attributes\EntityChangeAuditableAttribute.cs` - Attribute-based opt-in
- `C:\Code\Arcoro.One\arcoro-one\Arcoro.One.Data.Company.SqlServer\dbo\Tables\EntityChanges.sql` - Database schema
- `C:\Code\Arcoro.One\arcoro-one\Arcoro.One.Data.Company.SqlServer\dbo\Tables\EntityChangeProperties.sql` - Property table schema

**Microsoft Official Documentation:**
- [Interceptors - EF Core | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors) - ISaveChangesInterceptor API
- [Global Query Filters - EF Core | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/querying/filters) - Named query filters (EF Core 10.0)
- [Change Tracking - EF Core | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/change-tracking/) - ChangeTracker.Entries() usage
- [Accessing Tracked Entities - EF Core | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/change-tracking/entity-entries) - OriginalValue/CurrentValue access
- [Generated Values - EF Core | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/modeling/generated-properties) - Temporary values for auto-generated keys
- [Entity Properties - EF Core | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/modeling/entity-properties) - Attribute-based configuration

### Secondary (MEDIUM confidence)

- [Named Query Filters in EF 10: Multiple Filters per Entity | Medium](https://medium.com/@sangheraajit/named-query-filters-in-ef-10-multiple-filters-per-entity-796401825f6d) - EF Core 10.0 named filters feature
- [How To Use EF Core Interceptors | Milan Jovanovic](https://www.milanjovanovic.tech/blog/how-to-use-ef-core-interceptors) - Interceptor patterns and use cases
- [Implementing Audit Logs in EF Core Without Polluting Your Entities | Elmah.io](https://blog.elmah.io/implementing-audit-logs-in-ef-core-without-polluting-your-entities/) - Shadow properties vs explicit properties
- [Mastering Multi-Tenancy in .NET EF Core (Part 3 Security & Advanced Topics) | Medium](https://jordansrowles.medium.com/mastering-multi-tenancy-in-net-ef-core-part-3-security-advanced-topics-98bcd96900f5) - IgnoreQueryFilters security implications (Jan 2026)
- [Overriding SaveChanges in EF Core | Hashnode](https://mbarkt3sto.hashnode.dev/overriding-savechanges-in-ef-core) - SaveChanges override pattern

### Tertiary (LOW confidence - community resources)

- [EF Core Interceptors: SaveChangesInterceptor for Auditing Entities in .NET 8 Microservices | Medium](https://mehmetozkaya.medium.com/ef-core-interceptors-savechangesinterceptor-for-auditing-entities-in-net-8-microservices-6923190a03b9) - Alternative interceptor approach
- [Comprehensive Guide to Implementing Audit Logging in .NET with EF Core Interceptors | DEV Community](https://dev.to/hootanht/comprehensive-guide-to-implementing-audit-logging-in-net-with-ef-core-interceptors-1e83) - Recent (3 weeks ago) audit implementation guide
- [Audit.EntityFramework.Core | Learn Entity Framework Core](https://www.learnentityframeworkcore.com/extensions/audit-entityframework-core) - Third-party audit library option

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - EF Core 10.0 official packages, no third-party dependencies needed
- Architecture patterns: HIGH - Verified against Arcoro.One production code and Microsoft documentation
- Pitfalls: HIGH - Based on Arcoro.One implementation experience and official EF Core documentation warnings
- Code examples: HIGH - Adapted from working Arcoro.One code and official Microsoft examples
- Named query filters: MEDIUM - New in EF Core 10.0, documented but less production usage visible
- Performance considerations: MEDIUM - Based on documentation and community resources, not load-tested

**Research date:** 2026-01-25
**Valid until:** ~30 days (EF Core stable, but package versions and documentation may update)

**Key decision:** SaveChanges override pattern (Arcoro.One proven approach) chosen over ISaveChangesInterceptor based on simpler state management, easier testing, and production validation. This aligns with user decision for "hybrid approach" in CONTEXT.md allowing both base class usage and manual opt-in.
