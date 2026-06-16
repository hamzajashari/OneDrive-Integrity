using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession;
using OneDriveIntegrityLab.Logging;

namespace OneDriveIntegrityLab.Storage;

/// <summary>
/// Encapsulates all OneDrive interactions performed through Microsoft Graph:
/// resolving the user's drive, ensuring the target folder exists, and uploading
/// and downloading files. Small files use a single PUT; files larger than the
/// 4 MB simple-upload ceiling automatically switch to a resumable upload session.
/// </summary>
public sealed class OneDriveService
{
    // Graph allows a simple content PUT up to 4 MB; above that a session is required.
    private const long SimpleUploadLimitBytes = 4L * 1024 * 1024;
    private const int UploadChunkSize = 320 * 1024; // must be a multiple of 320 KiB

    private readonly GraphServiceClient _graph;
    private readonly LabLogger _logger;
    private string? _driveId;

    public OneDriveService(GraphServiceClient graph, LabLogger logger)
    {
        _graph = graph;
        _logger = logger;
    }

    /// <summary>Resolves and caches the signed-in user's default drive id.</summary>
    public async Task<string> GetDriveIdAsync()
    {
        if (_driveId is not null) return _driveId;

        var drive = await _graph.Me.Drive.GetAsync()
            ?? throw new InvalidOperationException("Could not resolve the user's OneDrive.");

        _driveId = drive.Id!;
        _logger.Info($"Resolved OneDrive (driveId: {_driveId}).");
        return _driveId;
    }

    /// <summary>
    /// Ensures the given top-level folder exists, creating it if necessary.
    /// Idempotent: a second call with the same name is a no-op.
    /// </summary>
    public async Task EnsureFolderAsync(string folderName)
    {
        var driveId = await GetDriveIdAsync();
        try
        {
            await _graph.Drives[driveId].Items["root"].ItemWithPath(folderName).GetAsync();
            _logger.Info($"Folder '{folderName}' already exists.");
        }
        catch (ODataError ex) when (ex.ResponseStatusCode == 404)
        {
            var folder = new DriveItem
            {
                Name = folderName,
                Folder = new Folder(),
                AdditionalData = new Dictionary<string, object>
                {
                    ["@microsoft.graph.conflictBehavior"] = "fail"
                }
            };
            await _graph.Drives[driveId].Items["root"].Children.PostAsync(folder);
            _logger.Success($"Created folder '{folderName}'.");
        }
    }

    /// <summary>
    /// Uploads a local file to {folder}/{fileName} and returns the resulting
    /// DriveItem id. Chooses simple vs. resumable upload based on file size.
    /// </summary>
    public async Task<string> UploadAsync(string localPath, string folderName)
    {
        var driveId = await GetDriveIdAsync();
        var fileName = Path.GetFileName(localPath);
        var remotePath = $"{folderName}/{fileName}";
        var fileSize = new FileInfo(localPath).Length;

        await using var stream = File.OpenRead(localPath);

        DriveItem? uploaded = fileSize <= SimpleUploadLimitBytes
            ? await SimpleUploadAsync(driveId, remotePath, stream)
            : await ResumableUploadAsync(driveId, remotePath, stream);

        var id = uploaded?.Id
            ?? throw new InvalidOperationException("Upload returned no DriveItem id.");

        _logger.Success($"Uploaded '{fileName}' ({fileSize:N0} bytes) -> itemId {id}.");
        return id;
    }

    private async Task<DriveItem?> SimpleUploadAsync(string driveId, string remotePath, Stream stream)
    {
        _logger.Step($"Simple upload (<= 4 MB) to '{remotePath}'.");
        return await _graph.Drives[driveId].Items["root"]
            .ItemWithPath(remotePath)
            .Content
            .PutAsync(stream);
    }

    private async Task<DriveItem?> ResumableUploadAsync(string driveId, string remotePath, Stream stream)
    {
        _logger.Step($"Resumable upload session (> 4 MB) to '{remotePath}'.");

        var sessionBody = new CreateUploadSessionPostRequestBody
        {
            Item = new DriveItemUploadableProperties
            {
                AdditionalData = new Dictionary<string, object>
                {
                    ["@microsoft.graph.conflictBehavior"] = "replace"
                }
            }
        };

        var uploadSession = await _graph.Drives[driveId].Items["root"]
            .ItemWithPath(remotePath)
            .CreateUploadSession
            .PostAsync(sessionBody);

        var uploadTask = new LargeFileUploadTask<DriveItem>(
            uploadSession, stream, UploadChunkSize, _graph.RequestAdapter);

        var result = await uploadTask.UploadAsync();
        if (!result.UploadSucceeded)
            throw new InvalidOperationException("Resumable upload did not complete successfully.");

        return result.ItemResponse;
    }

    /// <summary>Downloads a file by its DriveItem id to the given local path.</summary>
    public async Task DownloadAsync(string itemId, string localDestinationPath)
    {
        var driveId = await GetDriveIdAsync();
        _logger.Step($"Downloading itemId {itemId} -> '{localDestinationPath}'.");

        await using var remoteStream = await _graph.Drives[driveId].Items[itemId].Content.GetAsync()
            ?? throw new InvalidOperationException("Download returned an empty stream.");

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(localDestinationPath))!);
        await using var fileStream = File.Create(localDestinationPath);
        await remoteStream.CopyToAsync(fileStream);

        _logger.Success($"Downloaded to '{localDestinationPath}'.");
    }
}
