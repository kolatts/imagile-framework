using Imagile.EntityFrameworkCore.Tests.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Imagile.EntityFrameworkCore.Tests.Rules;

/// <summary>
/// Rule that validates Enum properties (except keys) end with "Type".
/// </summary>
public class EnumsMustEndWithTypeRule : IConventionRule
{
    public string Name => "Enum properties must end with Type";

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
                if (options.IsExcluded<EnumsMustEndWithTypeRule>(entityName))
                {
                    continue;
                }

                foreach (var property in entityType.GetProperties())
                {
                    var clrType = property.ClrType;

                    // Check if it's an enum or nullable enum
                    var isEnum = clrType.IsEnum ||
                                 (Nullable.GetUnderlyingType(clrType)?.IsEnum ?? false);

                    if (!isEnum)
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
                    if (options.IsExcluded<EnumsMustEndWithTypeRule>(entityName, propertyName))
                    {
                        continue;
                    }

                    // Allow properties ending with "Id" (some cases where DB relationship is not desired)
                    if (propertyName.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!propertyName.EndsWith("Type", StringComparison.OrdinalIgnoreCase))
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
