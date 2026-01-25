namespace Imagile.Framework.EntityFrameworkCore.Attributes;

/// <summary>
/// Explicitly marks a property as excluded from change tracking.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute to document that a property is intentionally excluded from audit tracking.
/// While properties without [Auditable] are already not tracked, [IgnoreAudit] makes the
/// exclusion explicit for code clarity and review.
/// </para>
/// <para>
/// Common use cases:
/// <list type="bullet">
///   <item>Computed properties that are derived from other tracked values</item>
///   <item>High-frequency update fields (like LastHeartbeat) that would create audit noise</item>
///   <item>Large binary/blob fields where change tracking would be expensive</item>
///   <item>Navigation properties and collections</item>
/// </list>
/// </para>
/// <para>
/// This is a marker attribute with no effect on behavior - the change tracking system
/// already ignores properties without [Auditable]. It exists for documentation purposes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Session : IEntityChangeAuditable&lt;int&gt;
/// {
///     public int Id { get; set; }
///
///     [Auditable]
///     public string UserId { get; set; } = string.Empty;
///
///     [Auditable]
///     public string IpAddress { get; set; } = string.Empty;
///
///     [IgnoreAudit]  // Updates every request, would create too much noise
///     public DateTimeOffset LastActivity { get; set; }
///
///     [IgnoreAudit]  // Large blob, expensive to track
///     public byte[]? SessionData { get; set; }
///
///     // IEntityChangeAuditable implementation...
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class IgnoreAuditAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IgnoreAuditAttribute"/> class.
    /// </summary>
    /// <param name="reason">Optional reason for excluding this property from audit tracking.</param>
    public IgnoreAuditAttribute(string? reason = null)
    {
        Reason = reason;
    }

    /// <summary>
    /// Gets the optional reason why this property is excluded from audit tracking.
    /// </summary>
    /// <remarks>
    /// Provides documentation for code reviewers about why the property is not audited.
    /// For example: "Updates every heartbeat, would create excessive audit records"
    /// </remarks>
    public string? Reason { get; }
}
