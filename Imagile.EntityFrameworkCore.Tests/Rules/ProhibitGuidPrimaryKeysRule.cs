using Imagile.EntityFrameworkCore.Tests.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Imagile.EntityFrameworkCore.Tests.Rules;

/// <summary>
/// Rule that validates single-property primary keys are not of type Guid.
/// </summary>
public class ProhibitGuidPrimaryKeysRule : IConventionRule
{
    public string Name => "Primary keys cannot be Guid";

    public IEnumerable<ConventionViolation> Validate(
        IEnumerable<DbContext> contexts,
        ConventionTestOptions options)
    {
        var violations = new List<ConventionViolation>();

        foreach (var context in contexts)
        {
            var contextName = context.GetType().Name;

            foreach (var entityType in context.Model.GetEntityTypes())
            {
                // Skip owned entities
                if (entityType.IsOwned())
                {
                    continue;
                }

                var entityName = entityType.ClrType.Name;

                // Check if entity is excluded
                if (options.IsExcluded<ProhibitGuidPrimaryKeysRule>(entityName))
                {
                    continue;
                }

                var primaryKey = entityType.FindPrimaryKey();
                if (primaryKey is null)
                {
                    continue;
                }

                // Skip composite keys (multiple properties)
                if (primaryKey.Properties.Count > 1)
                {
                    continue;
                }

                var keyProperty = primaryKey.Properties.Single();
                var propertyName = keyProperty.Name;

                // Check if property is excluded
                if (options.IsExcluded<ProhibitGuidPrimaryKeysRule>(entityName, propertyName))
                {
                    continue;
                }

                var clrType = keyProperty.ClrType;

                // Prohibit Guid (including nullable)
                if (clrType == typeof(Guid) || clrType == typeof(Guid?))
                {
                    violations.Add(new ConventionViolation(
                        contextName,
                        entityName,
                        propertyName));
                }
            }
        }

        return violations;
    }
}
