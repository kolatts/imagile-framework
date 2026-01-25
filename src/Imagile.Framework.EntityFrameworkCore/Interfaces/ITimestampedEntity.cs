namespace Imagile.Framework.EntityFrameworkCore.Interfaces;

/// <summary>
/// Marks an entity for automatic timestamp tracking. Provides the most basic audit capability.
/// </summary>
/// <remarks>
/// <para>
/// Entities implementing this interface will have CreatedOn automatically set on insert
/// and ModifiedOn automatically updated on every save via the ImagileDbContext SaveChanges override.
/// </para>
/// <para>
/// Use DateTimeOffset instead of DateTime for proper timezone handling in distributed systems.
/// The timestamps are always stored in UTC.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class BlogPost : ITimestampedEntity
/// {
///     public int Id { get; set; }
///     public string Title { get; set; } = string.Empty;
///     public DateTimeOffset CreatedOn { get; set; }
///     public DateTimeOffset ModifiedOn { get; set; }
/// }
/// </code>
/// </example>
public interface ITimestampedEntity
{
    /// <summary>
    /// Gets or sets the UTC timestamp when this entity was created.
    /// </summary>
    /// <remarks>
    /// Automatically populated by ImagileDbContext on first SaveChanges for new entities.
    /// Should not be modified after initial creation.
    /// </remarks>
    DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this entity was last modified.
    /// </summary>
    /// <remarks>
    /// Automatically updated by ImagileDbContext on every SaveChanges where the entity has changes.
    /// Initially set to the same value as CreatedOn.
    /// </remarks>
    DateTimeOffset ModifiedOn { get; set; }
}
