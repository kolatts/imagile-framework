using Imagile.Framework.EntityFrameworkCore.Testing.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Imagile.Framework.EntityFrameworkCore.Testing.Rules;

/// <summary>
/// Rule that validates all string properties have a maximum length configured.
/// </summary>
public class StringsMustHaveMaxLengthRule : IConventionRule
{
    public string Name => "String properties must have max length";

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
                if (options.IsExcluded<StringsMustHaveMaxLengthRule>(entityName))
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
                    if (options.IsExcluded<StringsMustHaveMaxLengthRule>(entityName, propertyName))
                    {
                        continue;
                    }

                    // Check if the property has a max length configured
                    var maxLength = property.GetMaxLength();
                    if (maxLength is null)
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
