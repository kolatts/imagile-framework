using Imagile.Framework.Core.Interfaces;
using Imagile.Framework.EntityFrameworkCore.Interfaces;

namespace Imagile.Framework.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for working with soft-deleted entities.
/// </summary>
public static class SoftDeleteExtensions
{
    /// <summary>
    /// Restores a soft-deleted entity by clearing the IsDeleted flag and deletion metadata.
    /// </summary>
    /// <typeparam name="TEntity">The entity type implementing IAuditableEntity.</typeparam>
    /// <typeparam name="TUserKey">The type of the user identifier.</typeparam>
    /// <param name="entity">The soft-deleted entity to restore.</param>
    /// <param name="auditContext">The audit context provider for the current user.</param>
    /// <remarks>
    /// <para>
    /// This method sets IsDeleted to false and clears DeletedOn and DeletedBy.
    /// The ImagileDbContext SaveChanges override will automatically update ModifiedOn and ModifiedBy.
    /// </para>
    /// <para>
    /// Call SaveChanges after Restore() to persist the changes.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Restore a soft-deleted customer
    /// var deletedCustomer = await context.Customers
    ///     .IgnoreQueryFilters()  // Need this to find soft-deleted entities
    ///     .FirstOrDefaultAsync(c => c.Id == customerId);
    ///
    /// if (deletedCustomer?.IsDeleted == true)
    /// {
    ///     deletedCustomer.Restore(auditContext);
    ///     await context.SaveChangesAsync();
    /// }
    /// </code>
    /// </example>
    public static void Restore<TEntity, TUserKey>(
        this TEntity entity,
        IAuditContextProvider<TUserKey, object?> auditContext)
        where TEntity : IAuditableEntity<TUserKey>
    {
        if (!entity.IsDeleted)
        {
            throw new InvalidOperationException(
                $"Cannot restore entity of type {typeof(TEntity).Name}: entity is not soft-deleted (IsDeleted = false).");
        }

        entity.IsDeleted = false;
        entity.DeletedOn = null;
        entity.DeletedBy = default;

        // ModifiedOn and ModifiedBy will be set by ImagileDbContext.SaveChanges
    }

    /// <summary>
    /// Restores a soft-deleted entity without audit context (sets ModifiedBy to default).
    /// </summary>
    /// <typeparam name="TEntity">The entity type implementing IAuditableEntity.</typeparam>
    /// <typeparam name="TUserKey">The type of the user identifier.</typeparam>
    /// <param name="entity">The soft-deleted entity to restore.</param>
    /// <remarks>
    /// <para>
    /// Use this overload for background jobs or system processes without user context.
    /// ModifiedBy will be set to default(TUserKey) when SaveChanges is called.
    /// </para>
    /// </remarks>
    public static void Restore<TEntity, TUserKey>(this TEntity entity)
        where TEntity : IAuditableEntity<TUserKey>
    {
        if (!entity.IsDeleted)
        {
            throw new InvalidOperationException(
                $"Cannot restore entity of type {typeof(TEntity).Name}: entity is not soft-deleted (IsDeleted = false).");
        }

        entity.IsDeleted = false;
        entity.DeletedOn = null;
        entity.DeletedBy = default;
    }

    /// <summary>
    /// Soft-deletes an entity by setting the IsDeleted flag.
    /// </summary>
    /// <typeparam name="TEntity">The entity type implementing IAuditableEntity.</typeparam>
    /// <typeparam name="TUserKey">The type of the user identifier.</typeparam>
    /// <param name="entity">The entity to soft-delete.</param>
    /// <remarks>
    /// <para>
    /// This method only sets IsDeleted to true. The ImagileDbContext SaveChanges override
    /// will automatically populate DeletedOn and DeletedBy when the change is detected.
    /// </para>
    /// <para>
    /// Call SaveChanges after SoftDelete() to persist the changes.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var customer = await context.Customers.FindAsync(customerId);
    /// if (customer is not null)
    /// {
    ///     customer.SoftDelete();
    ///     await context.SaveChangesAsync();  // DeletedOn, DeletedBy set automatically
    /// }
    /// </code>
    /// </example>
    public static void SoftDelete<TEntity, TUserKey>(this TEntity entity)
        where TEntity : IAuditableEntity<TUserKey>
    {
        if (entity.IsDeleted)
        {
            throw new InvalidOperationException(
                $"Cannot soft-delete entity of type {typeof(TEntity).Name}: entity is already soft-deleted.");
        }

        entity.IsDeleted = true;
        // DeletedOn and DeletedBy will be set by ImagileDbContext.SaveChanges
    }
}
