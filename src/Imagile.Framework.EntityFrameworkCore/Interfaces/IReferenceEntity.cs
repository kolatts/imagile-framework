namespace Imagile.Framework.EntityFrameworkCore.Interfaces;

/// <summary>
/// Marks an entity as reference/lookup data that can be seeded and upserted via
/// <see cref="Extensions.ReferenceEntityExtensions.AddOrUpdateReferenceEntities{T}"/>.
/// </summary>
/// <typeparam name="T">The implementing entity type (self-referencing generic).</typeparam>
/// <remarks>
/// <para>
/// Reference entities represent application-level lookup data (e.g., status codes, categories)
/// that is seeded into the database and kept in sync with the application's defined values.
/// </para>
/// <para>
/// The <see cref="ItemId"/> property identifies each record for upsert logic:
/// existing records are updated, missing records are inserted, and records absent from
/// seed data are deleted.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class StatusCode : IReferenceEntity&lt;StatusCode&gt;
/// {
///     public int Id { get; set; }
///     public string Name { get; set; } = string.Empty;
///
///     public string ItemId => Id.ToString();
///
///     public static List&lt;StatusCode&gt; GetSeedData() =>
///     [
///         new StatusCode { Id = 1, Name = "Active" },
///         new StatusCode { Id = 2, Name = "Inactive" },
///     ];
/// }
///
/// // Upsert reference data
/// context.AddOrUpdateReferenceEntities&lt;StatusCode&gt;();
/// </code>
/// </example>
public interface IReferenceEntity<T> where T : class, IReferenceEntity<T>
{
    /// <summary>
    /// Returns all seed data for this reference entity type.
    /// </summary>
    /// <returns>The complete list of seed records for this entity type.</returns>
    static abstract List<T> GetSeedData();

    /// <summary>
    /// Gets the string identifier used to match records during upsert operations.
    /// </summary>
    /// <remarks>
    /// Typically returns the primary key as a string. Used by
    /// <see cref="Extensions.ReferenceEntityExtensions.AddOrUpdateReferenceEntities{T}"/>
    /// to determine whether a seed record already exists in the database.
    /// </remarks>
    string ItemId { get; }
}
