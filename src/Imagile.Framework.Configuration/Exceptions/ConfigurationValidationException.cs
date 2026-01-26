namespace Imagile.Framework.Configuration.Exceptions;

/// <summary>
/// Exception thrown when configuration validation fails.
/// </summary>
/// <remarks>
/// This exception aggregates all validation errors found during recursive configuration validation.
/// The message contains a formatted list of all validation failures with their paths and error messages.
/// </remarks>
public class ConfigurationValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationValidationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ConfigurationValidationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationValidationException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConfigurationValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
