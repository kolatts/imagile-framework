namespace Imagile.Framework.EntityFrameworkCore.Interfaces;

/// <summary>
/// Marks an entity as supporting soft deletion via the <see cref="IsDeleted"/> flag.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface on entities that should not be physically removed from the database.
/// Use <see cref="Extensions.SoftDeleteQueryExtensions.WhereNotDeleted{T}"/> to filter out
/// soft-deleted entities in queries.
/// </para>
/// <para>
/// For full audit support (timestamps, user tracking, DeletedOn/DeletedBy), implement
/// <see cref="IAuditableEntity{TUserKey}"/> instead, which extends this interface.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Product : ISoftDeletable
/// {
///     public int Id { get; set; }
///     public string Name { get; set; } = string.Empty;
///     public bool IsDeleted { get; set; }
/// }
///
/// // Filter out deleted products
/// var activeProducts = context.Products.WhereNotDeleted().ToList();
/// </code>
/// </example>
public interface ISoftDeletable
{
    /// <summary>
    /// Gets or sets a value indicating whether this entity has been soft-deleted.
    /// </summary>
    bool IsDeleted { get; set; }
}
