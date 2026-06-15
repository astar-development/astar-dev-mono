using System.Collections.Frozen;

namespace AStar.Dev.OneDrive.Sync.Client.Classifications;

/// <summary>Classifies file extensions into broad <see cref="FileType"/> categories for the OneDrive sync client.</summary>
public sealed class SyncClientFileTypeClassifier : IFileTypeClassifier
{
    private static readonly FrozenDictionary<string, FileType> extensionMap =
        new Dictionary<string, FileType>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = FileType.Image,
            [".jpeg"] = FileType.Image,
            [".png"] = FileType.Image,
            [".gif"] = FileType.Image,
            [".bmp"] = FileType.Image,
            [".tiff"] = FileType.Image,
            [".tif"] = FileType.Image,
            [".webp"] = FileType.Image,
            [".svg"] = FileType.Image,
            [".heic"] = FileType.Image,
            [".raw"] = FileType.Image,
            [".ico"] = FileType.Image,
            [".avif"] = FileType.Image,

            [".pdf"] = FileType.Document,
            [".doc"] = FileType.Document,
            [".docx"] = FileType.Document,
            [".txt"] = FileType.Document,
            [".rtf"] = FileType.Document,
            [".odt"] = FileType.Document,
            [".md"] = FileType.Document,
            [".pages"] = FileType.Document,
            [".epub"] = FileType.Document,

            [".xls"] = FileType.Spreadsheet,
            [".xlsx"] = FileType.Spreadsheet,
            [".csv"] = FileType.Spreadsheet,
            [".ods"] = FileType.Spreadsheet,
            [".numbers"] = FileType.Spreadsheet,

            [".ppt"] = FileType.Presentation,
            [".pptx"] = FileType.Presentation,
            [".odp"] = FileType.Presentation,
            [".key"] = FileType.Presentation,

            [".mp4"] = FileType.Video,
            [".avi"] = FileType.Video,
            [".mov"] = FileType.Video,
            [".mkv"] = FileType.Video,
            [".wmv"] = FileType.Video,
            [".flv"] = FileType.Video,
            [".webm"] = FileType.Video,
            [".m4v"] = FileType.Video,

            [".mp3"] = FileType.Audio,
            [".wav"] = FileType.Audio,
            [".flac"] = FileType.Audio,
            [".aac"] = FileType.Audio,
            [".ogg"] = FileType.Audio,
            [".m4a"] = FileType.Audio,
            [".wma"] = FileType.Audio,

            [".zip"] = FileType.Archive,
            [".rar"] = FileType.Archive,
            [".7z"] = FileType.Archive,
            [".tar"] = FileType.Archive,
            [".gz"] = FileType.Archive,
            [".bz2"] = FileType.Archive,
            [".xz"] = FileType.Archive,

            [".cs"] = FileType.Code,
            [".py"] = FileType.Code,
            [".js"] = FileType.Code,
            [".ts"] = FileType.Code,
            [".java"] = FileType.Code,
            [".cpp"] = FileType.Code,
            [".c"] = FileType.Code,
            [".h"] = FileType.Code,
            [".go"] = FileType.Code,
            [".rs"] = FileType.Code,
            [".rb"] = FileType.Code,
            [".php"] = FileType.Code,
            [".html"] = FileType.Code,
            [".css"] = FileType.Code,
            [".json"] = FileType.Code,
            [".xml"] = FileType.Code,
            [".yaml"] = FileType.Code,
            [".yml"] = FileType.Code,
            [".sh"] = FileType.Code,
            [".ps1"] = FileType.Code,
            [".sql"] = FileType.Code,

            [".db"] = FileType.Database,
            [".sqlite"] = FileType.Database,
            [".sqlite3"] = FileType.Database,
            [".mdb"] = FileType.Database,
            [".accdb"] = FileType.Database,

            [".exe"] = FileType.Executable,
            [".dll"] = FileType.Executable,
            [".so"] = FileType.Executable,
            [".dylib"] = FileType.Executable,
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public FileType Classify(string fileExtension)
    {
        if (string.IsNullOrEmpty(fileExtension))
            return FileType.Unknown;

        return extensionMap.TryGetValue(fileExtension, out FileType type)
            ? type
            : FileType.Unknown;
    }
}
