using Microsoft.Extensions.Configuration;
using OneDriveIntegrityLab.Auth;
using OneDriveIntegrityLab.Configuration;
using OneDriveIntegrityLab.Experiment;
using OneDriveIntegrityLab.Logging;
using OneDriveIntegrityLab.Storage;

namespace OneDriveIntegrityLab;

/// <summary>
/// Entry point. Wires up configuration, authentication, and the experiment
/// runner, then executes the integrity test against one or more files.
///
/// Usage:
///   dotnet run                       # runs against an auto-generated sample set
///   dotnet run -- ./path/to/file     # runs against specific file(s)
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var logger = new LabLogger();
        logger.Section("OneDrive File Integrity Experiment");

        try
        {
            var settings = LoadSettings();
            settings.Validate();

            // Determine which files to test: explicit args, or a generated sample set.
            var files = args.Length > 0
                ? args.ToList()
                : SampleFiles.GenerateDefaultSet(logger);

            var authenticator = new GraphAuthenticator(settings, logger);
            var graphClient = authenticator.CreateClient();
            var oneDrive = new OneDriveService(graphClient, logger);
            var runner = new ExperimentRunner(oneDrive, settings, logger);

            var results = await runner.RunAsync(files);
            runner.PrintSummary(results);

            await logger.FlushToFileAsync("LAB_NOTES_run.txt");

            // Non-zero exit code if any file failed integrity, useful for CI.
            return results.All(r => r.HashesMatch) ? 0 : 1;
        }
        catch (Exception ex)
        {
            logger.Error($"Experiment aborted: {ex.Message}");
            await logger.FlushToFileAsync("LAB_NOTES_run.txt");
            return 2;
        }
    }

    private static ExperimentSettings LoadSettings()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            // Local secrets (gitignored) override the committed defaults.
            .AddJsonFile("appsettings.local.json", optional: true)
            // Environment variables override file values, e.g. ONEDRIVE_CLIENT_ID.
            .AddEnvironmentVariables(prefix: "ONEDRIVE_")
            .Build();

        var settings = new ExperimentSettings();
        config.Bind(settings);

        // Allow a bare ONEDRIVE_CLIENT_ID env var (without section binding) too.
        var envClientId = Environment.GetEnvironmentVariable("ONEDRIVE_CLIENT_ID");
        if (!string.IsNullOrWhiteSpace(envClientId))
        {
            settings.ClientId = envClientId;
        }

        return settings;
    }
}
