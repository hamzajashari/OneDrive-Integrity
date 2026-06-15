using Azure.Identity;
using Microsoft.Graph;
using OneDriveIntegrityLab.Configuration;
using OneDriveIntegrityLab.Logging;

namespace OneDriveIntegrityLab.Auth;

/// <summary>
/// Builds an authenticated <see cref="GraphServiceClient"/> using the
/// device code flow. Device code is ideal for a console experiment: it needs
/// no redirect URI and works on machines without a browser, prompting the user
/// to sign in on a separate device or tab.
/// </summary>
public sealed class GraphAuthenticator
{
    private readonly ExperimentSettings _settings;
    private readonly LabLogger _logger;

    public GraphAuthenticator(ExperimentSettings settings, LabLogger logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public GraphServiceClient CreateClient()
    {
        _logger.Step("Initialising Microsoft Graph client (device code flow).");

        var options = new DeviceCodeCredentialOptions
        {
            ClientId = _settings.ClientId,
            TenantId = _settings.TenantId,
            // Called when Azure issues the device code; we surface the
            // instructions to the user via the logger.
            DeviceCodeCallback = (code, _) =>
            {
                _logger.Info(code.Message);
                return Task.CompletedTask;
            }
        };

        var credential = new DeviceCodeCredential(options);
        var client = new GraphServiceClient(credential, _settings.Scopes);

        _logger.Success("Graph client created. Awaiting interactive sign-in on first call.");
        return client;
    }
}
