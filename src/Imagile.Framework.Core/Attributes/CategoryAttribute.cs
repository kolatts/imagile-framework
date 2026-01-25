namespace Imagile.Framework.Core.Attributes;

/// <summary>
/// Specifies the category for an enum value.
/// Used for grouping and organizing related enum values in UI or processing logic.
/// </summary>
/// <param name="category">The category name</param>
/// <example>
/// <code>
/// enum ReportType
/// {
///     [Category("Financial")]
///     BalanceSheet,
///
///     [Category("Financial")]
///     IncomeStatement,
///
///     [Category("Operational")]
///     InventoryReport
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field)]
public sealed class CategoryAttribute(string category) : Attribute
{
    /// <summary>
    /// Gets the category name.
    /// </summary>
    public string Category { get; } = category;
}
