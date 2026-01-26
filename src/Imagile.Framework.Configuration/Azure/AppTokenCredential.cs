using Azure.Core;
using Azure.Identity;

namespace Imagile.Framework.Configuration.Azure;

/// <summary>
/// Provides Azure authentication using a chained credential strategy optimized for both cloud and local development environments.
/// </summary>
/// <remarks>
/// <para>
/// This credential automatically selects the appropriate authentication method based on the environment:
/// </para>
/// <list type="table">
/// <listheader>
/// <term>Environment</term>
/// <description>Credential Chain</description>
/// </listheader>
/// <item>
/// <term>Cloud (Azure)</term>
/// <description>
/// <list type="bullet">
/// <item><description><see cref="WorkloadIdentityCredential"/> - Azure Kubernetes Service (AKS) workload identity</description></item>
/// <item><description><see cref="ManagedIdentityCredential"/> - Azure App Service, Container Apps, Functions, VMs</description></item>
/// </list>
/// </description>
/// </item>
/// <item>
/// <term>Local Development</term>
/// <description>
/// <list type="bullet">
/// <item><description><see cref="AzureCliCredential"/> - Azure CLI authentication (fastest, cross-platform)</description></item>
/// <item><description><see cref="VisualStudioCredential"/> - Visual Studio account</description></item>
/// </list>
/// </description>
/// </item>
/// </list>
/// <para>
/// The credential chain tries each method in order until one succeeds. This eliminates the need for
/// separate configuration or code changes when moving between local development and cloud environments.
/// </para>
/// <para>
/// <strong>WorkloadIdentity Support:</strong> For Azure Kubernetes Service (AKS) deployments, WorkloadIdentityCredential
/// is attempted first in cloud environments. This aligns with modern AKS security best practices using federated
/// identity credentials instead of traditional managed identity.
/// </para>
/// </remarks>
/// <example>
/// <para><strong>Basic Usage (Local Development):</strong></para>
/// <code>
/// // No parameters needed for local development - uses Azure CLI or Visual Studio credentials
/// var credential = new AppTokenCredential();
///
/// // Use with Azure SDK clients
/// var secretClient = new SecretClient(
///     new Uri("https://my-vault.vault.azure.net/"),
///     credential
/// );
/// </code>
/// <para><strong>Cloud Usage (User-Assigned Managed Identity):</strong></para>
/// <code>
/// // Provide the managed identity client ID from configuration
/// var managedIdentityClientId = configuration["AzureManagedIdentityClientId"];
/// var credential = new AppTokenCredential(managedIdentityClientId);
///
/// // Use with Azure SDK clients
/// var blobClient = new BlobServiceClient(
///     new Uri("https://mystorageaccount.blob.core.windows.net/"),
///     credential
/// );
/// </code>
/// </example>
public class AppTokenCredential : TokenCredential
{
    private readonly TokenCredential _tokenCredential;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppTokenCredential"/> class.
    /// </summary>
    /// <param name="managedIdentityClientId">
    /// Optional client ID of the user-assigned managed identity to use in cloud environments.
    /// If provided (not null or whitespace), uses <see cref="WorkloadIdentityCredential"/> and <see cref="ManagedIdentityCredential"/>.
    /// If not provided, uses <see cref="AzureCliCredential"/> and <see cref="VisualStudioCredential"/> for local development.
    /// </param>
    public AppTokenCredential(string? managedIdentityClientId = null)
    {
        _tokenCredential = !string.IsNullOrWhiteSpace(managedIdentityClientId) ?
                    new ChainedTokenCredential(new WorkloadIdentityCredential(), new ManagedIdentityCredential(clientId: managedIdentityClientId)) :
                    new ChainedTokenCredential(new AzureCliCredential(), new VisualStudioCredential());
    }

    /// <summary>
    /// Gets an <see cref="AccessToken"/> for the specified set of scopes asynchronously.
    /// </summary>
    /// <param name="requestContext">The <see cref="TokenRequestContext"/> with authentication information.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use.</param>
    /// <returns>A valid <see cref="AccessToken"/>.</returns>
    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return _tokenCredential.GetTokenAsync(requestContext, cancellationToken);
    }

    /// <summary>
    /// Gets an <see cref="AccessToken"/> for the specified set of scopes synchronously.
    /// </summary>
    /// <param name="requestContext">The <see cref="TokenRequestContext"/> with authentication information.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use.</param>
    /// <returns>A valid <see cref="AccessToken"/>.</returns>
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return _tokenCredential.GetToken(requestContext, cancellationToken);
    }
}
