using Humanizer;
using Imagile.EntityFrameworkCore.Tests.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Imagile.EntityFrameworkCore.Tests.Rules;

/// <summary>
/// Rule that validates table names are plural.
/// </summary>
public class TableNamesMustBePluralRule : IConventionRule
{
    public string Name => "Table names must be plural";

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
                if (options.IsExcluded<TableNamesMustBePluralRule>(entityName))
                {
                    continue;
                }

                var tableName = entityType.GetTableName();
                if (tableName is null)
                {
                    continue;
                }

                var pluralTableName = tableName.Pluralize(inputIsKnownToBeSingular: false);

                if (!string.Equals(tableName, pluralTableName, StringComparison.OrdinalIgnoreCase))
                {
                    violations.Add(new ConventionViolation(
                        contextName,
                        entityName));
                }
            }
        }

        return violations;
    }
}
