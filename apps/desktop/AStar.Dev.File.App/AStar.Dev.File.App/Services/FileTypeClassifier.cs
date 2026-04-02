using AStar.Dev.File.App.Models;
using System;
using System.Collections.Generic;

namespace AStar.Dev.File.App.Services;

public class FileTypeClassifier : IFileTypeClassifier
{
    private static readonly Dictionary<string, FileType> ExtensionMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Image
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

            // Document
            [".pdf"] = FileType.Document,
            [".doc"] = FileType.Document,
            [".docx"] = FileType.Document,
            [".txt"] = FileType.Document,
            [".rtf"] = FileType.Document,
            [".odt"] = FileType.Document,
            [".md"] = FileType.Document,
            [".pages"] = FileType.Document,
            [".epub"] = FileType.Document,

            // Spreadsheet
            [".xls"] = FileType.Spreadsheet,
            [".xlsx"] = FileType.Spreadsheet,
            [".csv"] = FileType.Spreadsheet,
            [".ods"] = FileType.Spreadsheet,
            [".numbers"] = FileType.Spreadsheet,

            // Presentation
            [".ppt"] = FileType.Presentation,
            [".pptx"] = FileType.Presentation,
            [".odp"] = FileType.Presentation,
            [".key"] = FileType.Presentation,

            // Video
            [".mp4"] = FileType.Video,
            [".avi"] = FileType.Video,
            [".mov"] = FileType.Video,
            [".mkv"] = FileType.Video,
            [".wmv"] = FileType.Video,
            [".flv"] = FileType.Video,
            [".webm"] = FileType.Video,
            [".m4v"] = FileType.Video,

            // Audio
            [".mp3"] = FileType.Audio,
            [".wav"] = FileType.Audio,
            [".flac"] = FileType.Audio,
            [".aac"] = FileType.Audio,
            [".ogg"] = FileType.Audio,
            [".m4a"] = FileType.Audio,
            [".wma"] = FileType.Audio,

            // Archive
            [".zip"] = FileType.Archive,
            [".rar"] = FileType.Archive,
            [".7z"] = FileType.Archive,
            [".tar"] = FileType.Archive,
            [".gz"] = FileType.Archive,
            [".bz2"] = FileType.Archive,
            [".xz"] = FileType.Archive,

            // Code
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

            // Database
            [".db"] = FileType.Database,
            [".sqlite"] = FileType.Database,
            [".sqlite3"] = FileType.Database,
            [".mdb"] = FileType.Database,
            [".accdb"] = FileType.Database,

            // Executable
            [".exe"] = FileType.Executable,
            [".dll"] = FileType.Executable,
            [".so"] = FileType.Executable,
            [".dylib"] = FileType.Executable,
        };

    public FileType Classify(string fileExtension)
    {
        if (string.IsNullOrEmpty(fileExtension))
            return FileType.Unknown;

        return ExtensionMap.TryGetValue(fileExtension, out var type)
            ? type
            : FileType.Unknown;
    }
}
