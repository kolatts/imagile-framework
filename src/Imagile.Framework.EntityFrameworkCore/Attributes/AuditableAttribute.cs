namespace Imagile.Framework.EntityFrameworkCore.Attributes;

/// <summary>
/// Marks a property for inclusion in property-level change tracking.
/// Only properties marked with this attribute will be recorded in EntityChangeProperty.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute to properties on entities implementing IEntityChangeAuditable.
/// When the property value changes, the old and new values are recorded in an EntityChangeProperty record.
/// </para>
/// <para>
/// For sensitive data like passwords or tokens, use <c>hideValueChanges: true</c> to record that
/// a change occurred without storing the actual values. This maintains audit trail integrity
/// while protecting sensitive information.
/// </para>
/// <para>
/// Properties without this attribute are not tracked, even on IEntityChangeAuditable entities.
/// This opt-in approach prevents noise from tracking every property and gives developers
/// explicit control over what's audited.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Employee : IEntityChangeAuditable&lt;int&gt;
/// {
///     public int Id { get; set; }
///
///     [Auditable]
///     public string FirstName { get; set; } = string.Empty;
///
///     [Auditable]
///     public string LastName { get; set; } = string.Empty;
///
///     [Auditable]
///     public decimal Salary { get; set; }
///
///     [Auditable(hideValueChanges: true)]  // Records change, hides actual hash
///     public string PasswordHash { get; set; } = string.Empty;
///
///     public string InternalNotes { get; set; } = string.Empty;  // Not tracked
///
///     // IEntityChangeAuditable implementation...
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class AuditableAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditableAttribute"/> class.
    /// </summary>
    /// <param name="hideValueChanges">
    /// When true, records that a change occurred but stores "[HIDDEN]" instead of actual values.
    /// Use for sensitive data like passwords, tokens, or PII that shouldn't appear in audit logs.
    /// </param>
    public AuditableAttribute(bool hideValueChanges = false)
    {
        HideValueChanges = hideValueChanges;
    }

    /// <summary>
    /// Gets a value indicating whether actual values should be hidden in the audit log.
    /// </summary>
    /// <remarks>
    /// When true, EntityChangeProperty.OriginalValue and NewValue will contain "[HIDDEN]"
    /// instead of the actual values. The change is still recorded for audit completeness.
    /// </remarks>
    public bool HideValueChanges { get; }
}
