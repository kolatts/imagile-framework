using Imagile.EntityFrameworkCore.Tests.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Imagile.EntityFrameworkCore.Tests.Rules;

/// <summary>
/// Rule that validates Boolean properties start with Is, Has, Are, or Does.
/// </summary>
public class BooleansMustStartWithPrefixRule : IConventionRule
{
    public string Name => "Boolean properties must start with Is, Has, Are, or Does";

    private static readonly string[] ValidPrefixes = ["Is", "Has", "Are", "Does"];

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
                if (options.IsExcluded<BooleansMustStartWithPrefixRule>(entityName))
                {
                    continue;
                }

                foreach (var property in entityType.GetProperties())
                {
                    var clrType = property.ClrType;

                    // Only check Boolean properties (note: nullable bools are prohibited by another rule)
                    if (clrType != typeof(bool) && clrType != typeof(bool?))
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
                    if (options.IsExcluded<BooleansMustStartWithPrefixRule>(entityName, propertyName))
                    {
                        continue;
                    }

                    var hasValidPrefix = ValidPrefixes.Any(prefix =>
                        propertyName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

                    if (!hasValidPrefix)
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
