namespace Imagile.Framework.Core.Attributes;

/// <summary>
/// Specifies a count value for an enum member.
/// Can be used for various counting scenarios such as number of items to generate,
/// capacity limits, sort order weights, or other numeric metadata.
/// </summary>
/// <example>
/// <code>
/// enum PackageSize
/// {
///     [Count(5)]
///     Small,
///
///     [Count(10)]
///     Medium,
///
///     [Count(25)]
///     Large
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class CountAttribute : Attribute
{
    /// <summary>
    /// Gets the count value.
    /// </summary>
    public int Value { get; }

    /// <summary>
    /// Initializes a new instance with the specified count value.
    /// </summary>
    /// <param name="value">The count value</param>
    public CountAttribute(int value)
    {
        Value = value;
    }
}
