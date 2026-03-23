using System.IO.Abstractions;
using AStar.Dev.Infrastructure.Data;
using AStar.Dev.Utilities;

namespace AStar.Dev.Infrastructure.FilesDb.Models;

/// <summary>
///     The FileDetail class containing the current properties
/// </summary>
public sealed class FileDetail
{
    /// <summary>
    ///     The default constructor required by EF Core
    /// </summary>
    public FileDetail()
    {
    }

    /// <summary>
    ///     The copy constructor that allows for passing an instance of FileInfo to this class, simplifying consumer code
    /// </summary>
    /// <param name="fileInfo">
    ///     The instance of FileInfo to map
    /// </param>
    public FileDetail(IFileInfo fileInfo)
    {
        FileName      = new(fileInfo.Name);
        DirectoryName = new(fileInfo.DirectoryName!);
        FileSize      = fileInfo.Length;
    }

    /// <summary>
    /// </summary>
    public ICollection<FileClassification> FileClassifications { get; set; } = [];

    /// <summary>
    ///     Gets or sets the ID of the <see href="FileDetail"></see>. I know, shocking...
    /// </summary>
    public FileId Id { get; set; }

    /// <summary>
    /// </summary>
    public FileAccessDetail FileAccessDetail { get; set; } = new();

    /// <summary>
    ///     Gets or sets the file name. I know, shocking...
    /// </summary>
    public required FileName FileName { get; set; }

    /// <summary>
    ///     Gets or sets the name of the directory containing the file detail. I know, shocking...
    /// </summary>
    public required DirectoryName DirectoryName { get; set; }

    /// <summary>
    ///     Gets the full name of the file with the path combined
    /// </summary>
    public string FullNameWithPath => Path.Combine(DirectoryName.Value, FileName.Value ?? "");

    /// <summary>
    ///     Gets or sets the height of the image. I know, shocking...
    /// </summary>
    public ImageDetail ImageDetail { get; set; } = new(null, null);

    /// <summary>
    ///     Gets or sets the file size. I know, shocking...
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    ///     Gets or sets the file Creation Date. I know, shocking...
    /// </summary>
    public DateTimeOffset CreatedDate { get; set; }

    /// <summary>
    ///     Gets or sets whether the file is of a supported image type
    /// </summary>
    public bool IsImage { get; set; }

    /// <summary>
    ///     Gets or sets the file handle. I know, shocking...
    /// </summary>
    public FileHandle FileHandle { get; set; }

    /// <summary>
    /// </summary>
    public DeletionStatus DeletionStatus { get; set; } = new();

    /// <summary>
    ///     Returns this object in JSON format
    /// </summary>
    /// <returns>
    ///     This object serialized as a JSON object.
    /// </returns>
    public override string ToString()
        => this.ToJson();
}