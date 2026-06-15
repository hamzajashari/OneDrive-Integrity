namespace OneDriveIntegrityLab.Configuration;

/// <summary>
/// Strongly-typed experiment configuration. Values are loaded from
/// appsettings.json and can be overridden by environment variables
/// (e.g. ONEDRIVE_CLIENT_ID) so that secrets never need to live in source control.
/// </summary>
public sealed class ExperimentSettings
{
    /// <summary>
    /// Application (client) ID of the Azure AD app registration used for
    /// Microsoft Graph authentication.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Tenant to authenticate against. For personal Microsoft accounts use
    /// "consumers"; for work/school accounts use "organizations" or a tenant ID;
    /// "common" supports both.
    /// </summary>
    public string TenantId { get; set; } = "consumers";

    /// <summary>
    /// Name of the OneDrive folder the experiment uploads test files into.
    /// Created automatically if it does not already exist.
    /// </summary>
    public string RemoteFolder { get; set; } = "IntegrityLab";

    /// <summary>
    /// Local directory where downloaded files are written for verification.
    /// </summary>
    public string DownloadDirectory { get; set; } = "downloads";

    /// <summary>
    /// Graph permission scopes requested during sign-in.
    /// </summary>
    public string[] Scopes { get; set; } = { "Files.ReadWrite", "User.Read" };

    /// <summary>
    /// Validates that required settings are present, throwing a clear error
    /// if the client ID has not been configured.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ClientId) ||
            ClientId.Equals("YOUR_CLIENT_ID_HERE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "No Azure AD Client ID configured. Set it in appsettings.json " +
                "or via the ONEDRIVE_CLIENT_ID environment variable.");
        }
    }
}
