using Imagile.Framework.EntityFrameworkCore.Interfaces;

namespace Imagile.Framework.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for filtering soft-deleted entities in LINQ queries.
/// </summary>
public static class SoftDeleteQueryExtensions
{
    /// <summary>
    /// Filters an <see cref="IQueryable{T}"/> to exclude soft-deleted entities.
    /// </summary>
    /// <typeparam name="T">An entity type that implements <see cref="ISoftDeletable"/>.</typeparam>
    /// <param name="source">The queryable source to filter.</param>
    /// <returns>A queryable containing only entities where <see cref="ISoftDeletable.IsDeleted"/> is <c>false</c>.</returns>
    /// <remarks>
    /// <para>
    /// Use this when EF Core global query filters are not configured and you need an explicit,
    /// ergonomic filter in individual queries. Works with any queryable including in-memory LINQ.
    /// </para>
    /// <para>
    /// When using <c>ImagileDbContext</c>, soft-deleted entities are typically excluded globally
    /// via query filters. This extension is useful for scenarios that bypass global filters or
    /// for non-<c>ImagileDbContext</c> consumers.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Filter out deleted customers in a query
    /// var activeCustomers = context.Customers
    ///     .WhereNotDeleted()
    ///     .OrderBy(c => c.Name)
    ///     .ToList();
    ///
    /// // Works with any ISoftDeletable entity
    /// var activeProducts = products.AsQueryable().WhereNotDeleted();
    /// </code>
    /// </example>
    public static IQueryable<T> WhereNotDeleted<T>(this IQueryable<T> source)
        where T : ISoftDeletable =>
        source.Where(e => !e.IsDeleted);
}
