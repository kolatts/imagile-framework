using Imagile.Framework.EntityFrameworkCore.Testing.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Imagile.Framework.EntityFrameworkCore.Testing.Rules;

/// <summary>
/// Rule that validates string properties are not nullable unless explicitly configured as required.
/// </summary>
public class ProhibitNullableStringsRule : IConventionRule
{
    public string Name => "String properties cannot be nullable";

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
                if (options.IsExcluded<ProhibitNullableStringsRule>(entityName))
                {
                    continue;
                }

                foreach (var property in entityType.GetProperties())
                {
                    // Only check string properties
                    if (property.ClrType != typeof(string))
                    {
                        continue;
                    }

                    var propertyName = property.Name;

                    // Check if property is excluded
                    if (options.IsExcluded<ProhibitNullableStringsRule>(entityName, propertyName))
                    {
                        continue;
                    }

                    // Check if the property is nullable
                    if (property.IsNullable)
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
