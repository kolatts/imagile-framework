namespace Imagile.Framework.Core.Attributes;

/// <summary>
/// Specifies the native language name for an enum value.
/// Used for displaying localized names or original-language terms.
/// </summary>
/// <param name="name">The native name in the original language</param>
/// <example>
/// <code>
/// enum Country
/// {
///     [NativeName("Deutschland")]
///     Germany,
///
///     [NativeName("Espana")]
///     Spain,
///
///     [NativeName("Nihon")]
///     Japan
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field)]
public sealed class NativeNameAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the native name in the original language.
    /// </summary>
    public string Name { get; } = name;
}
