# Investigating File Integrity in Microsoft OneDrive Using the Graph API

A command-line experiment that uploads files to Microsoft OneDrive via the
Microsoft Graph API, downloads them back, and verifies whether they remain
**bit-for-bit identical** by comparing **SHA-256** hashes.

## Research Question

> Does OneDrive preserve the integrity of files during upload and download operations?

## Hypothesis

Files uploaded to OneDrive and then downloaded will be bit-for-bit identical, so
their SHA-256 hashes should match. Any mismatch would point to encoding changes,
API handling differences, or metadata interference.

---

## How It Works

The experiment runs a single, well-defined cycle per file:

1. **Hash the original** — compute the SHA-256 of the local file.
2. **Upload** — push the file to a OneDrive folder (created if missing), timed.
3. **Download** — pull the same file back into an isolated local directory, timed.
4. **Hash the copy** — compute the SHA-256 of the downloaded file.
5. **Compare** — the run passes only if the two hashes are identical.

Files up to 4 MB use a single-request upload; larger files automatically switch
to a **resumable upload session**, exercising both Graph upload paths.

### Architecture

The solution is deliberately split into small, single-responsibility classes so
each concern can be read and tested in isolation:

| Component | Responsibility |
|---|---|
| `GraphAuthenticator` | Builds an authenticated Graph client (device code flow) |
| `OneDriveService` | Folder creation, upload (simple + resumable), download |
| `IntegrityChecker` | Streamed SHA-256 hashing and comparison |
| `ExperimentRunner` | Orchestrates the hash → upload → download → verify cycle |
| `LabLogger` | Timestamped logging to console and a lab-notes file |
| `SampleFiles` | Generates a varied default test set (text/binary, small/large) |

---

## Running the Experiment

### Prerequisites

- .NET 8 SDK
- A free Microsoft account with OneDrive
- An Azure AD app registration (see `SETUP.md` for step-by-step instructions)

### Configure

Put your app registration's client ID in `appsettings.json`, or supply it via an
environment variable (preferred, keeps it out of source control):

```bash
export ONEDRIVE_CLIENT_ID="<your-client-id>"
```

### Run

```bash
# Auto-generated sample set (text, small binary, 5 MB binary)
dotnet run

# Or test specific files
dotnet run -- ./myfile.pdf ./another.zip
```

On first run you will be prompted to sign in using the device code shown in the
console. Results are printed as a summary table and written to `LAB_NOTES_run.txt`.

---

## Results

> Run the experiment and record your observed values here.

| File | Type | Size | Hashes Match | Upload (ms) | Download (ms) |
|---|---|---|---|---|---|
| small-text.txt | text | ~70 B | _[fill in]_ | _[fill in]_ | _[fill in]_ |
| small-binary.bin | binary | 1 KB | _[fill in]_ | _[fill in]_ | _[fill in]_ |
| large-binary.bin | binary | 5 MB | _[fill in]_ | _[fill in]_ | _[fill in]_ |

---

## Report

### Was the hypothesis confirmed?

_[After running: state whether all SHA-256 hashes matched. The expected result is
that they do — OneDrive stores file content faithfully, so integrity is preserved
across upload/download. Record any exceptions you observed.]_

### What challenges did you encounter?

- **Authentication setup** — registering the Azure AD app and choosing the right
  account types and scopes (`Files.ReadWrite`, `User.Read`) was the main setup hurdle.
- **Personal vs. work accounts** — personal Microsoft accounts authenticate against
  the `consumers` tenant; using `common` or `organizations` can cause sign-in errors.
- **Upload size threshold** — the Graph simple-upload limit (4 MB) means larger files
  need a resumable upload session, handled automatically in `OneDriveService`.
- _[Add anything specific you hit during your run.]_

### What future experiments would you suggest?

- Test a wider matrix of file types (text, images, archives, executables) and sizes
  (bytes up to gigabytes) to probe edge cases.
- Measure and chart upload/download throughput across sizes to characterise performance.
- Repeat the experiment against other providers (Google Drive, Dropbox) and compare
  integrity and behaviour.
- Inspect whether OneDrive alters or strips file **metadata** even when content is
  preserved (e.g. timestamps, EXIF) — content integrity and metadata integrity differ.
- Test concurrent uploads and very small (0-byte) files for boundary behaviour.

---

## Sources

- Microsoft Graph SDK for .NET — https://learn.microsoft.com/graph/sdks/sdks-overview
- Upload large files with an upload session — https://learn.microsoft.com/graph/api/driveitem-createuploadsession
- DriveItem resource — https://learn.microsoft.com/graph/api/resources/driveitem
- Device code authentication (Azure.Identity) — https://learn.microsoft.com/dotnet/api/azure.identity.devicecodecredential
