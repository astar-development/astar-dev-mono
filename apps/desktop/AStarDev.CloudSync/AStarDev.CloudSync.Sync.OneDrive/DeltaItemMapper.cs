using AStarDev.FunctionalParadigm;
using Microsoft.Graph.Models;

namespace AStarDev.CloudSync.Sync.OneDrive;

internal static class DeltaItemMapper
{
    internal static IEnumerable<RemoteItemSnapshot> Map(IEnumerable<DriveItem>? items) =>
        (items ?? []).Where(item => item.Root is null && item.Package is null && item.RemoteItem is null).SelectMany(ToSnapshot);

    private static RemoteItemSnapshot[] ToSnapshot(DriveItem item) =>
        RemoteItemIdFactory.Create(item.Id).Match(
            id => KindOf(item).Match(kind => ToSnapshot(item, id, kind), () => Array.Empty<RemoteItemSnapshot>()),
            _ => []);

    private static RemoteItemSnapshot[] ToSnapshot(DriveItem item, RemoteItemId id, RemoteItemKind kind) =>
        EntityTagFactory.Create(item.ETag ?? item.CTag ?? id.Value).Match(
            eTag => new[] { RemoteItemSnapshotFactory.Create(id, ParentOf(item), item.Name, kind, item.LastModifiedDateTime ?? DateTimeOffset.UnixEpoch, eTag) },
            _ => Array.Empty<RemoteItemSnapshot>());

    private static Option<RemoteItemId> ParentOf(DriveItem item) =>
        RemoteItemIdFactory.Create(item.ParentReference?.Id).Match(parent => Option.Some(parent), _ => Option.None<RemoteItemId>());

    private static Option<RemoteItemKind> KindOf(DriveItem item)
    {
        if (item.Deleted is not null)
            return Option.Some(RemoteItemKindFactory.CreateDeleted());

        if (item.File is not null)
            return ContentTagFactory.Create(item.CTag ?? item.ETag).Match(cTag => Option.Some(RemoteItemKindFactory.CreateFile(item.Size ?? 0, HashOf(item.File), cTag)), _ => Option.None<RemoteItemKind>());

        return item.Folder is not null ? Option.Some(RemoteItemKindFactory.CreateFolder()) : Option.None<RemoteItemKind>();
    }

    private static Option<ContentHash> HashOf(FileObject file) =>
        ContentHashFactory.Create(file.Hashes?.QuickXorHash ?? file.Hashes?.Sha1Hash ?? file.Hashes?.Sha256Hash).Match(hash => Option.Some(hash), _ => Option.None<ContentHash>());
}
