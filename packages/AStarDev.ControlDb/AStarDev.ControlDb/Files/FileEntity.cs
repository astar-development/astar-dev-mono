namespace AStarDev.ControlDb.Files;

/// <summary>
/// Represents a file entity in the system, containing information about the file's identity, name, path, handle, size, access details, image details, and deletion status.
/// </summary>
/// <param name="Id">The unique identifier of the file entity.</param>
/// <param name="Name">The name of the file.</param>
/// <param name="Path">The directory path of the file.</param>
/// <param name="Handle">The handle of the file, which is an opaque, stable identifier for the file across renames and moves.</param>
/// <param name="FileSize">The file size in bytes.</param>
public sealed record FileEntity(FileId Id, FileName Name, DirectoryPath Path, FileHandle Handle, long FileSize)
{
    /// <summary>The access details of the file, which tracks when the file's details were last refreshed and when it was last viewed.</summary>
    public FileAccessDetailEntity FileAccessDetail { get; init; } = null!;

    /// <summary>The image details of the file, which contains the dimensions of the image if the file is an image.</summary>
    public ImageDetailEntity? ImageDetail { get; init; }

    /// <summary>The deletion status of the file, which tracks the soft- and hard-deletion state of the file.</summary>
    public DeletionStatusEntity DeletionStatus { get; init; } = null!;
}
