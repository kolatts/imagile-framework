using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Imagile.Framework.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Imagile.Framework.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for <see cref="ModelBuilder"/> to configure common EF Core conventions.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Sets a default decimal precision and scale for all decimal properties that do not already have
    /// a <see cref="ColumnAttribute"/> or <see cref="PrecisionAttribute"/> applied.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <param name="defaultPrecision">The total number of significant digits (e.g., 18).</param>
    /// <param name="defaultScale">The number of digits to the right of the decimal point (e.g., 2).</param>
    /// <remarks>
    /// <para>
    /// Call this in <c>OnModelCreating</c> to avoid EF Core warnings about decimal precision
    /// without having to annotate every decimal property individually.
    /// </para>
    /// <para>
    /// Properties that already have a <see cref="ColumnAttribute"/> or <see cref="PrecisionAttribute"/>
    /// are skipped so explicit configurations are always respected.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     base.OnModelCreating(modelBuilder);
    ///     modelBuilder.SetDecimalDefaultPrecisionAndScale(18, 2);
    /// }
    /// </code>
    /// </example>
    public static void SetDecimalDefaultPrecisionAndScale(
        this ModelBuilder modelBuilder,
        short defaultPrecision,
        short defaultScale)
    {
        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(x => x.GetProperties())
                     .Where(x => x.ClrType == typeof(decimal) || x.ClrType == typeof(decimal?)))
        {
            var columnAttribute = property.PropertyInfo?.GetCustomAttributes<ColumnAttribute>().FirstOrDefault();
            var precisionAttribute = property.PropertyInfo?.GetCustomAttributes<PrecisionAttribute>().FirstOrDefault();

            if (columnAttribute == null && precisionAttribute == null)
            {
                property.SetPrecision(defaultPrecision);
                property.SetScale(defaultScale);
            }
        }
    }

    /// <summary>
    /// Registers the <see cref="EnumLookupValue"/> entity type and configures its table, composite
    /// primary key, column lengths, and non-unicode column types in the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <remarks>
    /// <para>
    /// Call this in <c>OnModelCreating</c> to opt into enum lookup value support. After calling
    /// this method and applying migrations, populate the table by calling
    /// <see cref="DbContextExtensions.SeedEnumLookupValues"/> at startup or in a migration seed step.
    /// </para>
    /// <para>
    /// This method is automatically called by <c>ImagileDbContext</c> when
    /// <c>EnableEnumLookupValues</c> is overridden to return <c>true</c>.
    /// Any <see cref="Microsoft.EntityFrameworkCore.DbContext"/> can also call this directly.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     base.OnModelCreating(modelBuilder);
    ///     modelBuilder.ConfigureEnumLookupValues();
    /// }
    ///
    /// // At runtime, after EnsureCreated() / Migrate():
    /// context.SeedEnumLookupValues();
    /// </code>
    /// </example>
    public static void ConfigureEnumLookupValues(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EnumLookupValue>(entity =>
        {
            entity.ToTable("__EnumLookupValues");

            entity.HasKey(e => new { e.TableName, e.ColumnName, e.Value });

            entity.Property(e => e.TableName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .IsRequired();

            entity.Property(e => e.ColumnName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .IsRequired();

            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsUnicode(false)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .IsUnicode(false)
                .IsRequired();

            entity.HasIndex(e => new { e.TableName, e.ColumnName })
                .HasDatabaseName("IX_EnumLookupValues_TableName_ColumnName");
        });
    }
}
