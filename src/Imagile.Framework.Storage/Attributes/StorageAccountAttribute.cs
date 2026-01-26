namespace Imagile.Framework.Storage.Attributes;

/// <summary>
/// Specifies which storage account a queue message or blob container type should use.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute to associate queue message or blob container types with specific
/// storage accounts when your application uses multiple Azure Storage accounts.
/// </para>
/// <para>
/// The <see cref="Name"/> property corresponds to the name used during DI registration
/// with <c>AddStorageAbstractions()</c>. Types without this attribute use the default
/// (unnamed) storage account.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Primary storage account (default)
/// public class UserNotificationMessage : IQueueMessage
/// {
///     public static string DefaultQueueName => "user-notifications";
///     public int UserId { get; set; }
/// }
///
/// // Archive storage account (named)
/// [StorageAccount("archive")]
/// public class AuditLogContainer : IBlobContainer
/// {
///     public static string DefaultContainerName => "audit-logs";
/// }
///
/// // DI registration
/// services.AddStorageAbstractions(config =>
/// {
///     config.AddStorageAccount(connectionString); // Default account
///     config.AddStorageAccount("archive", archiveConnectionString); // Named account
///     config.ScanAssemblies(typeof(UserNotificationMessage).Assembly);
/// });
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class StorageAccountAttribute : Attribute
{
    /// <summary>
    /// Gets the storage account name.
    /// </summary>
    /// <value>
    /// The name used during DI registration to identify this storage account.
    /// </value>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StorageAccountAttribute"/> class.
    /// </summary>
    /// <param name="name">
    /// The storage account name used during DI registration.
    /// This name must match the name used in <c>AddStorageAccount("name", ...)</c>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is null, empty, or whitespace.
    /// </exception>
    public StorageAccountAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
}
