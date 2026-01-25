namespace Imagile.Framework.Core.Attributes;

/// <summary>
/// Marks a property that should not be updated during reference data sync or batch update operations.
/// Properties with this attribute will only be set during initial insert, not subsequent updates.
/// </summary>
/// <remarks>
/// <para>
/// This is a marker attribute with no properties - its presence alone indicates that
/// the decorated property should be excluded from update operations. This is useful for:
/// </para>
/// <list type="bullet">
///   <item><description>Properties set once during creation (e.g., CreatedDate)</description></item>
///   <item><description>Properties managed by external systems during sync</description></item>
///   <item><description>Computed or derived values that shouldn't be overwritten</description></item>
///   <item><description>Reference data fields that represent historical state</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class Product
/// {
///     public int Id { get; set; }
///
///     public string Name { get; set; }
///
///     [DoNotUpdate]
///     public DateTime CreatedDate { get; set; }
///
///     [DoNotUpdate]
///     public string OriginalSku { get; set; }
///
///     public string CurrentSku { get; set; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DoNotUpdateAttribute : Attribute
{
}
