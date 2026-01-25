namespace Imagile.Framework.Core.Interfaces;

/// <summary>
/// Provides the current user and tenant context for audit logging operations.
/// </summary>
/// <typeparam name="TUserKey">The type of the user identifier (e.g., int, Guid, string)</typeparam>
/// <typeparam name="TTenantKey">The type of the tenant identifier (e.g., int, Guid, string)</typeparam>
/// <remarks>
/// <para>
/// Implement this interface to provide audit context from your authentication system.
/// The EF Core audit interceptor uses this to populate CreatedBy, ModifiedBy, and DeletedBy fields.
/// </para>
/// <para>
/// In ASP.NET Core, typically implemented using IHttpContextAccessor to extract claims from the current user.
/// For background services, implement with explicit user/tenant context passed at job creation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class HttpContextAuditProvider : IAuditContextProvider&lt;int, int&gt;
/// {
///     private readonly IHttpContextAccessor _httpContext;
///
///     public int? UserId => _httpContext.HttpContext?.User.GetUserId();
///     public int? TenantId => _httpContext.HttpContext?.User.GetTenantId();
///     public Guid? CorrelationId => Guid.TryParse(
///         _httpContext.HttpContext?.TraceIdentifier, out var id) ? id : null;
///     public bool IsAuthenticated => UserId.HasValue;
/// }
/// </code>
/// </example>
public interface IAuditContextProvider<TUserKey, TTenantKey>
{
    /// <summary>
    /// Gets the current user's identifier, or null if not authenticated.
    /// </summary>
    TUserKey? UserId { get; }

    /// <summary>
    /// Gets the current tenant's identifier, or null if not in a tenant context.
    /// </summary>
    TTenantKey? TenantId { get; }

    /// <summary>
    /// Gets a correlation identifier for grouping related operations across services.
    /// </summary>
    /// <remarks>
    /// Use this to correlate audit records with distributed tracing.
    /// In ASP.NET Core, this is typically the HttpContext.TraceIdentifier.
    /// </remarks>
    Guid? CorrelationId { get; }

    /// <summary>
    /// Gets a value indicating whether the current context has a valid authenticated user.
    /// </summary>
    /// <remarks>
    /// Returns true when UserId has a value. Some operations (like system jobs) may
    /// have a TenantId but no UserId - check this property to determine if user-level
    /// audit fields should be populated.
    /// </remarks>
    bool IsAuthenticated { get; }
}
