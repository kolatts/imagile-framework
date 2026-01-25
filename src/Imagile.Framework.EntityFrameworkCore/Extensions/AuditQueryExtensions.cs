using Imagile.Framework.EntityFrameworkCore.Entities;
using Imagile.Framework.EntityFrameworkCore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Imagile.Framework.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for querying entity change history.
/// </summary>
public static class AuditQueryExtensions
{
    /// <summary>
    /// Gets the change history for a specific entity.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TUserKey">The type of the user identifier.</typeparam>
    /// <param name="context">The DbContext containing EntityChanges.</param>
    /// <param name="entityName">The entity type name (e.g., "Customer").</param>
    /// <param name="itemId">The entity's primary key value.</param>
    /// <returns>An IQueryable of EntityChange records for the specified entity.</returns>
    /// <remarks>
    /// <para>
    /// Returns changes ordered by ChangedOn descending (most recent first).
    /// Include the Properties collection to see property-level changes.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var customerHistory = await context.GetChangeHistory&lt;AppDbContext, int&gt;("Customer", customerId)
    ///     .Include(c => c.Properties)
    ///     .Take(10)
    ///     .ToListAsync();
    ///
    /// foreach (var change in customerHistory)
    /// {
    ///     Console.WriteLine($"{change.ChangedOn}: {change.Operation} by {change.ChangedBy}");
    ///     foreach (var prop in change.Properties)
    ///     {
    ///         Console.WriteLine($"  {prop.PropertyName}: {prop.OriginalValue} -> {prop.NewValue}");
    ///     }
    /// }
    /// </code>
    /// </example>
    public static IQueryable<EntityChange<TUserKey>> GetChangeHistory<TContext, TUserKey>(
        this TContext context,
        string entityName,
        int itemId)
        where TContext : Microsoft.EntityFrameworkCore.DbContext
    {
        return context.Set<EntityChange<TUserKey>>()
            .Where(c => c.EntityName == entityName && c.ItemId == itemId)
            .OrderByDescending(c => c.ChangedOn);
    }

    /// <summary>
    /// Gets the change history for a specific entity instance.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TEntity">The entity type implementing IEntityChangeAuditable.</typeparam>
    /// <typeparam name="TUserKey">The type of the user identifier.</typeparam>
    /// <param name="context">The DbContext containing EntityChanges.</param>
    /// <param name="entity">The entity to get history for.</param>
    /// <returns>An IQueryable of EntityChange records for the specified entity.</returns>
    /// <remarks>
    /// <para>
    /// Uses the entity's type name and ItemId to query change history.
    /// Returns null if the entity has no ItemId (not yet persisted).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var customer = await context.Customers.FindAsync(customerId);
    /// var history = context.GetChangeHistoryFor&lt;AppDbContext, Customer, int&gt;(customer);
    /// if (history is not null)
    /// {
    ///     var changes = await history.Include(c => c.Properties).ToListAsync();
    /// }
    /// </code>
    /// </example>
    public static IQueryable<EntityChange<TUserKey>>? GetChangeHistoryFor<TContext, TEntity, TUserKey>(
        this TContext context,
        TEntity entity)
        where TContext : Microsoft.EntityFrameworkCore.DbContext
        where TEntity : IEntityChangeAuditable<TUserKey>
    {
        if (entity.ItemId is null) return null;

        return context.GetChangeHistory<TContext, TUserKey>(
            typeof(TEntity).Name,
            entity.ItemId.Value);
    }

    /// <summary>
    /// Gets changes grouped by transaction for a specific entity.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TUserKey">The type of the user identifier.</typeparam>
    /// <param name="context">The DbContext containing EntityChanges.</param>
    /// <param name="entityName">The entity type name.</param>
    /// <param name="itemId">The entity's primary key value.</param>
    /// <returns>Changes grouped by TransactionUnique.</returns>
    /// <example>
    /// <code>
    /// var groupedChanges = await context
    ///     .GetChangesByTransaction&lt;AppDbContext, int&gt;("Invoice", invoiceId)
    ///     .ToListAsync();
    ///
    /// foreach (var group in groupedChanges)
    /// {
    ///     Console.WriteLine($"Transaction {group.Key} at {group.First().ChangedOn}:");
    ///     foreach (var change in group)
    ///     {
    ///         Console.WriteLine($"  {change.EntityName}: {change.Operation}");
    ///     }
    /// }
    /// </code>
    /// </example>
    public static IQueryable<IGrouping<Guid, EntityChange<TUserKey>>> GetChangesByTransaction<TContext, TUserKey>(
        this TContext context,
        string entityName,
        int itemId)
        where TContext : Microsoft.EntityFrameworkCore.DbContext
    {
        return context.Set<EntityChange<TUserKey>>()
            .Where(c => c.EntityName == entityName && c.ItemId == itemId)
            .GroupBy(c => c.TransactionUnique);
    }

    /// <summary>
    /// Gets all changes within a specific transaction.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TUserKey">The type of the user identifier.</typeparam>
    /// <param name="context">The DbContext containing EntityChanges.</param>
    /// <param name="transactionUnique">The transaction identifier.</param>
    /// <returns>All EntityChange records from the specified transaction.</returns>
    /// <example>
    /// <code>
    /// // Given a change, find all other changes in the same transaction
    /// var relatedChanges = await context
    ///     .GetTransactionChanges&lt;AppDbContext, int&gt;(change.TransactionUnique)
    ///     .Include(c => c.Properties)
    ///     .ToListAsync();
    /// </code>
    /// </example>
    public static IQueryable<EntityChange<TUserKey>> GetTransactionChanges<TContext, TUserKey>(
        this TContext context,
        Guid transactionUnique)
        where TContext : Microsoft.EntityFrameworkCore.DbContext
    {
        return context.Set<EntityChange<TUserKey>>()
            .Where(c => c.TransactionUnique == transactionUnique)
            .OrderBy(c => c.EntityName)
            .ThenBy(c => c.ItemId);
    }

    /// <summary>
    /// Gets recent changes across all entities, optionally filtered by user.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <typeparam name="TUserKey">The type of the user identifier.</typeparam>
    /// <param name="context">The DbContext containing EntityChanges.</param>
    /// <param name="changedBy">Optional user ID to filter by.</param>
    /// <param name="since">Optional start date to filter by.</param>
    /// <returns>Recent EntityChange records.</returns>
    /// <example>
    /// <code>
    /// // Get changes by a specific user in the last 24 hours
    /// var recentByUser = await context
    ///     .GetRecentChanges&lt;AppDbContext, int&gt;(userId, DateTime.UtcNow.AddDays(-1))
    ///     .Take(100)
    ///     .ToListAsync();
    /// </code>
    /// </example>
    public static IQueryable<EntityChange<TUserKey>> GetRecentChanges<TContext, TUserKey>(
        this TContext context,
        TUserKey? changedBy = default,
        DateTimeOffset? since = null)
        where TContext : Microsoft.EntityFrameworkCore.DbContext
    {
        var query = context.Set<EntityChange<TUserKey>>().AsQueryable();

        if (changedBy is not null && !EqualityComparer<TUserKey>.Default.Equals(changedBy, default))
        {
            query = query.Where(c => EqualityComparer<TUserKey>.Default.Equals(c.ChangedBy, changedBy));
        }

        if (since.HasValue)
        {
            query = query.Where(c => c.ChangedOn >= since.Value);
        }

        return query.OrderByDescending(c => c.ChangedOn);
    }
}
