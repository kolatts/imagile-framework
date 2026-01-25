using Humanizer;
using Imagile.EntityFrameworkCore.Tests.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Imagile.EntityFrameworkCore.Tests.Rules;

/// <summary>
/// Rule that validates table names are PascalCase.
/// </summary>
public class TableNamesMustBePascalCaseRule : IConventionRule
{
    public string Name => "Table names must be PascalCase";

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
                if (options.IsExcluded<TableNamesMustBePascalCaseRule>(entityName))
                {
                    continue;
                }

                var tableName = entityType.GetTableName();
                if (tableName is null || tableName.Length == 0)
                {
                    continue;
                }

                // Skip if table name doesn't start with a letter
                if (!char.IsAsciiLetter(tableName[0]))
                {
                    continue;
                }

                var pascalTableName = tableName.Pascalize();

                if (tableName != pascalTableName)
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
