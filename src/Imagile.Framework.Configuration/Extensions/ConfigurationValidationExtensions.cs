using System.ComponentModel.DataAnnotations;
using System.Text;
using Imagile.Framework.Configuration.Exceptions;

namespace Imagile.Framework.Configuration.Extensions;

/// <summary>
/// Provides extension methods for validating configuration objects using data annotations.
/// </summary>
/// <remarks>
/// These extensions enable recursive validation of configuration objects, ensuring that all data annotation
/// constraints are satisfied throughout the entire object graph. This is particularly useful for validating
/// strongly-typed configuration objects bound from IConfiguration.
/// </remarks>
public static class ConfigurationValidationExtensions
{
    /// <summary>
    /// Validates an object and its nested properties recursively using data annotations.
    /// </summary>
    /// <param name="obj">The object to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> is null.</exception>
    /// <exception cref="ConfigurationValidationException">
    /// Thrown when validation fails. The exception message contains all validation errors found throughout
    /// the object graph, formatted with their property paths.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method performs deep validation by recursing into complex properties (non-value types, non-strings)
    /// that belong to the same assembly as the root object. This ensures that nested configuration sections
    /// are fully validated while avoiding infinite recursion on framework types.
    /// </para>
    /// <para>
    /// All validation errors are aggregated and thrown together in a single exception, providing complete
    /// diagnostics rather than failing on the first error encountered.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class DatabaseSettings
    /// {
    ///     [Required]
    ///     public string ConnectionString { get; set; } = string.Empty;
    ///
    ///     [Range(1, 100)]
    ///     public int MaxConnections { get; set; }
    ///
    ///     [Required]
    ///     public RetrySettings Retry { get; set; } = null!;
    /// }
    ///
    /// public class RetrySettings
    /// {
    ///     [Range(0, 10)]
    ///     public int MaxAttempts { get; set; }
    /// }
    ///
    /// // Validate configuration after binding
    /// var settings = configuration.Get&lt;DatabaseSettings&gt;()!;
    /// settings.ValidateRecursively(); // Throws if any Required or Range constraint violated
    /// </code>
    /// </example>
    public static void ValidateRecursively(this object obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        var results = new List<ValidationResult>();
        if (!TryValidateRecursive(obj, results))
        {
            var errorMessage = BuildValidationErrorMessage(results);
            throw new ConfigurationValidationException(errorMessage);
        }
    }

    /// <summary>
    /// Attempts to validate an object and its nested properties recursively using data annotations.
    /// </summary>
    /// <param name="obj">The object to validate.</param>
    /// <param name="results">A list to populate with validation results. Any validation errors will be added to this list.</param>
    /// <returns><c>true</c> if validation succeeds; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> or <paramref name="results"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// This method performs deep validation by recursing into complex properties (non-value types, non-strings)
    /// that belong to the same assembly as the root object. Nested property paths are preserved in the
    /// <see cref="ValidationResult.MemberNames"/> collection using dot notation (e.g., "Parent.Child.Property").
    /// </para>
    /// <para>
    /// Unlike <see cref="ValidateRecursively"/>, this method does not throw an exception. Instead, it returns
    /// a boolean indicating success or failure and populates the results list with all validation errors found.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var settings = configuration.Get&lt;DatabaseSettings&gt;()!;
    /// var results = new List&lt;ValidationResult&gt;();
    ///
    /// if (!settings.TryValidateRecursive(results))
    /// {
    ///     foreach (var error in results)
    ///     {
    ///         var memberName = error.MemberNames.FirstOrDefault() ?? "Unknown";
    ///         Console.WriteLine($"{memberName}: {error.ErrorMessage}");
    ///     }
    /// }
    /// </code>
    /// </example>
    public static bool TryValidateRecursive(this object obj, List<ValidationResult> results)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(results);

        var context = new ValidationContext(obj);
        bool isValid = Validator.TryValidateObject(obj, context, results, validateAllProperties: true);

        foreach (var property in obj.GetType().GetProperties())
        {
            var value = property.GetValue(obj);

            if (value != null &&
                !property.PropertyType.IsValueType &&
                property.PropertyType != typeof(string) &&
                IsApplicationType(property.PropertyType, obj.GetType()))
            {
                var nestedResults = new List<ValidationResult>();
                if (!TryValidateRecursive(value, nestedResults))
                {
                    isValid = false;
                    foreach (var validationResult in nestedResults)
                    {
                        results.Add(new ValidationResult(
                            validationResult.ErrorMessage,
                            validationResult.MemberNames.Select(x => $"{property.Name}.{x}")));
                    }
                }
            }
        }

        return isValid;
    }

    /// <summary>
    /// Determines if a property type should be recursively validated.
    /// </summary>
    /// <param name="propertyType">The type of the property to check.</param>
    /// <param name="rootType">The type of the root object being validated.</param>
    /// <returns><c>true</c> if the property type is from the same assembly as the root type; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// This method ensures that only application-specific types are recursively validated, preventing
    /// infinite recursion on framework types like collections or other complex types.
    /// </remarks>
    private static bool IsApplicationType(Type propertyType, Type rootType)
    {
        return propertyType.Assembly == rootType.Assembly;
    }

    /// <summary>
    /// Builds a formatted error message from a list of validation results.
    /// </summary>
    /// <param name="results">The validation results to format.</param>
    /// <returns>A formatted string containing all validation errors with their property paths.</returns>
    /// <remarks>
    /// The error message format is:
    /// <code>
    /// The following errors were found in the configuration:
    ///   PropertyName: Error message
    ///   Parent.Child.PropertyName: Error message
    /// </code>
    /// </remarks>
    private static string BuildValidationErrorMessage(List<ValidationResult> results)
    {
        var errorMessages = new StringBuilder();
        errorMessages.AppendLine("The following errors were found in the configuration:");

        foreach (var validationResult in results)
        {
            var memberName = validationResult.MemberNames.FirstOrDefault() ?? string.Empty;
            errorMessages.AppendLine($"  {memberName}: {validationResult.ErrorMessage}");
        }

        return errorMessages.ToString();
    }
}
