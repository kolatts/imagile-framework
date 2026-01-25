using Imagile.EntityFrameworkCore.Tests.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Imagile.EntityFrameworkCore.Tests.Rules;

/// <summary>
/// Rule that validates no properties have type bool? (nullable boolean).
/// </summary>
public class ProhibitNullableBooleansRule : IConventionRule
{
    public string Name => "Properties cannot be nullable booleans";

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
                if (options.IsExcluded<ProhibitNullableBooleansRule>(entityName))
                {
                    continue;
                }

                foreach (var property in entityType.GetProperties())
                {
                    var propertyName = property.Name;

                    // Check if property is excluded
                    if (options.IsExcluded<ProhibitNullableBooleansRule>(entityName, propertyName))
                    {
                        continue;
                    }

                    // Check if the property is a nullable boolean
                    if (property.ClrType == typeof(bool?))
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
