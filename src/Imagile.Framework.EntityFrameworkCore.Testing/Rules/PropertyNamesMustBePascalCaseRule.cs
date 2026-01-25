using Humanizer;
using Imagile.Framework.EntityFrameworkCore.Testing.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Imagile.Framework.EntityFrameworkCore.Testing.Rules;

/// <summary>
/// Rule that validates property names are PascalCase.
/// </summary>
public class PropertyNamesMustBePascalCaseRule : IConventionRule
{
    public string Name => "Property names must be PascalCase";

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
                if (options.IsExcluded<PropertyNamesMustBePascalCaseRule>(entityName))
                {
                    continue;
                }

                foreach (var property in entityType.GetProperties())
                {
                    var propertyName = property.Name;

                    // Check if property is excluded
                    if (options.IsExcluded<PropertyNamesMustBePascalCaseRule>(entityName, propertyName))
                    {
                        continue;
                    }

                    var pascalPropertyName = propertyName.Pascalize();

                    if (propertyName != pascalPropertyName)
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
