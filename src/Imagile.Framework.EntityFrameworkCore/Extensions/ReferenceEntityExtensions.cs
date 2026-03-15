using Imagile.Framework.EntityFrameworkCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using EFDbContext = Microsoft.EntityFrameworkCore.DbContext;

namespace Imagile.Framework.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for seeding and synchronizing <see cref="IReferenceEntity{T}"/> data.
/// </summary>
public static class ReferenceEntityExtensions
{
    /// <summary>
    /// Upserts reference entity records, matching on <see cref="IReferenceEntity{T}.ItemId"/>.
    /// Existing records are updated, new records are inserted, and records absent from the seed
    /// data are deleted. Changes are saved immediately via <c>SaveChanges</c>.
    /// </summary>
    /// <typeparam name="T">The reference entity type to upsert.</typeparam>
    /// <param name="context">The DbContext to operate on.</param>
    /// <param name="seedEntities">
    /// Optional override for the seed data. When <c>null</c>, seed data is obtained from
    /// <see cref="IReferenceEntity{T}.GetSeedData"/>.
    /// </param>
    /// <remarks>
    /// <para>
    /// This method is designed for seeding application-level reference/lookup data (e.g., status
    /// codes, permission definitions) that is defined in code and must stay in sync with the database.
    /// </para>
    /// <para>
    /// The upsert logic uses <see cref="IReferenceEntity{T}.ItemId"/> as the match key.
    /// For entities with integer primary keys, <c>ItemId</c> typically returns <c>Id.ToString()</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Seed with default data from GetSeedData()
    /// context.AddOrUpdateReferenceEntities&lt;StatusCode&gt;();
    ///
    /// // Seed with custom data (e.g., filtered for a specific tenant)
    /// var tenantStatuses = StatusCode.GetSeedData().Where(s => s.IsGlobal).ToList();
    /// context.AddOrUpdateReferenceEntities&lt;StatusCode&gt;(tenantStatuses);
    /// </code>
    /// </example>
    public static void AddOrUpdateReferenceEntities<T>(
        this EFDbContext context,
        IEnumerable<T>? seedEntities = null)
        where T : class, IReferenceEntity<T>
    {
        var existingEntities = context.Set<T>().ToList();
        var entitiesToUpsert = (seedEntities ?? T.GetSeedData()).ToList();

        foreach (var entity in entitiesToUpsert)
        {
            var existingEntity = existingEntities.FirstOrDefault(e => e.ItemId == entity.ItemId);
            if (existingEntity != null)
            {
                context.Entry(existingEntity).CurrentValues.SetValues(entity);
            }
            else
            {
                context.Set<T>().Add(entity);
            }
        }

        var entitiesToDelete = existingEntities
            .Where(existing => entitiesToUpsert.All(seed => seed.ItemId != existing.ItemId))
            .ToList();

        context.RemoveRange(entitiesToDelete);

        context.SaveChanges();
    }
}
