namespace Imagile.Framework.EntityFrameworkCore.Interfaces;

/// <summary>
/// Marks an entity for property-level change tracking. Records old and new values for audited properties.
/// </summary>
/// <typeparam name="TUserKey">The type of the user identifier (e.g., int, Guid, string)</typeparam>
/// <remarks>
/// <para>
/// Extends IAuditableEntity to enable detailed change history. When an entity implementing this interface
/// is modified, the system creates EntityChange and EntityChangeProperty records capturing exactly
/// what changed, the old value, and the new value.
/// </para>
/// <para>
/// Only properties marked with [Auditable] attribute are tracked. Use this interface when you need
/// detailed audit trails for compliance, debugging, or undo functionality.
/// </para>
/// <para>
/// The ItemId property should return the entity's primary key. For entities with auto-generated keys,
/// this may be null/0 until after SaveChanges completes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Invoice : IEntityChangeAuditable&lt;int&gt;
/// {
///     public int Id { get; set; }
///
///     [Auditable]
///     public decimal Amount { get; set; }
///
///     [Auditable]
///     public string Status { get; set; } = string.Empty;
///
///     // IEntityChangeAuditable implementation
///     public int? ItemId => Id == 0 ? null : Id;
///     public string? ParentEntityName => null;
///     public int? ParentItemId => null;
///     public string? EntityChangeDescription => $"Invoice #{Id}";
///
///     // IAuditableEntity properties...
/// }
/// </code>
/// </example>
public interface IEntityChangeAuditable<TUserKey> : IAuditableEntity<TUserKey>
{
    /// <summary>
    /// Gets the primary key value for EntityChange tracking.
    /// </summary>
    /// <remarks>
    /// Returns the entity's primary key, or null if not yet persisted (auto-generated key).
    /// The audit system handles temporary values and populates the EntityChange.ItemId
    /// after SaveChanges completes.
    /// </remarks>
    int? ItemId { get; }

    /// <summary>
    /// Gets the parent entity type name for hierarchical change tracking.
    /// </summary>
    /// <remarks>
    /// Use this to link child entity changes to their parent. For example, an OrderLine
    /// might return "Order" to group its changes with the parent Order's changes.
    /// Return null for root entities without a parent relationship.
    /// </remarks>
    string? ParentEntityName { get; }

    /// <summary>
    /// Gets the parent entity's primary key for hierarchical change tracking.
    /// </summary>
    /// <remarks>
    /// The foreign key value linking to the parent entity. Used together with
    /// ParentEntityName to create a hierarchical audit trail.
    /// </remarks>
    int? ParentItemId { get; }

    /// <summary>
    /// Gets a human-readable description for the EntityChange record.
    /// </summary>
    /// <remarks>
    /// Provides context in audit logs. For example: "Invoice #1234" or "Customer: Acme Corp".
    /// Return null to use the default (entity type name + primary key).
    /// </remarks>
    string? EntityChangeDescription { get; }
}
