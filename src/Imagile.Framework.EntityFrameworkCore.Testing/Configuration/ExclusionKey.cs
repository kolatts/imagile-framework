namespace Imagile.Framework.EntityFrameworkCore.Testing.Configuration;

/// <summary>
/// Represents a key for excluding entities or properties from convention tests.
/// </summary>
/// <param name="EntityName">The name of the entity.</param>
/// <param name="PropertyName">The optional name of the property.</param>
public record ExclusionKey(string EntityName, string? PropertyName = null)
{
    /// <summary>
    /// Creates an exclusion key for an entire entity.
    /// </summary>
    /// <param name="entityName">The name of the entity to exclude.</param>
    /// <returns>An exclusion key for the entity.</returns>
    public static ExclusionKey ForEntity(string entityName)
    {
        return new ExclusionKey(entityName);
    }

    /// <summary>
    /// Creates an exclusion key for a specific property of an entity.
    /// </summary>
    /// <param name="entityName">The name of the entity.</param>
    /// <param name="propertyName">The name of the property to exclude.</param>
    /// <returns>An exclusion key for the property.</returns>
    public static ExclusionKey ForProperty(string entityName, string propertyName)
    {
        return new ExclusionKey(entityName, propertyName);
    }
}
