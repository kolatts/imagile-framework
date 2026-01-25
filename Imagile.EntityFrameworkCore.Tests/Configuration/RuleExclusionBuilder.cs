using System.Linq.Expressions;

namespace Imagile.EntityFrameworkCore.Tests.Configuration;

/// <summary>
/// Fluent builder for configuring exclusions for a specific convention rule.
/// </summary>
public class RuleExclusionBuilder
{
    internal HashSet<ExclusionKey> Exclusions { get; } = new();

    /// <summary>
    /// Excludes an entire entity from the rule.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to exclude.</typeparam>
    /// <returns>The builder for chaining.</returns>
    public RuleExclusionBuilder ExcludeEntity<TEntity>()
    {
        return ExcludeEntity(typeof(TEntity).Name);
    }

    /// <summary>
    /// Excludes an entire entity from the rule by name.
    /// </summary>
    /// <param name="entityName">The name of the entity to exclude.</param>
    /// <returns>The builder for chaining.</returns>
    public RuleExclusionBuilder ExcludeEntity(string entityName)
    {
        Exclusions.Add(ExclusionKey.ForEntity(entityName));
        return this;
    }

    /// <summary>
    /// Excludes a specific property of an entity from the rule.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyExpression">Expression selecting the property to exclude.</param>
    /// <returns>The builder for chaining.</returns>
    public RuleExclusionBuilder ExcludeProperty<TEntity, TProperty>(
        Expression<Func<TEntity, TProperty>> propertyExpression)
    {
        var propertyName = GetPropertyName(propertyExpression);
        return ExcludeProperty(typeof(TEntity).Name, propertyName);
    }

    /// <summary>
    /// Excludes a specific property of an entity from the rule by name.
    /// </summary>
    /// <param name="entityName">The name of the entity.</param>
    /// <param name="propertyName">The name of the property to exclude.</param>
    /// <returns>The builder for chaining.</returns>
    public RuleExclusionBuilder ExcludeProperty(string entityName, string propertyName)
    {
        Exclusions.Add(ExclusionKey.ForProperty(entityName, propertyName));
        return this;
    }

    private static string GetPropertyName<TEntity, TProperty>(
        Expression<Func<TEntity, TProperty>> propertyExpression)
    {
        if (propertyExpression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        throw new ArgumentException(
            "Property expression must be a simple member access expression",
            nameof(propertyExpression));
    }
}
