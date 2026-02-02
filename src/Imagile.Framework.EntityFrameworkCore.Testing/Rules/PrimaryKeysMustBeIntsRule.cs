using Imagile.Framework.EntityFrameworkCore.Testing.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Imagile.Framework.EntityFrameworkCore.Testing.Rules;

/// <summary>
/// Rule that validates single-property primary keys are of type int, long, or enum.
/// </summary>
public class PrimaryKeysMustBeIntsRule : IConventionRule
{
    public string Name => "Primary keys must be int, long, or enum";

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
                if (options.IsExcluded<PrimaryKeysMustBeIntsRule>(entityName))
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
                if (options.IsExcluded<PrimaryKeysMustBeIntsRule>(entityName, propertyName))
                {
                    continue;
                }

                var clrType = keyProperty.ClrType;

                // Allow int, long, and enums (enums are backed by int storage)
                var isValidType = clrType == typeof(int) ||
                    clrType == typeof(int?) ||
                    clrType == typeof(long) ||
                    clrType == typeof(long?) ||
                    clrType.IsEnum;

                if (!isValidType)
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
