using System.ComponentModel;
using Imagile.Framework.Core.Attributes;

namespace Imagile.Framework.Core.Extensions;

/// <summary>
/// Extension methods for working with enums and their declarative attributes.
/// Provides reflection helpers for retrieving attribute values from enum members.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Gets the first attribute of the specified type from an enum value.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type to retrieve</typeparam>
    /// <typeparam name="TEnum">The enum type</typeparam>
    /// <param name="value">The enum value</param>
    /// <returns>The first attribute of the specified type, or null if not found</returns>
    /// <example>
    /// <code>
    /// var category = myEnum.GetAttributeFirstOrDefault&lt;CategoryAttribute, MyEnum&gt;();
    /// </code>
    /// </example>
    public static TAttribute? GetAttributeFirstOrDefault<TAttribute, TEnum>(this TEnum value)
        where TAttribute : Attribute
        where TEnum : Enum
    {
        var type = value.GetType();
        var memberInfos = type.GetMember(value.ToString());
        return memberInfos.FirstOrDefault()
            ?.GetCustomAttributes(typeof(TAttribute), false)
            ?.Cast<TAttribute>()
            .FirstOrDefault();
    }

    /// <summary>
    /// Gets all attributes of the specified type from an enum value.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type to retrieve</typeparam>
    /// <typeparam name="TEnum">The enum type</typeparam>
    /// <param name="value">The enum value</param>
    /// <returns>All attributes of the specified type</returns>
    /// <example>
    /// <code>
    /// var requires = myEnum.GetAttributes&lt;RequiresAttribute&lt;OtherEnum&gt;, MyEnum&gt;();
    /// </code>
    /// </example>
    public static IEnumerable<TAttribute> GetAttributes<TAttribute, TEnum>(this TEnum value)
        where TAttribute : Attribute
        where TEnum : Enum
    {
        var type = value.GetType();
        var memberInfos = type.GetMember(value.ToString());
        return memberInfos.First()
            .GetCustomAttributes(typeof(TAttribute), false)
            .Cast<TAttribute>();
    }

    /// <summary>
    /// Gets the category from a [Category] attribute on an enum value.
    /// </summary>
    /// <typeparam name="TEnum">The enum type</typeparam>
    /// <param name="value">The enum value</param>
    /// <param name="valueIfNone">The value to return if no Category attribute is found</param>
    /// <returns>The category, or the specified default value if no attribute found</returns>
    /// <example>
    /// <code>
    /// var category = myEnum.GetCategory(); // Returns "[None]" if no attribute
    /// var category = myEnum.GetCategory("Unknown"); // Returns "Unknown" if no attribute
    /// </code>
    /// </example>
    public static string? GetCategory<TEnum>(this TEnum value, string? valueIfNone = "[None]")
        where TEnum : Enum
    {
        return GetAttributeFirstOrDefault<Attributes.CategoryAttribute, TEnum>(value)?.Category ?? valueIfNone;
    }

    /// <summary>
    /// Gets the count from a [Count] attribute on an enum value.
    /// </summary>
    /// <typeparam name="TEnum">The enum type</typeparam>
    /// <param name="value">The enum value</param>
    /// <returns>The count value, or null if no attribute found</returns>
    /// <example>
    /// <code>
    /// var count = myEnum.GetCount();
    /// </code>
    /// </example>
    public static int? GetCount<TEnum>(this TEnum value)
        where TEnum : Enum
    {
        return GetAttributeFirstOrDefault<CountAttribute, TEnum>(value)?.Value;
    }

    /// <summary>
    /// Gets the native name from a [NativeName] attribute on an enum value.
    /// </summary>
    /// <typeparam name="TEnum">The enum type</typeparam>
    /// <param name="value">The enum value</param>
    /// <returns>The native name, or the enum value's ToString() if no attribute found</returns>
    /// <example>
    /// <code>
    /// var nativeName = language.GetNativeName(); // e.g., "Español" for Spanish
    /// </code>
    /// </example>
    public static string GetNativeName<TEnum>(this TEnum value)
        where TEnum : Enum
    {
        return GetAttributeFirstOrDefault<NativeNameAttribute, TEnum>(value)?.Name ?? value.ToString();
    }

    /// <summary>
    /// Gets the hosted URLs from a [Hosted] attribute on an enum value.
    /// </summary>
    /// <typeparam name="TEnum">The enum type</typeparam>
    /// <param name="value">The enum value</param>
    /// <returns>A tuple containing (ApiUrl, WebUrl), or (null, null) if no attribute found</returns>
    /// <example>
    /// <code>
    /// var (apiUrl, webUrl) = environment.GetHostedUrls();
    /// </code>
    /// </example>
    public static (string? ApiUrl, string? WebUrl) GetHostedUrls<TEnum>(this TEnum value)
        where TEnum : Enum
    {
        var attribute = GetAttributeFirstOrDefault<HostedAttribute, TEnum>(value);
        return (attribute?.ApiUrl, attribute?.WebUrl);
    }

    /// <summary>
    /// Gets the description from a [Description] attribute on an enum value.
    /// </summary>
    /// <typeparam name="TEnum">The enum type</typeparam>
    /// <param name="value">The enum value</param>
    /// <returns>The description, or the enum value's ToString() if no attribute found</returns>
    /// <example>
    /// <code>
    /// var description = myEnum.GetDescription();
    /// </code>
    /// </example>
    public static string GetDescription<TEnum>(this TEnum value)
        where TEnum : Enum
    {
        return GetAttributeFirstOrDefault<DescriptionAttribute, TEnum>(value)?.Description ?? value.ToString();
    }

    /// <summary>
    /// Gets the associated enum values from an AssociatedAttribute on an enum value.
    /// </summary>
    /// <typeparam name="TEnumSource">The source enum type</typeparam>
    /// <typeparam name="TEnumAssociated">The associated enum type</typeparam>
    /// <param name="value">The enum value to get associations from</param>
    /// <returns>The associated enum values, or empty if no attribute found</returns>
    /// <example>
    /// <code>
    /// var associated = myEnum.GetAssociated&lt;MyEnum, OtherEnum&gt;();
    /// </code>
    /// </example>
    public static IEnumerable<TEnumAssociated> GetAssociated<TEnumSource, TEnumAssociated>(this TEnumSource value)
        where TEnumSource : struct, Enum
        where TEnumAssociated : struct, Enum
    {
        var attribute = value.GetAttributeFirstOrDefault<AssociatedAttribute<TEnumAssociated>, TEnumSource>();
        return attribute?.Associated ?? [];
    }

    /// <summary>
    /// Checks if the enum value fulfills its requirements given a set of available values.
    /// Evaluates both RequireAll and RequireAny semantics based on the RequiresAttribute configuration.
    /// </summary>
    /// <typeparam name="TEnumSource">The source enum type</typeparam>
    /// <typeparam name="TEnumRequired">The required enum type</typeparam>
    /// <param name="value">The enum value to check requirements for</param>
    /// <param name="availableValues">The available values to check against</param>
    /// <returns>True if requirements are fulfilled, false otherwise</returns>
    /// <remarks>
    /// If RequireAll is true, all required values must be present in availableValues.
    /// If RequireAll is false, at least one required value must be present in availableValues.
    /// If no RequiresAttribute is present, returns true.
    /// </remarks>
    /// <example>
    /// <code>
    /// var fulfills = myEnum.FulfillsRequirements(availableEnums);
    /// </code>
    /// </example>
    public static bool FulfillsRequirements<TEnumSource, TEnumRequired>(
        this TEnumSource value,
        IEnumerable<TEnumRequired> availableValues)
        where TEnumSource : struct, Enum
        where TEnumRequired : struct, Enum
    {
        var attributes = value.GetAttributes<RequiresAttribute<TEnumRequired>, TEnumSource>();
        var values = availableValues.ToList();

        foreach (var attribute in attributes)
        {
            var required = attribute?.Required.ToList() ?? [];
            if (required.Count == 0)
            {
                continue;
            }

            if (attribute!.RequireAll && !required.All(values.Contains))
            {
                return false;
            }

            if (!attribute.RequireAll && !required.Any(values.Contains))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets all enum values included by this value, recursively resolving nested includes.
    /// Uses the IncludesAttribute to determine which values are included.
    /// </summary>
    /// <typeparam name="TEnum">The enum type</typeparam>
    /// <param name="value">The enum value</param>
    /// <returns>All included enum values (excluding the value itself)</returns>
    /// <remarks>
    /// This method recursively resolves includes and prevents infinite loops by tracking visited values.
    /// Works with derived attributes that inherit from IncludesAttribute.
    /// </remarks>
    /// <example>
    /// <code>
    /// var included = myEnum.GetIncluded(); // Returns all recursively included values
    /// </code>
    /// </example>
    public static IEnumerable<TEnum> GetIncluded<TEnum>(this TEnum value)
        where TEnum : struct, Enum
    {
        return value.GetIncluded([value]).Where(x => !Equals(x, value));
    }

    /// <summary>
    /// Internal recursive helper for GetIncluded that tracks visited values to prevent infinite loops.
    /// </summary>
    private static HashSet<TEnum> GetIncluded<TEnum>(this TEnum value, HashSet<TEnum> visited)
        where TEnum : struct, Enum
    {
        visited.Add(value);

        var attribute = value.GetAttributeFirstOrDefault<IncludesAttribute<TEnum>, TEnum>();
        var included = attribute?.Included.Where(x => !Equals(x, value)).ToList() ?? [];

        // Remove already visited values to prevent infinite recursion
        included.RemoveAll(visited.Contains);

        // Recursively get included values
        foreach (var include in included)
        {
            include.GetIncluded(visited);
        }

        return visited;
    }

    /// <summary>
    /// Checks if a property has the DoNotUpdate attribute applied.
    /// </summary>
    /// <param name="propertyInfo">The property to check</param>
    /// <returns>True if the property has the DoNotUpdate attribute, false otherwise</returns>
    /// <example>
    /// <code>
    /// var property = typeof(MyClass).GetProperty("MyProperty");
    /// if (property.HasDoNotUpdate())
    /// {
    ///     // Skip updating this property
    /// }
    /// </code>
    /// </example>
    public static bool HasDoNotUpdate(this System.Reflection.PropertyInfo propertyInfo)
    {
        return propertyInfo.GetCustomAttributes(typeof(DoNotUpdateAttribute), false).Any();
    }

    /// <summary>
    /// Adds the specified flag value to the enum, validating that the enum type has the [Flags] attribute.
    /// </summary>
    /// <typeparam name="TEnum">Enum type with [Flags] attribute.</typeparam>
    /// <param name="value">The original enum value.</param>
    /// <param name="flagToAdd">The flag to add.</param>
    /// <returns>The enum value with the flag added.</returns>
    /// <exception cref="ArgumentException">Thrown when the enum type does not have the [Flags] attribute.</exception>
    /// <example>
    /// <code>
    /// var newValue = myFlags.AddFlag(MyFlags.NewFlag);
    /// </code>
    /// </example>
    public static TEnum AddFlag<TEnum>(this TEnum value, TEnum flagToAdd)
        where TEnum : struct, Enum
    {
        var type = typeof(TEnum);
        if (!type.IsDefined(typeof(FlagsAttribute), false))
        {
            throw new ArgumentException($"{type.Name} must have the [Flags] attribute.");
        }

        var result = Convert.ToInt64(value) | Convert.ToInt64(flagToAdd);
        return (TEnum)Enum.ToObject(type, result);
    }

    /// <summary>
    /// Removes the specified flag value from the enum, validating that the enum type has the [Flags] attribute.
    /// </summary>
    /// <typeparam name="TEnum">Enum type with [Flags] attribute.</typeparam>
    /// <param name="value">The original enum value.</param>
    /// <param name="flagToRemove">The flag to remove.</param>
    /// <returns>The enum value with the flag removed.</returns>
    /// <exception cref="ArgumentException">Thrown when the enum type does not have the [Flags] attribute.</exception>
    /// <example>
    /// <code>
    /// var newValue = myFlags.RemoveFlag(MyFlags.OldFlag);
    /// </code>
    /// </example>
    public static TEnum RemoveFlag<TEnum>(this TEnum value, TEnum flagToRemove)
        where TEnum : struct, Enum
    {
        var type = typeof(TEnum);
        if (!type.IsDefined(typeof(FlagsAttribute), false))
        {
            throw new ArgumentException($"{type.Name} must have the [Flags] attribute.");
        }

        var result = Convert.ToInt64(value) & ~Convert.ToInt64(flagToRemove);
        return (TEnum)Enum.ToObject(type, result);
    }
}
