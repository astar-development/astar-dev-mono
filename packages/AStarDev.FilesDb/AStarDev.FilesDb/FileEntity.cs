namespace AStarDev.FilesDb;

/// <summary>
/// Represents a file entity in the database.
/// </summary>
public class FileEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the file entity.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the file.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path of the file.
    /// </summary>
    public string Path { get; set; } = string.Empty;
}
