namespace Imagile.Framework.EntityFrameworkCore.Interfaces;

/// <summary>
/// Marks an entity for full audit tracking including user context and soft delete support.
/// </summary>
/// <typeparam name="TUserKey">The type of the user identifier (e.g., int, Guid, string)</typeparam>
/// <remarks>
/// <para>
/// Extends ITimestampedEntity to add user tracking (CreatedBy, ModifiedBy) and soft delete
/// (IsDeleted, DeletedOn, DeletedBy). Use this interface when you need to know WHO made changes,
/// not just WHEN they occurred.
/// </para>
/// <para>
/// Soft delete support is built-in: setting IsDeleted = true will populate DeletedOn and DeletedBy
/// automatically. Use global query filters to exclude soft-deleted entities from normal queries.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Customer : IAuditableEntity&lt;int&gt;
/// {
///     public int Id { get; set; }
///     public string Name { get; set; } = string.Empty;
///
///     // ITimestampedEntity
///     public DateTimeOffset CreatedOn { get; set; }
///     public DateTimeOffset ModifiedOn { get; set; }
///
///     // IAuditableEntity - user tracking
///     public int? CreatedBy { get; set; }
///     public int? ModifiedBy { get; set; }
///
///     // IAuditableEntity - soft delete
///     public bool IsDeleted { get; set; }
///     public DateTimeOffset? DeletedOn { get; set; }
///     public int? DeletedBy { get; set; }
/// }
/// </code>
/// </example>
public interface IAuditableEntity<TUserKey> : ITimestampedEntity
{
    /// <summary>
    /// Gets or sets the user identifier who created this entity.
    /// </summary>
    /// <remarks>
    /// Automatically populated from IAuditContextProvider.UserId on insert.
    /// Nullable to support system-generated records without user context.
    /// </remarks>
    TUserKey? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the user identifier who last modified this entity.
    /// </summary>
    /// <remarks>
    /// Automatically updated from IAuditContextProvider.UserId on every SaveChanges.
    /// </remarks>
    TUserKey? ModifiedBy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this entity has been soft-deleted.
    /// </summary>
    /// <remarks>
    /// When set to true, DeletedOn and DeletedBy are automatically populated.
    /// Use global query filters to exclude soft-deleted entities from normal queries.
    /// </remarks>
    bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this entity was soft-deleted.
    /// </summary>
    /// <remarks>
    /// Automatically populated when IsDeleted changes from false to true.
    /// Cleared when entity is restored (IsDeleted set back to false).
    /// </remarks>
    DateTimeOffset? DeletedOn { get; set; }

    /// <summary>
    /// Gets or sets the user identifier who soft-deleted this entity.
    /// </summary>
    /// <remarks>
    /// Automatically populated from IAuditContextProvider.UserId when soft-deleting.
    /// Cleared when entity is restored.
    /// </remarks>
    TUserKey? DeletedBy { get; set; }
}
