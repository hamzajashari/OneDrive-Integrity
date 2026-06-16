using System.Diagnostics;
using OneDriveIntegrityLab.Configuration;
using OneDriveIntegrityLab.Integrity;
using OneDriveIntegrityLab.Logging;
using OneDriveIntegrityLab.Storage;

namespace OneDriveIntegrityLab.Experiment;

/// <summary>
/// Orchestrates the full integrity experiment for one or more files:
/// hash the original, upload, download, hash the copy, compare, and record
/// timings. Produces a list of <see cref="ExperimentResult"/> for reporting.
/// </summary>
public sealed class ExperimentRunner
{
    private readonly OneDriveService _oneDrive;
    private readonly ExperimentSettings _settings;
    private readonly LabLogger _logger;

    public ExperimentRunner(OneDriveService oneDrive, ExperimentSettings settings, LabLogger logger)
    {
        _oneDrive = oneDrive;
        _settings = settings;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ExperimentResult>> RunAsync(IEnumerable<string> filePaths)
    {
        _logger.Section("Setup");
        await _oneDrive.EnsureFolderAsync(_settings.RemoteFolder);
        Directory.CreateDirectory(_settings.DownloadDirectory);

        var results = new List<ExperimentResult>();

        foreach (var path in filePaths)
        {
            _logger.Section($"File: {Path.GetFileName(path)}");
            results.Add(await RunSingleAsync(path));
        }

        return results;
    }

    private async Task<ExperimentResult> RunSingleAsync(string localPath)
    {
        var fileName = Path.GetFileName(localPath);
        var fileSize = new FileInfo(localPath).Length;

        // 1. Hash the original.
        var originalHash = await IntegrityChecker.ComputeSha256Async(localPath);
        _logger.Info($"Original SHA-256: {originalHash}");

        // 2. Upload (timed).
        var uploadStopwatch = Stopwatch.StartNew();
        var itemId = await _oneDrive.UploadAsync(localPath, _settings.RemoteFolder);
        uploadStopwatch.Stop();
        _logger.Info($"Upload took {uploadStopwatch.ElapsedMilliseconds} ms.");

        // 3. Download (timed) to an isolated local path.
        var downloadPath = Path.Combine(_settings.DownloadDirectory, fileName);
        var downloadStopwatch = Stopwatch.StartNew();
        await _oneDrive.DownloadAsync(itemId, downloadPath);
        downloadStopwatch.Stop();
        _logger.Info($"Download took {downloadStopwatch.ElapsedMilliseconds} ms.");

        // 4. Hash the downloaded copy and compare.
        var downloadedHash = await IntegrityChecker.ComputeSha256Async(downloadPath);
        _logger.Info($"Downloaded SHA-256: {downloadedHash}");

        var match = IntegrityChecker.HashesMatch(originalHash, downloadedHash);
        if (match)
            _logger.Success("Integrity verified: hashes match.");
        else
            _logger.Error("Integrity FAILURE: hashes differ!");

        return new ExperimentResult(
            fileName,
            fileSize,
            originalHash,
            downloadedHash,
            match,
            uploadStopwatch.Elapsed,
            downloadStopwatch.Elapsed);
    }

    /// <summary>Renders a compact results table to the log.</summary>
    public void PrintSummary(IReadOnlyList<ExperimentResult> results)
    {
        _logger.Section("Results Summary");
        _logger.Info($"{"File",-22}{"Size (B)",12}{"Up (ms)",10}{"Down (ms)",11}  Result");
        foreach (var r in results)
        {
            _logger.Info(
                $"{Truncate(r.FileName, 20),-22}" +
                $"{r.FileSizeBytes,12:N0}" +
                $"{(int)r.UploadDuration.TotalMilliseconds,10}" +
                $"{(int)r.DownloadDuration.TotalMilliseconds,11}" +
                $"  {r.Status}");
        }

        var passed = results.Count(r => r.HashesMatch);
        _logger.Info($"\n{passed}/{results.Count} files preserved integrity (bit-for-bit identical).");
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..(max - 1)] + "…";
}
