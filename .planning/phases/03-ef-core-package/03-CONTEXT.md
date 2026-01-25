# Phase 3: EF Core Package - Context

**Gathered:** 2026-01-25
**Status:** Ready for planning

<domain>
## Phase Boundary

EF Core audit logging system that automatically tracks entity changes, user context, timestamps, and soft deletes. Developers opt-in via interface implementation, and the system captures changes transparently via interceptors. Multi-tenancy support with configurable filtering.

</domain>

<decisions>
## Implementation Decisions

### Interface Design & Hierarchy

- **Three-level inheritance chain for audit tracking:**
  - `ITimestampedEntity<TTenantKey>` (most basic) - CreatedOn, ModifiedOn as DateTimeOffset
  - `IAuditableEntity<TUserKey, TTenantKey> : ITimestampedEntity<TTenantKey>` - adds CreatedBy, ModifiedBy of type TUserKey
  - `IEntityChangeAuditable<TUserKey, TTenantKey> : IAuditableEntity<TUserKey, TTenantKey>` - adds property-level change tracking

- **Soft delete built into IAuditableEntity:**
  - IsDeleted (bool), DeletedOn (DateTimeOffset?), DeletedBy (TUserKey?) properties
  - Allows hard deletable child collections with deletion summarized to parent

- **Separate tenant interface:**
  - `ITenantEntity<TTenantKey>` as separate concern
  - Entities can implement both ITenantEntity and IAuditableEntity independently

- **Fixed property names:**
  - CreatedOn, ModifiedOn (DateTimeOffset)
  - CreatedBy, ModifiedBy (generic TUserKey)
  - IsDeleted, DeletedOn, DeletedBy

### Interceptor Registration

- **Hybrid approach:**
  - Provide abstract `ImagileDbContext` base class with audit interceptor built-in
  - Also support manual registration for developers who can't/won't inherit from base class

### Change Tracking Scope

- **Track all scalar properties** for IEntityChangeAuditable entities (not navigation properties)
- **[IgnoreAudit] attribute** to exclude specific properties from change tracking (e.g., password hashes, tokens)
- **Separate EntityChange table** for storing change history:
  - Columns: EntityType, EntityId, PropertyName, OldValue, NewValue, ChangedDate, ChangedBy
- **Dual access pattern:**
  - `DbSet<EntityChange>` for bulk queries
  - Extension method `entity.GetChangeHistory()` for convenience

### User Context Integration

- **IAuditContextProvider interface in Core project:**
  - Provides: User ID, Tenant ID, Correlation ID
  - Generic signature: `IAuditContextProvider<TUserKey, TTenantKey>`
  - EF Core package consumes this abstraction

- **Multi-tenancy:**
  - Tenant context flows through IAuditContextProvider
  - Global query filters for ITenantEntity based on current tenant from context provider

### Soft Delete Behavior

- **Configurable cascade per relationship:**
  - Developer chooses whether child entities cascade soft delete or hard delete
  - Supports mixed strategies in same entity graph

- **Configurable global query filtering:**
  - Soft delete filtering (IsDeleted) is configurable at DbContext level
  - Tenant filtering (TenantId) automatic based on current user context
  - Use `IgnoreQueryFilters()` to see soft-deleted or cross-tenant entities

- **Restore() extension method:**
  - Sets IsDeleted=false
  - Clears DeletedOn and DeletedBy
  - Updates ModifiedOn and ModifiedBy with current user/timestamp

### Claude's Discretion

- Exact implementation of cascade configuration (fluent API, attributes, or both)
- EntityChange table schema optimizations (indexing, partitioning)
- Performance optimizations for change tracking
- Correlation ID format and generation strategy

</decisions>

<specifics>
## Specific Ideas

- Inspired by Arcoro.One audit logging patterns (research IEntityChangeAuditable implementation)
- Generic type parameters (TUserKey, TTenantKey) for maximum flexibility across projects
- DateTimeOffset for proper timezone handling in distributed systems
- Interface-based design for clear contracts and testability

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 03-ef-core-package*
*Context gathered: 2026-01-25*
