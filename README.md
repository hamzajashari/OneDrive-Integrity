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

Run on 2026-06-16 against a personal OneDrive account (`consumers` tenant).
Full log in [`LAB_NOTES_run.txt`](LAB_NOTES_run.txt).

| File | Type | Size | Hashes Match | Upload (ms) | Download (ms) |
|---|---|---|---|---|---|
| small-text.txt | text | 61 B | ✅ yes | 730 | 1111 |
| small-binary.bin | binary | 1,024 B | ✅ yes | 684 | 591 |
| large-binary.bin | binary | 5,242,880 B (5 MB) | ✅ yes | 44,212 | 1,505 |

**3/3 files preserved integrity (bit-for-bit identical).**

The SHA-256 digest of every file was identical before upload and after download,
for example for `small-text.txt`:

```
original   : e646d5f945f1c0903ed6d1c5533cbe26cecff3f7714bed75e74833e78e8024a4
downloaded : e646d5f945f1c0903ed6d1c5533cbe26cecff3f7714bed75e74833e78e8024a4
```

---

## Report

### Was the hypothesis confirmed?

Yes. All three files — a tiny UTF-8 text file (including ASCII, accented, CJK and
emoji characters), a 1 KB random binary, and a 5 MB random binary — came back from
OneDrive with **SHA-256 hashes identical to the originals**. OneDrive preserved the
content faithfully in every case, including across both Graph upload paths (the
single-PUT simple upload for the small files and the resumable upload session for
the 5 MB file). No discrepancies, encoding changes, or content mutation were observed.

The only meaningful difference between files was **timing, not integrity**: the
5 MB resumable upload took ~44 s, while every small file uploaded in well under a
second. That cost comes from the upload-session protocol overhead (creating the
session, then chunked PUTs), not from the file size alone — the 5 MB download, by
contrast, completed in ~1.5 s. So the size/path difference shows up purely as
performance, with integrity unaffected.

### What challenges did you encounter?

- **The `appsettings.local.json` path didn't actually work.** Both `SETUP.md` and
  `.gitignore` treat `appsettings.local.json` as the place to keep the client ID,
  but the original `LoadSettings()` only read `appsettings.json` and `ONEDRIVE_`
  environment variables. I had to add that file to the configuration builder *and*
  copy it to the build output directory before the documented workflow worked.
- **Build/runtime mismatch** — the project targets .NET 8 (LTS), but only newer
  runtimes were installed locally, so the app wouldn't launch until I added
  `<RollForward>LatestMajor</RollForward>`. `config.Bind` also needed the
  `Microsoft.Extensions.Configuration.Binder` package, which wasn't referenced.
- **Device-code sign-in** — the code has to be entered at
  `microsoft.com/devicelogin`, not the general account page, and only one instance
  of the app should be running at a time (two concurrent runs issue two competing
  codes, which is confusing). Personal accounts must use the `consumers` tenant;
  `common`/`organizations` can cause sign-in errors.
- **Upload size threshold** — the Graph simple-upload limit (4 MB) means larger
  files need a resumable upload session, handled automatically in `OneDriveService`.

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
