using Imagile.Framework.EntityFrameworkCore.Testing.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Imagile.Framework.EntityFrameworkCore.Testing.Rules;

/// <summary>
/// Rule that validates foreign key properties end with "Id".
/// </summary>
public class ForeignKeysMustEndWithIdRule : IConventionRule
{
    public string Name => "Foreign keys must end with Id";

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
                if (options.IsExcluded<ForeignKeysMustEndWithIdRule>(entityName))
                {
                    continue;
                }

                foreach (var property in entityType.GetProperties())
                {
                    if (!property.IsForeignKey())
                    {
                        continue;
                    }

                    var propertyName = property.Name;

                    // Check if property is excluded
                    if (options.IsExcluded<ForeignKeysMustEndWithIdRule>(entityName, propertyName))
                    {
                        continue;
                    }

                    if (!propertyName.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
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
