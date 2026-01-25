using Imagile.EntityFrameworkCore.Tests.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Imagile.EntityFrameworkCore.Tests.Rules;

/// <summary>
/// Rule that validates DateTime and DateTimeOffset properties end with "Date".
/// </summary>
public class DateTimesMustEndWithDateRule : IConventionRule
{
    public string Name => "DateTime properties must end with Date";

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
                if (options.IsExcluded<DateTimesMustEndWithDateRule>(entityName))
                {
                    continue;
                }

                foreach (var property in entityType.GetProperties())
                {
                    var clrType = property.ClrType;

                    // Only check DateTime and DateTimeOffset properties
                    if (clrType != typeof(DateTime) &&
                        clrType != typeof(DateTime?) &&
                        clrType != typeof(DateTimeOffset) &&
                        clrType != typeof(DateTimeOffset?))
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
                    if (options.IsExcluded<DateTimesMustEndWithDateRule>(entityName, propertyName))
                    {
                        continue;
                    }

                    if (!propertyName.EndsWith("Date", StringComparison.OrdinalIgnoreCase))
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
