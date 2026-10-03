using System.IO.Abstractions;
using System.Text.Json;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

public sealed class ScrapeConfigurationFileWriter(IFileSystem fileSystem) : IScrapeConfigurationFileWriter
{
    private const string PartialFileExtension = ".part";

    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private static readonly FileStreamOptions ownerOnlyOptions = CreateOwnerOnlyOptions();

    public async Task WriteAsync(ScrapeConfigurationImportDocument document, string filePath, CancellationToken cancellationToken = default)
    {
        var partialPath = $"{filePath}{PartialFileExtension}";
        try
        {
            await using (var stream = fileSystem.FileStream.New(partialPath, ownerOnlyOptions))
            {
                await JsonSerializer.SerializeAsync(stream, document, jsonOptions, cancellationToken);
            }

            fileSystem.File.Move(partialPath, filePath, overwrite: true);
        }
        finally
        {
            // Only reached with the partial file still present when writing failed or was cancelled: never leave a truncated export, and never touch an earlier one.
            if (fileSystem.File.Exists(partialPath)) fileSystem.File.Delete(partialPath);
        }
    }

    private static FileStreamOptions CreateOwnerOnlyOptions()
    {
        var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write, Share = FileShare.None, Options = FileOptions.Asynchronous };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;

        return options;
    }
}
