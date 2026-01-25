namespace Imagile.EntityFrameworkCore.Tests.Configuration;

/// <summary>
/// Fluent builder for configuring convention test options.
/// </summary>
public class ConventionTestOptionsBuilder
{
    private readonly ConventionTestOptions _options = new();

    /// <summary>
    /// Configures exclusions for a specific rule.
    /// </summary>
    /// <typeparam name="TRule">The rule type to configure.</typeparam>
    /// <param name="configure">Action to configure the rule exclusions.</param>
    /// <returns>The builder for chaining.</returns>
    public ConventionTestOptionsBuilder ForRule<TRule>(Action<RuleExclusionBuilder> configure)
    {
        var ruleType = typeof(TRule);
        if (!_options.RuleExclusions.TryGetValue(ruleType, out var builder))
        {
            builder = new RuleExclusionBuilder();
            _options.RuleExclusions[ruleType] = builder;
        }

        configure(builder);
        return this;
    }

    /// <summary>
    /// Excludes an entity from all convention rules.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to exclude.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public ConventionTestOptionsBuilder ExcludeEntityFromAllRules<TEntity>()
    {
        return ExcludeEntityFromAllRules(typeof(TEntity).Name);
    }

    /// <summary>
    /// Excludes an entity from all convention rules by name.
    /// </summary>
    /// <param name="entityName">The name of the entity to exclude.</param>
    /// <returns>The builder for chaining.</returns>
    public ConventionTestOptionsBuilder ExcludeEntityFromAllRules(string entityName)
    {
        _options.GlobalExclusions.Add(ExclusionKey.ForEntity(entityName));
        return this;
    }

    /// <summary>
    /// Builds the configured convention test options.
    /// </summary>
    /// <returns>The configured options.</returns>
    public ConventionTestOptions Build()
    {
        return _options;
    }
}
