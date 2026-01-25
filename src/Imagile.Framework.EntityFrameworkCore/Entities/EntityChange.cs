using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Imagile.Framework.EntityFrameworkCore.Entities;

/// <summary>
/// Records a change event for an entity. Serves as the header for property-level change details.
/// </summary>
/// <typeparam name="TUserKey">The type of the user identifier (e.g., int, Guid, string)</typeparam>
/// <remarks>
/// <para>
/// Each EntityChange represents a single entity being created, updated, or deleted within a SaveChanges call.
/// Multiple EntityChange records may share the same TransactionUnique when multiple entities are
/// modified in the same SaveChanges operation.
/// </para>
/// <para>
/// Property-level changes are stored in related EntityChangeProperty records. Query the Properties
/// collection to see what specific values changed.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Query changes for a specific entity
/// var changes = await context.EntityChanges
///     .Where(c => c.EntityName == "Customer" &amp;&amp; c.ItemId == customerId)
///     .Include(c => c.Properties)
///     .OrderByDescending(c => c.ChangedOn)
///     .ToListAsync();
/// </code>
/// </example>
public class EntityChange<TUserKey>
{
    /// <summary>
    /// Gets or sets the primary key for this change record.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier grouping changes within the same SaveChanges call.
    /// </summary>
    /// <remarks>
    /// Use this to find all entities modified in the same transaction.
    /// A new Guid is generated for each SaveChanges invocation.
    /// </remarks>
    public Guid TransactionUnique { get; set; }

    /// <summary>
    /// Gets or sets the correlation identifier from the audit context.
    /// </summary>
    /// <remarks>
    /// Links audit records to distributed tracing. Typically the HTTP request TraceIdentifier.
    /// May span multiple SaveChanges calls within the same request.
    /// </remarks>
    public Guid? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the database table name for the changed entity.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the CLR type name of the changed entity.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the primary key value of the changed entity.
    /// </summary>
    /// <remarks>
    /// For newly created entities with auto-generated keys, this is populated
    /// after the initial SaveChanges completes and the key is generated.
    /// </remarks>
    public int? ItemId { get; set; }

    /// <summary>
    /// Gets or sets the type of change operation (Create, Update, Delete).
    /// </summary>
    public EntityChangeOperation Operation { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this change was recorded.
    /// </summary>
    public DateTimeOffset ChangedOn { get; set; }

    /// <summary>
    /// Gets or sets the user identifier who made this change.
    /// </summary>
    /// <remarks>
    /// Populated from IAuditContextProvider.UserId. May be null for system-generated changes.
    /// </remarks>
    public TUserKey? ChangedBy { get; set; }

    /// <summary>
    /// Gets or sets a human-readable description of the change.
    /// </summary>
    /// <remarks>
    /// Populated from IEntityChangeAuditable.EntityChangeDescription if the entity provides one.
    /// For example: "Invoice #1234" or "Customer: Acme Corp".
    /// </remarks>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the parent entity type name for hierarchical tracking.
    /// </summary>
    /// <remarks>
    /// Used to group child entity changes with their parent.
    /// For example, OrderLine changes might have ParentEntityName = "Order".
    /// </remarks>
    [MaxLength(256)]
    public string? ParentEntityName { get; set; }

    /// <summary>
    /// Gets or sets the parent entity's primary key.
    /// </summary>
    public int? ParentItemId { get; set; }

    /// <summary>
    /// Gets or sets the collection of property-level changes for this entity.
    /// </summary>
    public virtual ICollection<EntityChangeProperty> Properties { get; set; } = new List<EntityChangeProperty>();
}
