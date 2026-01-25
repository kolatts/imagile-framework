using Imagile.EntityFrameworkCore.Tests.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Imagile.EntityFrameworkCore.Tests.Rules;

/// <summary>
/// Rule that validates primary keys follow the format EntityNameId.
/// </summary>
public class PrimaryKeyMustBeEntityNameIdRule : IConventionRule
{
    public string Name => "Primary key must be EntityNameId";

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
                if (options.IsExcluded<PrimaryKeyMustBeEntityNameIdRule>(entityName))
                {
                    continue;
                }

                var primaryKey = entityType.FindPrimaryKey();
                if (primaryKey is null)
                {
                    continue;
                }

                // Only check single-property primary keys
                if (primaryKey.Properties.Count != 1)
                {
                    continue;
                }

                // Skip table-per-hierarchy types (discriminator columns)
                if (entityType.GetDiscriminatorPropertyName() is not null)
                {
                    continue;
                }

                var keyProperty = primaryKey.Properties.Single();
                var propertyName = keyProperty.Name;

                // Skip if property is not generated (manually assigned keys can have different names)
                if (keyProperty.ValueGenerated == ValueGenerated.Never)
                {
                    continue;
                }

                // Check if property is excluded
                if (options.IsExcluded<PrimaryKeyMustBeEntityNameIdRule>(entityName, propertyName))
                {
                    continue;
                }

                var expectedName = $"{entityName}Id";

                if (!string.Equals(propertyName, expectedName, StringComparison.OrdinalIgnoreCase))
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
