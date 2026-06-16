namespace OneDriveIntegrityLab.Experiment;

/// <summary>
/// Immutable record of a single upload/download/verify cycle for one file.
/// Used to build the final results summary in the lab notes.
/// </summary>
public sealed record ExperimentResult(
    string FileName,
    long FileSizeBytes,
    string OriginalHash,
    string DownloadedHash,
    bool HashesMatch,
    TimeSpan UploadDuration,
    TimeSpan DownloadDuration)
{
    public string Status => HashesMatch ? "PASS" : "FAIL";
}
