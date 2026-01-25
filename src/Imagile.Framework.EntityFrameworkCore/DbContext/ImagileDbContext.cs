using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using Imagile.Framework.Core.Interfaces;
using Imagile.Framework.EntityFrameworkCore.Attributes;
using Imagile.Framework.EntityFrameworkCore.Entities;
using Imagile.Framework.EntityFrameworkCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Imagile.Framework.EntityFrameworkCore.DbContext;

/// <summary>
/// Base DbContext with automatic audit logging for timestamps, user tracking, soft delete, and property-level change tracking.
/// </summary>
/// <typeparam name="TUserKey">The type of the user identifier (e.g., int, Guid, string)</typeparam>
/// <typeparam name="TTenantKey">The type of the tenant identifier (e.g., int, Guid, string)</typeparam>
/// <remarks>
/// <para>
/// Inherit from this class to get automatic audit functionality. The SaveChanges override handles:
/// - Timestamp population (CreatedOn, ModifiedOn)
/// - User tracking (CreatedBy, ModifiedBy, DeletedBy)
/// - Soft delete detection (IsDeleted, DeletedOn)
/// - Tenant assignment (TenantId)
/// - Property-level change tracking for IEntityChangeAuditable entities
/// </para>
/// <para>
/// The audit pattern follows Arcoro.One proven approach:
/// 1. Generate TransactionUnique and capture current timestamp
/// 2. BEFORE base.SaveChanges: Capture EntityChange records for IEntityChangeAuditable entities
/// 3. BEFORE base.SaveChanges: Populate audit fields on all tracked entities
/// 4. Call base.SaveChanges (writes entities, generates auto-increment keys)
/// 5. AFTER base.SaveChanges: Populate ItemIds and save EntityChange records
/// This prevents recursive SaveChanges deadlock while handling temporary primary key values.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MyDbContext : ImagileDbContext&lt;int, int&gt;
/// {
///     public MyDbContext(
///         DbContextOptions&lt;MyDbContext&gt; options,
///         IAuditContextProvider&lt;int, int&gt; auditContext)
///         : base(options, auditContext)
///     {
///     }
///
///     public DbSet&lt;Customer&gt; Customers => Set&lt;Customer&gt;();
///     public DbSet&lt;Invoice&gt; Invoices => Set&lt;Invoice&gt;();
/// }
/// </code>
/// </example>
public abstract class ImagileDbContext<TUserKey, TTenantKey> : Microsoft.EntityFrameworkCore.DbContext
{
    /// <summary>
    /// Constant value used to replace actual values in audit logs when HideValueChanges is true.
    /// </summary>
    private const string HiddenValuePlaceholder = "[HIDDEN]";

    /// <summary>
    /// Gets the audit context provider for accessing current user and tenant information.
    /// </summary>
    protected IAuditContextProvider<TUserKey, TTenantKey> AuditContext { get; }

    /// <summary>
    /// Gets or sets the EntityChange records for change tracking.
    /// </summary>
    public DbSet<EntityChange<TUserKey>> EntityChanges => Set<EntityChange<TUserKey>>();

    /// <summary>
    /// Gets or sets the EntityChangeProperty records for property-level change tracking.
    /// </summary>
    public DbSet<EntityChangeProperty> EntityChangeProperties => Set<EntityChangeProperty>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ImagileDbContext{TUserKey, TTenantKey}"/> class.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext</param>
    /// <param name="auditContext">The audit context provider for user and tenant information</param>
    protected ImagileDbContext(
        DbContextOptions options,
        IAuditContextProvider<TUserKey, TTenantKey> auditContext)
        : base(options)
    {
        AuditContext = auditContext ?? throw new ArgumentNullException(nameof(auditContext));
    }

    /// <summary>
    /// Saves all changes made in this context to the database with automatic audit logging.
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">
    /// Indicates whether Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AcceptAllChanges
    /// is called after the changes have been sent successfully to the database.
    /// </param>
    /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>The number of state entries written to the database.</returns>
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var transactionUnique = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // STEP 1: Capture entity changes BEFORE SaveChanges (while original values are available)
        var entityChangesToSave = CaptureEntityChanges(transactionUnique, now);

        // STEP 2: Populate audit fields for all entities
        PopulateAuditFields(now);

        // STEP 3: Call base SaveChanges (writes entities to DB, generates keys)
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

        // STEP 4: Save EntityChange records AFTER base SaveChanges (now ItemIds are available)
        await SaveEntityChangesAsync(entityChangesToSave, cancellationToken);

        return result;
    }

    /// <summary>
    /// Saves all changes made in this context to the database with automatic audit logging.
    /// </summary>
    /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>The number of state entries written to the database.</returns>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);
    }

    /// <summary>
    /// Saves all changes made in this context to the database with automatic audit logging.
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess">
    /// Indicates whether Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AcceptAllChanges
    /// is called after the changes have been sent successfully to the database.
    /// </param>
    /// <returns>The number of state entries written to the database.</returns>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        // Use async version internally to avoid code duplication
        return SaveChangesAsync(acceptAllChangesOnSuccess).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Saves all changes made in this context to the database with automatic audit logging.
    /// </summary>
    /// <returns>The number of state entries written to the database.</returns>
    public override int SaveChanges()
    {
        return SaveChanges(acceptAllChangesOnSuccess: true);
    }

    /// <summary>
    /// Captures entity changes for IEntityChangeAuditable entities before SaveChanges.
    /// </summary>
    /// <param name="transactionUnique">Unique identifier for this SaveChanges transaction</param>
    /// <param name="now">Current timestamp</param>
    /// <returns>List of EntityChange records paired with their temporary properties</returns>
    protected virtual List<(EntityChange<TUserKey> Change, List<PropertyEntry> TemporaryProperties)> CaptureEntityChanges(
        Guid transactionUnique,
        DateTimeOffset now)
    {
        var entityChanges = new List<(EntityChange<TUserKey>, List<PropertyEntry>)>();

        // Get all entries that implement IEntityChangeAuditable and are in a trackable state
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IEntityChangeAuditable<TUserKey> &&
                        (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToList(); // Materialize to avoid collection modified exception

        foreach (var entry in entries)
        {
            var auditableEntity = (IEntityChangeAuditable<TUserKey>)entry.Entity;
            var entityType = entry.Metadata;

            // Determine operation type
            var operation = DetermineOperation(entry);

            // Create EntityChange record
            var entityChange = new EntityChange<TUserKey>
            {
                TransactionUnique = transactionUnique,
                CorrelationId = AuditContext.CorrelationId,
                TableName = entityType.GetTableName() ?? entityType.ClrType.Name,
                EntityName = entityType.ClrType.Name,
                Operation = operation,
                ChangedOn = now,
                ChangedBy = AuditContext.UserId,
                Description = auditableEntity.EntityChangeDescription,
                ParentEntityName = auditableEntity.ParentEntityName,
                ParentItemId = auditableEntity.ParentItemId
            };

            // Capture temporary properties for auto-generated keys
            var temporaryProperties = new List<PropertyEntry>();
            if (entry.State == EntityState.Added)
            {
                temporaryProperties = entry.Properties.Where(p => p.IsTemporary).ToList();
            }
            else
            {
                // For Modified/Deleted, ItemId is already available
                entityChange.ItemId = auditableEntity.ItemId;
            }

            // Capture property-level changes
            entityChange.Properties = CapturePropertyChanges(entry, operation).ToList();

            entityChanges.Add((entityChange, temporaryProperties));
        }

        return entityChanges;
    }

    /// <summary>
    /// Determines the operation type for an entity entry, handling soft delete detection.
    /// </summary>
    /// <param name="entry">The entity entry</param>
    /// <returns>The operation type</returns>
    protected virtual EntityChangeOperation DetermineOperation(EntityEntry entry)
    {
        if (entry.State == EntityState.Added)
        {
            return EntityChangeOperation.Create;
        }

        if (entry.State == EntityState.Deleted)
        {
            return EntityChangeOperation.Delete;
        }

        // Check for soft delete transition (IsDeleted changed from false to true)
        if (entry.Entity is IAuditableEntity<TUserKey> auditableEntity)
        {
            var isDeletedProperty = entry.Property(nameof(IAuditableEntity<TUserKey>.IsDeleted));
            if (isDeletedProperty.IsModified)
            {
                var originalValue = isDeletedProperty.OriginalValue as bool? ?? false;
                var currentValue = isDeletedProperty.CurrentValue as bool? ?? false;

                if (!originalValue && currentValue)
                {
                    return EntityChangeOperation.Delete; // Soft delete
                }
            }
        }

        return EntityChangeOperation.Update;
    }

    /// <summary>
    /// Captures property-level changes for properties marked with [Auditable] attribute.
    /// </summary>
    /// <param name="entry">The entity entry</param>
    /// <param name="operation">The operation type</param>
    /// <returns>Collection of EntityChangeProperty records</returns>
    protected virtual IEnumerable<EntityChangeProperty> CapturePropertyChanges(
        EntityEntry entry,
        EntityChangeOperation operation)
    {
        var properties = new List<EntityChangeProperty>();

        foreach (var property in entry.Properties)
        {
            // Skip primary keys
            if (property.Metadata.IsPrimaryKey())
            {
                continue;
            }

            // Only track properties with [Auditable] attribute
            var propertyInfo = property.Metadata.PropertyInfo;
            if (propertyInfo == null)
            {
                continue;
            }

            var auditableAttribute = propertyInfo.GetCustomAttribute<AuditableAttribute>();
            if (auditableAttribute == null)
            {
                continue;
            }

            // For Update operations, only track modified properties
            if (operation == EntityChangeOperation.Update && !property.IsModified)
            {
                continue;
            }

            // Create property change record
            var propertyChange = new EntityChangeProperty
            {
                PropertyName = propertyInfo.Name,
                ColumnName = property.Metadata.GetColumnName(),
                AreValuesHidden = auditableAttribute.HideValueChanges
            };

            // Format values (or hide them if requested)
            if (auditableAttribute.HideValueChanges)
            {
                propertyChange.OriginalValue = operation == EntityChangeOperation.Create ? null : HiddenValuePlaceholder;
                propertyChange.NewValue = operation == EntityChangeOperation.Delete ? null : HiddenValuePlaceholder;
            }
            else
            {
                propertyChange.OriginalValue = operation == EntityChangeOperation.Create
                    ? null
                    : FormatValue(property.OriginalValue, property.Metadata);

                propertyChange.NewValue = operation == EntityChangeOperation.Delete
                    ? null
                    : FormatValue(property.CurrentValue, property.Metadata);
            }

            properties.Add(propertyChange);
        }

        return properties;
    }

    /// <summary>
    /// Formats a property value as a string for audit logging.
    /// </summary>
    /// <param name="value">The value to format</param>
    /// <param name="metadata">Property metadata</param>
    /// <returns>Formatted string representation</returns>
    protected virtual string? FormatValue(object? value, IPropertyBase metadata)
    {
        if (value == null)
        {
            return null;
        }

        // Check for DisplayFormatAttribute
        var propertyInfo = metadata.PropertyInfo;
        if (propertyInfo != null)
        {
            var displayFormat = propertyInfo.GetCustomAttribute<DisplayFormatAttribute>();
            if (displayFormat?.DataFormatString != null)
            {
                try
                {
                    return string.Format(CultureInfo.InvariantCulture, displayFormat.DataFormatString, value);
                }
                catch
                {
                    // Fall through to default formatting if format string is invalid
                }
            }
        }

        // Handle common types with consistent formatting
        return value switch
        {
            DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture), // ISO 8601
            DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture), // ISO 8601
            DateOnly dateOnly => dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeOnly timeOnly => timeOnly.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            decimal dec => dec.ToString(CultureInfo.InvariantCulture),
            double dbl => dbl.ToString(CultureInfo.InvariantCulture),
            float flt => flt.ToString(CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }

    /// <summary>
    /// Populates audit fields (timestamps, user IDs, tenant IDs) on all tracked entities.
    /// </summary>
    /// <param name="now">Current timestamp</param>
    protected virtual void PopulateAuditFields(DateTimeOffset now)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList(); // Materialize to avoid collection modified exception

        foreach (var entry in entries)
        {
            var entity = entry.Entity;

            // Handle ITimestampedEntity
            if (entity is ITimestampedEntity timestampedEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    timestampedEntity.CreatedOn = now;
                    timestampedEntity.ModifiedOn = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    timestampedEntity.ModifiedOn = now;
                }
            }

            // Handle IAuditableEntity (user tracking and soft delete)
            if (IsAuditableEntity(entity, out var auditableEntity))
            {
                if (entry.State == EntityState.Added)
                {
                    if (AuditContext.IsAuthenticated)
                    {
                        SetCreatedBy(auditableEntity, AuditContext.UserId);
                        SetModifiedBy(auditableEntity, AuditContext.UserId);
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    if (AuditContext.IsAuthenticated)
                    {
                        SetModifiedBy(auditableEntity, AuditContext.UserId);
                    }

                    // Check for soft delete transition
                    var isDeletedProperty = entry.Property(nameof(IAuditableEntity<TUserKey>.IsDeleted));
                    if (isDeletedProperty.IsModified)
                    {
                        var originalValue = isDeletedProperty.OriginalValue as bool? ?? false;
                        var currentValue = isDeletedProperty.CurrentValue as bool? ?? false;

                        if (!originalValue && currentValue)
                        {
                            // Soft delete transition - populate DeletedOn and DeletedBy
                            SetDeletedOn(auditableEntity, now);
                            if (AuditContext.IsAuthenticated)
                            {
                                SetDeletedBy(auditableEntity, AuditContext.UserId);
                            }
                        }
                        else if (originalValue && !currentValue)
                        {
                            // Restore transition - clear DeletedOn and DeletedBy
                            SetDeletedOn(auditableEntity, null);
                            SetDeletedBy(auditableEntity, default);
                        }
                    }
                }
            }

            // Handle ITenantEntity
            if (IsTenantEntity(entity, out var tenantEntity))
            {
                if (entry.State == EntityState.Added && AuditContext.TenantId != null)
                {
                    SetTenantId(tenantEntity, AuditContext.TenantId);
                }
            }
        }
    }

    /// <summary>
    /// Saves EntityChange records after base SaveChanges completes (when ItemIds are available).
    /// </summary>
    /// <param name="entityChanges">Captured entity changes with temporary properties</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected virtual async Task SaveEntityChangesAsync(
        List<(EntityChange<TUserKey> Change, List<PropertyEntry> TemporaryProperties)> entityChanges,
        CancellationToken cancellationToken)
    {
        if (!entityChanges.Any())
        {
            return;
        }

        // Populate ItemIds from temporary properties (auto-generated keys)
        foreach (var (change, temporaryProperties) in entityChanges)
        {
            if (temporaryProperties.Any())
            {
                var keyProperty = temporaryProperties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                if (keyProperty != null)
                {
                    change.ItemId = keyProperty.CurrentValue as int?;
                }
            }
        }

        // Add EntityChange records to context
        EntityChanges.AddRange(entityChanges.Select(x => x.Change));

        // Save EntityChange records (second SaveChanges call)
        await base.SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);
    }

    /// <summary>
    /// Configures the model for EntityChange tables and indexes.
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure EntityChange table
        modelBuilder.Entity<EntityChange<TUserKey>>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.TransactionUnique)
                .HasDatabaseName("IX_EntityChanges_TransactionUnique");

            entity.HasIndex(e => new { e.EntityName, e.ItemId })
                .HasDatabaseName("IX_EntityChanges_EntityName_ItemId");

            entity.HasIndex(e => e.ChangedOn)
                .HasDatabaseName("IX_EntityChanges_ChangedOn");

            // Configure relationship to Properties
            entity.HasMany(e => e.Properties)
                .WithOne()
                .HasForeignKey(p => p.EntityChangeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure EntityChangeProperty table
        modelBuilder.Entity<EntityChangeProperty>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.EntityChangeId)
                .HasDatabaseName("IX_EntityChangeProperties_EntityChangeId");
        });
    }

    #region Generic Interface Helpers

    /// <summary>
    /// Checks if an entity implements IAuditableEntity with generic type parameters matching this context.
    /// </summary>
    private bool IsAuditableEntity(object entity, out object auditableEntity)
    {
        var entityType = entity.GetType();
        var auditableInterface = entityType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType &&
                                 i.GetGenericTypeDefinition() == typeof(IAuditableEntity<>) &&
                                 i.GetGenericArguments()[0] == typeof(TUserKey));

        auditableEntity = auditableInterface != null ? entity : null!;
        return auditableInterface != null;
    }

    /// <summary>
    /// Checks if an entity implements ITenantEntity with generic type parameter matching this context.
    /// </summary>
    private bool IsTenantEntity(object entity, out object tenantEntity)
    {
        var entityType = entity.GetType();
        var tenantInterface = entityType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType &&
                                 i.GetGenericTypeDefinition() == typeof(ITenantEntity<>) &&
                                 i.GetGenericArguments()[0] == typeof(TTenantKey));

        tenantEntity = tenantInterface != null ? entity : null!;
        return tenantInterface != null;
    }

    /// <summary>
    /// Sets CreatedBy property using reflection to handle generic interface.
    /// </summary>
    private void SetCreatedBy(object entity, TUserKey? userId)
    {
        var property = entity.GetType().GetProperty(nameof(IAuditableEntity<TUserKey>.CreatedBy));
        property?.SetValue(entity, userId);
    }

    /// <summary>
    /// Sets ModifiedBy property using reflection to handle generic interface.
    /// </summary>
    private void SetModifiedBy(object entity, TUserKey? userId)
    {
        var property = entity.GetType().GetProperty(nameof(IAuditableEntity<TUserKey>.ModifiedBy));
        property?.SetValue(entity, userId);
    }

    /// <summary>
    /// Sets DeletedOn property using reflection to handle generic interface.
    /// </summary>
    private void SetDeletedOn(object entity, DateTimeOffset? deletedOn)
    {
        var property = entity.GetType().GetProperty(nameof(IAuditableEntity<TUserKey>.DeletedOn));
        property?.SetValue(entity, deletedOn);
    }

    /// <summary>
    /// Sets DeletedBy property using reflection to handle generic interface.
    /// </summary>
    private void SetDeletedBy(object entity, TUserKey? userId)
    {
        var property = entity.GetType().GetProperty(nameof(IAuditableEntity<TUserKey>.DeletedBy));
        property?.SetValue(entity, userId);
    }

    /// <summary>
    /// Sets TenantId property using reflection to handle generic interface.
    /// </summary>
    private void SetTenantId(object entity, TTenantKey? tenantId)
    {
        var property = entity.GetType().GetProperty(nameof(ITenantEntity<TTenantKey>.TenantId));
        property?.SetValue(entity, tenantId);
    }

    #endregion
}
