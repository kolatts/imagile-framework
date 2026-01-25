using System.Linq.Expressions;
using Imagile.Framework.Core.Interfaces;
using Imagile.Framework.EntityFrameworkCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Imagile.Framework.EntityFrameworkCore.Configuration;

/// <summary>
/// Helper class for configuring audit-related model building options.
/// </summary>
public static class AuditConfiguration
{
    /// <summary>
    /// Configures soft delete query filters for all entities implementing IAuditableEntity.
    /// </summary>
    /// <typeparam name="TUserKey">The type of the user identifier.</typeparam>
    /// <param name="modelBuilder">The model builder.</param>
    /// <remarks>
    /// <para>
    /// Adds a global query filter that excludes entities where IsDeleted = true.
    /// Use IgnoreQueryFilters() to see soft-deleted entities.
    /// </para>
    /// <para>
    /// Call this in your DbContext's OnModelCreating method.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     base.OnModelCreating(modelBuilder);
    ///     AuditConfiguration.ConfigureSoftDeleteFilter&lt;int&gt;(modelBuilder);
    /// }
    /// </code>
    /// </example>
    public static void ConfigureSoftDeleteFilter<TUserKey>(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (ImplementsInterface(entityType.ClrType, typeof(IAuditableEntity<>)))
            {
                ConfigureSoftDeleteFilterForEntity(modelBuilder, entityType);
            }
        }
    }

    /// <summary>
    /// Configures tenant query filters for all entities implementing ITenantEntity.
    /// </summary>
    /// <typeparam name="TTenantKey">The type of the tenant identifier.</typeparam>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="getCurrentTenantId">Expression to get the current tenant ID.</param>
    /// <remarks>
    /// <para>
    /// Adds a global query filter that restricts entities to the current tenant.
    /// Use IgnoreQueryFilters() to see entities from all tenants (with caution for security).
    /// </para>
    /// <para>
    /// <strong>Important:</strong> The getCurrentTenantId expression must reference a field or property
    /// on the DbContext that EF Core can evaluate at query time.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // In your DbContext:
    /// private readonly IAuditContextProvider&lt;int, int&gt; _auditContext;
    ///
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     base.OnModelCreating(modelBuilder);
    ///     AuditConfiguration.ConfigureTenantFilter&lt;int&gt;(modelBuilder, () => _auditContext.TenantId);
    /// }
    /// </code>
    /// </example>
    public static void ConfigureTenantFilter<TTenantKey>(
        ModelBuilder modelBuilder,
        Expression<Func<TTenantKey?>> getCurrentTenantId)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (ImplementsInterface(entityType.ClrType, typeof(ITenantEntity<>)))
            {
                ConfigureTenantFilterForEntity(modelBuilder, entityType, getCurrentTenantId);
            }
        }
    }

    /// <summary>
    /// Configures both soft delete and tenant filters for all applicable entities.
    /// </summary>
    /// <typeparam name="TUserKey">The type of the user identifier.</typeparam>
    /// <typeparam name="TTenantKey">The type of the tenant identifier.</typeparam>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="getCurrentTenantId">Expression to get the current tenant ID.</param>
    /// <remarks>
    /// <para>
    /// Convenience method that applies both filters. Entities implementing both interfaces
    /// will have both filters applied (AND logic).
    /// </para>
    /// </remarks>
    public static void ConfigureAllFilters<TUserKey, TTenantKey>(
        ModelBuilder modelBuilder,
        Expression<Func<TTenantKey?>> getCurrentTenantId)
    {
        ConfigureSoftDeleteFilter<TUserKey>(modelBuilder);
        ConfigureTenantFilter(modelBuilder, getCurrentTenantId);
    }

    /// <summary>
    /// Sets a default precision for all DateTimeOffset columns in the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="precision">The precision (0-7, where 0 = seconds, 7 = 100 nanoseconds).</param>
    /// <remarks>
    /// <para>
    /// Use this to ensure consistent timestamp precision across all audit columns.
    /// Arcoro.One uses precision 0 (second-level) for performance.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// protected override void OnModelCreating(ModelBuilder modelBuilder)
    /// {
    ///     AuditConfiguration.SetDateTimeOffsetPrecision(modelBuilder, 0);
    /// }
    /// </code>
    /// </example>
    public static void SetDateTimeOffsetPrecision(ModelBuilder modelBuilder, int precision)
    {
        if (precision < 0 || precision > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(precision), "Precision must be between 0 and 7.");
        }

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties()
                .Where(p => p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?)))
            {
                property.SetPrecision(precision);
            }
        }
    }

    private static bool ImplementsInterface(Type type, Type interfaceType)
    {
        return type.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType);
    }

    private static void ConfigureSoftDeleteFilterForEntity(ModelBuilder modelBuilder, IMutableEntityType entityType)
    {
        var clrType = entityType.ClrType;
        var parameter = Expression.Parameter(clrType, "e");

        // Get IsDeleted property
        var isDeletedProperty = clrType.GetProperty(nameof(IAuditableEntity<object>.IsDeleted));
        if (isDeletedProperty is null) return;

        var propertyAccess = Expression.Property(parameter, isDeletedProperty);
        var notDeleted = Expression.Not(propertyAccess);
        var lambda = Expression.Lambda(notDeleted, parameter);

        modelBuilder.Entity(clrType).HasQueryFilter(lambda);
    }

    private static void ConfigureTenantFilterForEntity<TTenantKey>(
        ModelBuilder modelBuilder,
        IMutableEntityType entityType,
        Expression<Func<TTenantKey?>> getCurrentTenantId)
    {
        var clrType = entityType.ClrType;
        var parameter = Expression.Parameter(clrType, "e");

        // Get TenantId property
        var tenantIdProperty = clrType.GetProperty(nameof(ITenantEntity<TTenantKey>.TenantId));
        if (tenantIdProperty is null) return;

        var propertyAccess = Expression.Property(parameter, tenantIdProperty);
        var currentTenantId = Expression.Invoke(getCurrentTenantId);
        var equality = Expression.Equal(propertyAccess, currentTenantId);
        var lambda = Expression.Lambda(equality, parameter);

        modelBuilder.Entity(clrType).HasQueryFilter(lambda);
    }
}
