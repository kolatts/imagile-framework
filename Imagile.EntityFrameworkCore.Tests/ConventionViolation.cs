namespace Imagile.EntityFrameworkCore.Tests;

/// <summary>
/// Represents a violation of a database convention rule.
/// </summary>
/// <param name="ContextName">The name of the DbContext where the violation occurred.</param>
/// <param name="EntityName">The name of the entity where the violation occurred.</param>
/// <param name="PropertyName">The name of the property where the violation occurred, if applicable.</param>
public record ConventionViolation(string ContextName, string EntityName, string? PropertyName = null)
{
    /// <summary>
    /// Returns a formatted string representation of the violation.
    /// </summary>
    public override string ToString()
    {
        return PropertyName is null
            ? $"{ContextName} ({EntityName})"
            : $"{ContextName} ({EntityName}) {PropertyName}";
    }
}
