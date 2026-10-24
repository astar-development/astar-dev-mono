using AStarDev.FunctionalParadigm;
using AStar.Dev.Infrastructure.AppDb.Domain;

namespace AStarDev.OneDriveSyncClient.Classifications;

/// <inheritdoc />
public sealed class FolderNameCategoriser : IFolderNameCategoriser
{
    private static readonly Dictionary<string, (string Level1, Option<string> Level2)> folderMap = new Dictionary<string, (string Level1, Option<string> Level2)>(StringComparer.OrdinalIgnoreCase)
    {
        ["wallpapers"] = ("Photo", Option.Some("Wallpaper")),
        ["screenshots"] = ("Photo", Option.Some("Screenshot")),
        ["receipts"] = ("Document", Option.Some("Receipt")),
        ["invoices"] = ("Document", Option.Some("Invoice")),
        ["docs"] = ("Document", Option.None<string>())
    };

    /// <inheritdoc />
    public IReadOnlyList<FileClassification> Categorise(string remotePath)
    {
        if (string.IsNullOrWhiteSpace(remotePath))
            return [];

        string[] segments = remotePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        return [.. segments[..^Math.Min(1, segments.Length)]
            .Where(folderMap.ContainsKey)
            .Select(segment => folderMap[segment])
            .Distinct()
            .Select(levels => FileClassificationFactory.Create(levels.Level1, levels.Level2, Option.None<string>(), false, false))];
    }
}
