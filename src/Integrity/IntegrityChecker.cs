using System.Security.Cryptography;

namespace OneDriveIntegrityLab.Integrity;

/// <summary>
/// Computes and compares SHA-256 hashes. The hash is streamed from disk so that
/// arbitrarily large files can be verified without loading them into memory.
/// </summary>
public static class IntegrityChecker
{
    /// <summary>Computes the lowercase hex SHA-256 digest of a file.</summary>
    public static async Task<string> ComputeSha256Async(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        using var sha = SHA256.Create();
        var hashBytes = await sha.ComputeHashAsync(stream);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Constant-time-ish comparison of two hex digests. (Ordinal compare is
    /// sufficient here since both values are produced locally.)
    /// </summary>
    public static bool HashesMatch(string a, string b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
