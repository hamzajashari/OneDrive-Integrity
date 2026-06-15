using System.Text;

namespace OneDriveIntegrityLab.Logging;

/// <summary>
/// Lightweight structured logger that writes timestamped entries both to the
/// console (for live feedback) and to an in-memory buffer that can be flushed
/// to a lab-notes file at the end of a run. Keeping the experiment's narrative
/// in one place makes the "Lab Notes" deliverable reproducible.
/// </summary>
public sealed class LabLogger
{
    private readonly StringBuilder _buffer = new();
    private readonly object _gate = new();

    public void Info(string message) => Write("INFO", message);

    public void Step(string message) => Write("STEP", message);

    public void Success(string message) => Write("OK", message);

    public void Warn(string message) => Write("WARN", message);

    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        var line = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff}] {level,-7} {message}";
        lock (_gate)
        {
            _buffer.AppendLine(line);
            Console.WriteLine(line);
        }
    }

    /// <summary>Appends a free-form section header to the notes for readability.</summary>
    public void Section(string title)
    {
        var divider = new string('=', 60);
        lock (_gate)
        {
            _buffer.AppendLine();
            _buffer.AppendLine(divider);
            _buffer.AppendLine($"  {title}");
            _buffer.AppendLine(divider);
            Console.WriteLine();
            Console.WriteLine(divider);
            Console.WriteLine($"  {title}");
            Console.WriteLine(divider);
        }
    }

    /// <summary>Writes the accumulated log buffer to disk as lab notes.</summary>
    public async Task FlushToFileAsync(string path)
    {
        string contents;
        lock (_gate)
        {
            contents = _buffer.ToString();
        }
        await File.WriteAllTextAsync(path, contents);
        Console.WriteLine($"\nLab notes written to: {Path.GetFullPath(path)}");
    }
}
