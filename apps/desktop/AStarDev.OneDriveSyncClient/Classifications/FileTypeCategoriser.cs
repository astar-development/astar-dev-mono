using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;

namespace AStarDev.OneDriveSyncClient.Classifications;

/// <inheritdoc />
public sealed class FileTypeCategoriser(IFileTypeClassifier fileTypeClassifier) : IFileTypeCategoriser
{
    private static readonly HashSet<string> scriptExtensions = new(StringComparer.OrdinalIgnoreCase) { ".ps1", ".sh" };

    /// <inheritdoc />
    public Option<FileClassification> Categorise(string remotePath)
    {
        if (string.IsNullOrWhiteSpace(remotePath))
            return Option.None<FileClassification>();

        string extension = Path.GetExtension(remotePath);
        var (level1, level2) = ToLevels(fileTypeClassifier.Classify(extension), extension);

        return Option.Some(FileClassificationFactory.Create(level1, level2, Option.None<string>(), false, false));
    }

    private static (string Level1, Option<string> Level2) ToLevels(FileType fileType, string extension) => fileType switch
    {
        FileType.Image => ("Photo", Option.None<string>()),
        FileType.Document => ("Document", Option.None<string>()),
        FileType.Spreadsheet => ("Document", Option.Some("Spreadsheet")),
        FileType.Presentation => ("Document", Option.Some("Presentation")),
        FileType.Video => ("Video", Option.None<string>()),
        FileType.Audio => ("Audio", Option.None<string>()),
        FileType.Archive => ("Archive", Option.None<string>()),
        FileType.Code => ("Code", scriptExtensions.Contains(extension) ? Option.Some("Script") : Option.None<string>()),
        _ => ("Other", Option.None<string>())
    };
}
