namespace Imagile.Framework.Core.Attributes;

/// <summary>
/// Specifies that an enum value includes one or more other values of the same enum type.
/// Used for hierarchical or composite patterns like roles that include other roles.
/// </summary>
/// <typeparam name="TEnum">The enum type (same as the decorated field's containing type)</typeparam>
/// <remarks>
/// <para>
/// This attribute creates a self-referential relationship within a single enum type.
/// Unlike <see cref="AssociatedAttribute{TEnum}"/> which relates different enum types,
/// IncludesAttribute relates values within the same enum.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// enum Role
/// {
///     Reader,
///     Writer,
///
///     [Includes&lt;Role&gt;(Role.Reader)]
///     Editor,
///
///     [Includes&lt;Role&gt;(Role.Reader, Role.Writer, Role.Editor)]
///     Admin
/// }
/// </code>
/// </example>
public class IncludesAttribute<TEnum> : AssociatedAttribute<TEnum>
    where TEnum : struct, Enum
{
    /// <summary>
    /// Initializes a new instance specifying which enum values are included.
    /// </summary>
    /// <param name="included">The enum values that are included by this value</param>
    public IncludesAttribute(params TEnum[] included) : base(included) { }

    /// <summary>
    /// Gets the enum values that are included by this value.
    /// This is an alias for <see cref="AssociatedAttribute{TEnum}.Associated"/>.
    /// </summary>
    public TEnum[] Included => Associated;
}
