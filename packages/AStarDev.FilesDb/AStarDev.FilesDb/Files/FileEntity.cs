namespace AStarDev.FilesDb.Files;

/// <summary>
/// Represents a file entity in the database.
/// </summary>
public class FileEntity
{
    /// <summary>
    /// Gets or sets the unique identifier of the file entity.
    /// </summary>
    public FileId Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the file.
    /// </summary>
    public FileName Name { get; set; } = new FileName(string.Empty);

    /// <summary>
    /// Gets or sets the directory path of the file.
    /// </summary>
    public DirectoryPath Path { get; set; } = new DirectoryPath(string.Empty);

    /// <summary>
    /// Gets or sets the handle of the file, which is an opaque, stable identifier for the file across renames and moves.
    /// </summary>
    public FileHandle Handle { get; set; } = new FileHandle(string.Empty);

    /// <summary>The file size in bytes.</summary>
    public long FileSize { get; set; }

    /// <summary>Whether the file is of a supported image type.</summary>
    public bool IsImage => ImageDetail != null && ImageDetail.Width.HasValue && ImageDetail.Height.HasValue;

    /// <summary>
    /// Gets or sets the access details of the file, which tracks when the file's details were last refreshed and when it was last viewed.
    /// </summary>
    public FileAccessDetailEntity FileAccessDetail { get; set; } = new();

    /// <summary>
    /// Gets or sets the image details of the file, which contains the dimensions of the image if the file is an image.
    /// </summary>
    public ImageDetailEntity? ImageDetail { get; set; }

    /// <summary>
    /// Gets or sets the deletion status of the file, which tracks the soft- and hard-deletion state of the file.
    /// </summary>
    public DeletionStatusEntity DeletionStatus { get; set; } = new();
}
