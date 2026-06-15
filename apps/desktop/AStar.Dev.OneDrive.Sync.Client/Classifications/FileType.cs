namespace AStar.Dev.OneDrive.Sync.Client.Classifications;

/// <summary>Classifies the broad category of a file based on its extension.</summary>
public enum FileType
{
    /// <summary>Image files such as JPEG, PNG, GIF.</summary>
    Image,

    /// <summary>Document files such as PDF, DOCX, TXT.</summary>
    Document,

    /// <summary>Spreadsheet files such as XLSX, CSV.</summary>
    Spreadsheet,

    /// <summary>Presentation files such as PPTX, KEY.</summary>
    Presentation,

    /// <summary>Video files such as MP4, MKV.</summary>
    Video,

    /// <summary>Audio files such as MP3, FLAC.</summary>
    Audio,

    /// <summary>Archive files such as ZIP, TAR.</summary>
    Archive,

    /// <summary>Source code files such as CS, PY, JS.</summary>
    Code,

    /// <summary>Database files such as SQLite, MDB.</summary>
    Database,

    /// <summary>Executable or native library files.</summary>
    Executable,

    /// <summary>Extension not recognised by the classifier.</summary>
    Unknown
}
