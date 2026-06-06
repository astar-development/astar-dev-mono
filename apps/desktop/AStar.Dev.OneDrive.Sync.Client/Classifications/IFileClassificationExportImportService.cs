namespace AStar.Dev.OneDrive.Sync.Client.Classifications;

/// <summary>Exports and imports the file classification taxonomy to/from a JSON file.</summary>
public interface IFileClassificationExportImportService
{
    /// <summary>Serializes the current taxonomy to a JSON file at <paramref name="filePath"/>.</summary>
    Task ExportAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>Reads a JSON taxonomy file and replaces ALL existing categories and keywords.</summary>
    Task ImportAsync(string filePath, CancellationToken cancellationToken = default);
}
