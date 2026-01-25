using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Imagile.Framework.EntityFrameworkCore.Entities;

/// <summary>
/// Records the old and new values for a property change within an EntityChange.
/// </summary>
/// <remarks>
/// <para>
/// Each EntityChangeProperty represents one property that changed on an entity.
/// The OriginalValue and NewValue are stored as strings using ToString() or custom formatting.
/// </para>
/// <para>
/// For properties marked with [Auditable(hideValueChanges: true)], the values will contain
/// "[HIDDEN]" instead of actual data, indicating a change occurred without exposing sensitive values.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Find salary changes for a specific employee
/// var salaryChanges = await context.Set&lt;EntityChangeProperty&gt;()
///     .Include(p => p.EntityChange)
///     .Where(p => p.PropertyName == "Salary"
///         &amp;&amp; p.EntityChange.EntityName == "Employee"
///         &amp;&amp; p.EntityChange.ItemId == employeeId)
///     .OrderByDescending(p => p.EntityChange.ChangedOn)
///     .Select(p => new {
///         p.OriginalValue,
///         p.NewValue,
///         p.EntityChange.ChangedOn,
///         p.EntityChange.ChangedBy
///     })
///     .ToListAsync();
/// </code>
/// </example>
public class EntityChangeProperty
{
    /// <summary>
    /// Gets or sets the primary key for this property change record.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the parent EntityChange.
    /// </summary>
    public int EntityChangeId { get; set; }

    /// <summary>
    /// Gets or sets the parent EntityChange this property belongs to.
    /// </summary>
    [ForeignKey(nameof(EntityChangeId))]
    public virtual EntityChange<object>? EntityChange { get; set; }

    /// <summary>
    /// Gets or sets the name of the property that changed.
    /// </summary>
    /// <remarks>
    /// The CLR property name (e.g., "FirstName"), not the database column name.
    /// </remarks>
    [Required]
    [MaxLength(256)]
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database column name for this property.
    /// </summary>
    /// <remarks>
    /// May differ from PropertyName if column name is explicitly configured.
    /// Useful for correlating with raw SQL queries or database logs.
    /// </remarks>
    [MaxLength(256)]
    public string? ColumnName { get; set; }

    /// <summary>
    /// Gets or sets the value before the change, formatted as a string.
    /// </summary>
    /// <remarks>
    /// <para>Null for newly created entities (Create operation).</para>
    /// <para>Contains "[HIDDEN]" for properties with Auditable(hideValueChanges: true).</para>
    /// <para>Formatted using DisplayFormatAttribute if present, otherwise ToString().</para>
    /// </remarks>
    public string? OriginalValue { get; set; }

    /// <summary>
    /// Gets or sets the value after the change, formatted as a string.
    /// </summary>
    /// <remarks>
    /// <para>Null for deleted entities (Delete operation).</para>
    /// <para>Contains "[HIDDEN]" for properties with Auditable(hideValueChanges: true).</para>
    /// <para>Formatted using DisplayFormatAttribute if present, otherwise ToString().</para>
    /// </remarks>
    public string? NewValue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether actual values are hidden.
    /// </summary>
    /// <remarks>
    /// True when the property is marked with [Auditable(hideValueChanges: true)].
    /// When true, OriginalValue and NewValue contain "[HIDDEN]" instead of actual values.
    /// </remarks>
    public bool AreValuesHidden { get; set; }
}
