namespace Imagile.Framework.EntityFrameworkCore.Entities;

/// <summary>
/// Specifies the type of change operation recorded in an EntityChange.
/// </summary>
public enum EntityChangeOperation
{
    /// <summary>
    /// A new entity was created (INSERT operation).
    /// </summary>
    Create = 1,

    /// <summary>
    /// An existing entity was modified (UPDATE operation).
    /// </summary>
    Update = 2,

    /// <summary>
    /// An entity was deleted. For soft-deleted entities, this is recorded when IsDeleted changes to true.
    /// </summary>
    Delete = 3
}
