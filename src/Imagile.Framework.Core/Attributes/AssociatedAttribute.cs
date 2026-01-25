namespace Imagile.Framework.Core.Attributes;

/// <summary>
/// Specifies that an enum value is associated with one or more values from another enum type.
/// Used to create declarative relationships between enum types.
/// </summary>
/// <typeparam name="TEnum">The type of enum this value is associated with</typeparam>
/// <param name="associatedWith">The enum values this value is associated with</param>
/// <example>
/// <code>
/// enum Permission { Read, Write, Delete }
/// enum Feature
/// {
///     [Associated&lt;Permission&gt;(Permission.Read)]
///     ViewDashboard,
///
///     [Associated&lt;Permission&gt;(Permission.Read, Permission.Write)]
///     EditProfile
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class AssociatedAttribute<TEnum>(params TEnum[] associatedWith) : Attribute
    where TEnum : struct, Enum
{
    /// <summary>
    /// Gets the enum values this value is associated with.
    /// </summary>
    public TEnum[] Associated { get; } = associatedWith;
}
