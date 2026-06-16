using System.Security.Cryptography;
using System.Text;
using OneDriveIntegrityLab.Logging;

namespace OneDriveIntegrityLab.Experiment;

/// <summary>
/// Generates a small, varied set of sample files so the experiment can run with
/// no arguments. Covers the dimensions called out in the lab: text vs binary,
/// and small vs larger files (which also exercises the resumable-upload path).
/// </summary>
public static class SampleFiles
{
    public static List<string> GenerateDefaultSet(LabLogger logger)
    {
        var dir = Path.Combine("samples");
        Directory.CreateDirectory(dir);
        logger.Info($"No file argument supplied. Generating sample set in '{dir}'.");

        var files = new List<string>();

        // 1. Small UTF-8 text file (includes a unicode char to probe encoding handling).
        var textPath = Path.Combine(dir, "small-text.txt");
        File.WriteAllText(textPath,
            "OneDrive integrity test \u2014 ascii + unicode (\u00e9, \u4e2d, \uD83D\uDE00).\n",
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        files.Add(textPath);

        // 2. Small random binary file (1 KB).
        files.Add(WriteRandomBinary(dir, "small-binary.bin", 1 * 1024));

        // 3. Larger binary file (5 MB) to trigger the resumable upload session.
        files.Add(WriteRandomBinary(dir, "large-binary.bin", 5 * 1024 * 1024));

        return files;
    }

    private static string WriteRandomBinary(string dir, string name, int sizeBytes)
    {
        var path = Path.Combine(dir, name);
        var buffer = new byte[sizeBytes];
        RandomNumberGenerator.Fill(buffer);
        File.WriteAllBytes(path, buffer);
        return path;
    }
}
