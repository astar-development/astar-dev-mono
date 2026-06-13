using System.IO.Abstractions;

namespace AStar.Dev.OneDrive.Sync.Client.Classifications;

/// <summary>Exports and imports the file classification taxonomy to/from a JSON file.</summary>
public interface IFileClassificationExportImportService
{
    /// <summary>Serializes the current taxonomy to a JSON file described by <paramref name="fileInfo"/>.</summary>
    Task ExportAsync(IFileInfo fileInfo, CancellationToken cancellationToken = default);

    /// <summary>Reads a JSON taxonomy file described by <paramref name="fileInfo"/> and replaces ALL existing categories and keywords.</summary>
    Task ImportAsync(IFileInfo fileInfo, CancellationToken cancellationToken = default);
}
