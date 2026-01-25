namespace Imagile.Framework.EntityFrameworkCore.Testing.Configuration;

/// <summary>
/// Configuration options for convention tests, including rule-specific and global exclusions.
/// </summary>
public class ConventionTestOptions
{
    internal Dictionary<Type, RuleExclusionBuilder> RuleExclusions { get; } = new();
    internal HashSet<ExclusionKey> GlobalExclusions { get; } = new();

    /// <summary>
    /// Checks if an entity or property is excluded from a specific rule.
    /// </summary>
    /// <typeparam name="TRule">The rule type to check.</typeparam>
    /// <param name="entityName">The name of the entity.</param>
    /// <param name="propertyName">The optional name of the property.</param>
    /// <returns>True if the entity or property is excluded, otherwise false.</returns>
    public bool IsExcluded<TRule>(string entityName, string? propertyName = null)
    {
        var exclusionKey = propertyName is null
            ? ExclusionKey.ForEntity(entityName)
            : ExclusionKey.ForProperty(entityName, propertyName);

        // Check global exclusions first (applies to all rules)
        if (GlobalExclusions.Contains(ExclusionKey.ForEntity(entityName)))
        {
            return true;
        }

        if (propertyName is not null && GlobalExclusions.Contains(exclusionKey))
        {
            return true;
        }

        // Check rule-specific exclusions
        if (!RuleExclusions.TryGetValue(typeof(TRule), out var ruleBuilder))
        {
            return false;
        }

        // Check if the entire entity is excluded for this rule
        if (ruleBuilder.Exclusions.Contains(ExclusionKey.ForEntity(entityName)))
        {
            return true;
        }

        // Check if the specific property is excluded for this rule
        if (propertyName is not null && ruleBuilder.Exclusions.Contains(exclusionKey))
        {
            return true;
        }

        return false;
    }
}
