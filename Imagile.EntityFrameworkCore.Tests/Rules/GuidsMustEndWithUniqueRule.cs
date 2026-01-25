using Imagile.EntityFrameworkCore.Tests.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Imagile.EntityFrameworkCore.Tests.Rules;

/// <summary>
/// Rule that validates Guid properties (except keys) end with "Unique".
/// </summary>
public class GuidsMustEndWithUniqueRule : IConventionRule
{
    public string Name => "Guid properties must end with Unique";

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
                var entityName = entityType.ClrType.Name;

                // Check if entity is excluded
                if (options.IsExcluded<GuidsMustEndWithUniqueRule>(entityName))
                {
                    continue;
                }

                foreach (var property in entityType.GetProperties())
                {
                    var clrType = property.ClrType;

                    // Only check Guid properties
                    if (clrType != typeof(Guid) && clrType != typeof(Guid?))
                    {
                        continue;
                    }

                    // Skip primary and foreign keys
                    if (property.IsPrimaryKey() || property.IsForeignKey())
                    {
                        continue;
                    }

                    var propertyName = property.Name;

                    // Check if property is excluded
                    if (options.IsExcluded<GuidsMustEndWithUniqueRule>(entityName, propertyName))
                    {
                        continue;
                    }

                    if (!propertyName.EndsWith("Unique", StringComparison.OrdinalIgnoreCase))
                    {
                        violations.Add(new ConventionViolation(
                            contextName,
                            entityName,
                            propertyName));
                    }
                }
            }
        }

        return violations;
    }
}
