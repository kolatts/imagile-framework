using System.ComponentModel.DataAnnotations;

namespace Imagile.Framework.EntityFrameworkCore.Entities;

/// <summary>
/// Stores a human-readable lookup entry for each value of every enum column persisted as an integer.
/// </summary>
/// <remarks>
/// <para>
/// This entity is registered and configured via
/// <see cref="Extensions.ModelBuilderExtensions.ConfigureEnumLookupValues"/> and populated via
/// <see cref="Extensions.DbContextExtensions.SeedEnumLookupValues"/>. The composite primary key
/// is <c>(TableName, ColumnName, Value)</c>, stored in the <c>__EnumLookupValues</c> table.
/// </para>
/// <para>
/// Consumers can query this table at runtime to display friendly names and descriptions for
/// integer enum values stored in the database — useful for reporting, APIs, and admin UIs.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register and seed enum lookup values in your DbContext
/// protected override void OnModelCreating(ModelBuilder modelBuilder)
/// {
///     base.OnModelCreating(modelBuilder);
///     modelBuilder.ConfigureEnumLookupValues();
/// }
///
/// // Populate after EnsureCreated / Migrate
/// context.SeedEnumLookupValues();
/// </code>
/// </example>
public class EnumLookupValue
{
    /// <summary>
    /// Gets or sets the name of the database table containing the enum column.
    /// </summary>
    [StringLength(255)]
    [Required]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the database column that stores the enum integer value.
    /// </summary>
    [StringLength(255)]
    [Required]
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the integer representation of the enum member.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Gets or sets the enum member name (e.g., <c>"Active"</c>).
    /// </summary>
    [StringLength(255)]
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable description sourced from
    /// <see cref="System.ComponentModel.DescriptionAttribute"/> if present,
    /// otherwise falls back to <see cref="Name"/>.
    /// </summary>
    [StringLength(255)]
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when this lookup value was seeded into the database.
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
}
