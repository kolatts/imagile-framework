using System.ComponentModel;
using Imagile.Framework.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using EFDbContext = Microsoft.EntityFrameworkCore.DbContext;

namespace Imagile.Framework.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for <see cref="DbContext"/> providing string truncation and enum seeding utilities.
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// Truncates any string property values in Added or Modified entities that exceed their configured
    /// <c>MaxLength</c>, preventing database insert/update failures due to oversized strings.
    /// </summary>
    /// <param name="context">The DbContext whose change tracker will be scanned.</param>
    /// <remarks>
    /// <para>
    /// Call this before <c>SaveChanges</c> to silently truncate strings rather than throwing
    /// a database-level error. Only properties with an explicit <c>MaxLength</c> configured
    /// in the model are affected; properties without a max length constraint are left unchanged.
    /// </para>
    /// <para>
    /// This method is automatically called by <c>ImagileDbContext</c> when
    /// <c>EnableStringTruncation</c> is overridden to return <c>true</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Manual usage in a custom DbContext
    /// public override async Task&lt;int&gt; SaveChangesAsync(CancellationToken ct = default)
    /// {
    ///     this.TruncateStringsToMaxLength();
    ///     return await base.SaveChangesAsync(ct);
    /// }
    /// </code>
    /// </example>
    public static void TruncateStringsToMaxLength(this EFDbContext context)
    {
        var propertiesToTruncate = context.ChangeTracker
            .Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .SelectMany(e => e.Properties)
            .Where(p => p.Metadata.ClrType == typeof(string)
                        && p.Metadata.GetMaxLength().HasValue
                        && p.CurrentValue is string value
                        && !string.IsNullOrEmpty(value)
                        && value.Length > p.Metadata.GetMaxLength()!.Value);

        foreach (var property in propertiesToTruncate)
        {
            var value = (string)property.CurrentValue!;
            var maxLength = property.Metadata.GetMaxLength()!.Value;
            property.CurrentValue = value[..maxLength];
        }
    }

    /// <summary>
    /// Scans all entities in the model for enum and nullable enum properties, then populates
    /// the <c>__EnumLookupValues</c> table with one row per enum member per column.
    /// </summary>
    /// <param name="context">The DbContext whose model will be scanned.</param>
    /// <remarks>
    /// <para>
    /// All existing rows in <c>__EnumLookupValues</c> are deleted before re-seeding.
    /// The <see cref="EnumLookupValue"/> entity type must already be registered in the model
    /// (via <see cref="ModelBuilderExtensions.ConfigureEnumLookupValues"/>) before calling this method.
    /// </para>
    /// <para>
    /// If a <see cref="DescriptionAttribute"/> is applied to an enum member, its value is used
    /// as the <see cref="EnumLookupValue.Description"/>; otherwise the member name is used.
    /// </para>
    /// <para>
    /// Table-per-hierarchy duplicate columns are automatically deduplicated.
    /// </para>
    /// <para>
    /// This method requires a relational database provider (e.g., SQL Server, SQLite).
    /// It will not work with the EF Core InMemory provider.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In Program.cs or a migration seeder
    /// await context.Database.MigrateAsync();
    /// context.SeedEnumLookupValues();
    /// </code>
    /// </example>
    public static void SeedEnumLookupValues(this EFDbContext context)
    {
        context.Set<EnumLookupValue>().ExecuteDelete();
        context.ChangeTracker.Clear();

        var allEnumLookupValues = new List<EnumLookupValue>();
        var allEntities = context.Model.GetEntityTypes().ToList();

        allEntities.ForEach(entityType =>
        {
            var tableMappings = entityType.GetTableMappings().ToList();
            tableMappings.ForEach(tableMapping =>
            {
                var enumColumns = tableMapping.ColumnMappings
                    .Where(c => c.Property.ClrType.IsEnum || c.Property.ClrType.IsNullableEnum())
                    .ToList();

                enumColumns.ForEach(columnMapping =>
                {
                    var enumType = columnMapping.Property.ClrType.IsEnum
                        ? columnMapping.Property.ClrType
                        : columnMapping.Property.ClrType.GetGenericArguments()[0];

                    foreach (var value in enumType.GetEnumValues())
                    {
                        var enumValue = Convert.ChangeType(value, enumType);
                        var enumValueAsString = enumValue.ToString();
                        ArgumentNullException.ThrowIfNull(enumValueAsString);

                        var members = enumType.GetMember(enumValueAsString);
                        var description = members.FirstOrDefault()
                            ?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                            .Cast<DescriptionAttribute>()
                            .FirstOrDefault()
                            ?.Description ?? enumValueAsString;

                        // Deduplicate entries from table-per-hierarchy entities sharing a column
                        if (!allEnumLookupValues.Any(e =>
                                e.TableName == tableMapping.Table.Name &&
                                e.ColumnName == columnMapping.Column.Name &&
                                e.Name == enumValueAsString))
                        {
                            allEnumLookupValues.Add(new EnumLookupValue
                            {
                                TableName = tableMapping.Table.Name,
                                ColumnName = columnMapping.Column.Name,
                                Name = enumValueAsString,
                                Description = description,
                                Value = Convert.ToInt32(enumValue)
                            });
                        }
                    }
                });
            });
        });

        context.AddRange(allEnumLookupValues);
        context.SaveChanges();
    }

    private static bool IsNullableEnum(this Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type);
        return underlyingType is { IsEnum: true };
    }
}
