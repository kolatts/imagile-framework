namespace Imagile.Framework.EntityFrameworkCore.Interfaces;

/// <summary>
/// Marks an entity as tenant-scoped for multi-tenant data isolation.
/// </summary>
/// <typeparam name="TTenantKey">The type of the tenant identifier (e.g., int, Guid, string)</typeparam>
/// <remarks>
/// <para>
/// Entities implementing this interface are automatically filtered by tenant using global query filters.
/// The TenantId is populated from IAuditContextProvider.TenantId on insert.
/// </para>
/// <para>
/// This interface is separate from the audit interfaces - an entity can be tenant-scoped without
/// being audited, or fully audited without being tenant-scoped. Implement both interfaces when needed.
/// </para>
/// <para>
/// <strong>Security Note:</strong> Using IgnoreQueryFilters() disables ALL global filters including
/// tenant isolation. In EF Core 10.0, use named filters to disable soft delete while keeping
/// tenant filtering active.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Tenant-scoped and audited
/// public class Project : IAuditableEntity&lt;int&gt;, ITenantEntity&lt;int&gt;
/// {
///     public int Id { get; set; }
///     public string Name { get; set; } = string.Empty;
///
///     // ITenantEntity
///     public int TenantId { get; set; }
///
///     // IAuditableEntity properties...
/// }
///
/// // Tenant-scoped only (no audit)
/// public class TenantSettings : ITenantEntity&lt;int&gt;
/// {
///     public int Id { get; set; }
///     public int TenantId { get; set; }
///     public string Settings { get; set; } = "{}";
/// }
/// </code>
/// </example>
public interface ITenantEntity<TTenantKey>
{
    /// <summary>
    /// Gets or sets the tenant identifier this entity belongs to.
    /// </summary>
    /// <remarks>
    /// Automatically populated from IAuditContextProvider.TenantId on insert.
    /// Should not be modified after initial creation to maintain data isolation.
    /// Global query filters automatically restrict queries to the current tenant.
    /// </remarks>
    TTenantKey TenantId { get; set; }
}
