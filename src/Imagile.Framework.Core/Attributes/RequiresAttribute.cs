namespace Imagile.Framework.Core.Attributes;

/// <summary>
/// Specifies that an enum value requires one or more values from another enum type.
/// Extends <see cref="AssociatedAttribute{TEnum}"/> with "require all" vs "require any" semantics.
/// </summary>
/// <typeparam name="TEnum">The type of enum this value requires</typeparam>
/// <remarks>
/// <para>
/// When <see cref="RequireAll"/> is <c>true</c>, all specified values must be present for the requirement to be satisfied.
/// When <see cref="RequireAll"/> is <c>false</c> (default), at least one of the specified values must be present.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// enum Permission { Read, Write, Delete }
/// enum Feature
/// {
///     // Requires Read OR Write permission (any)
///     [Requires&lt;Permission&gt;(Permission.Read, Permission.Write)]
///     ViewReports,
///
///     // Requires BOTH Read AND Write permissions (all)
///     [Requires&lt;Permission&gt;(true, Permission.Read, Permission.Write)]
///     EditReports
/// }
/// </code>
/// </example>
public class RequiresAttribute<TEnum> : AssociatedAttribute<TEnum>
    where TEnum : struct, Enum
{
    /// <summary>
    /// Initializes a new instance with optional RequireAll semantics.
    /// </summary>
    /// <param name="requireAll">If <c>true</c>, all required values must be present. If <c>false</c>, at least one must be present.</param>
    /// <param name="required">The enum values that are required</param>
    public RequiresAttribute(bool requireAll, params TEnum[] required) : base(required)
    {
        RequireAll = requireAll;
    }

    /// <summary>
    /// Initializes a new instance with default RequireAll=false (any) semantics.
    /// </summary>
    /// <param name="required">The enum values that are required</param>
    public RequiresAttribute(params TEnum[] required) : base(required) { }

    /// <summary>
    /// Gets the required enum values. This is an alias for <see cref="AssociatedAttribute{TEnum}.Associated"/>.
    /// </summary>
    public TEnum[] Required => Associated;

    /// <summary>
    /// Gets whether all required values must be present (<c>true</c>) or just one (<c>false</c>).
    /// Default is <c>false</c> (require any).
    /// </summary>
    public bool RequireAll { get; init; }
}
