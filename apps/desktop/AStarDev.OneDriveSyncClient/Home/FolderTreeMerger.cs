using AStar.Dev.Infrastructure.AppDb.Domain;
using AStarDev.FunctionalParadigm;

namespace AStarDev.OneDriveSyncClient.Home;

/// <summary>Merges the remote and local child folders of one tree level into a single ordered list.</summary>
public static class FolderTreeMerger
{
    /// <summary>Merges folders by case-insensitive name. An explicit rule always wins; otherwise local-only folders are included and the rest take <paramref name="defaultState"/>.</summary>
    public static IReadOnlyList<MergedFolder> Merge(IReadOnlyList<DriveFolder> remoteFolders, IReadOnlyList<string> localFolderNames, string parentRemotePath, Option<string> parentRemoteId, FolderSyncState defaultState, Func<string, Option<FolderSyncState>> ruleStateResolver)
    {
        var remoteByName = remoteFolders.GroupBy(folder => folder.Name, StringComparer.OrdinalIgnoreCase).ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var localNames = new HashSet<string>(localFolderNames, StringComparer.OrdinalIgnoreCase);

        var fromRemote = remoteByName.Values.Select(remote => MergedFolderFactory.Create(
            remote.Name,
            ChildPath(parentRemotePath, remote.Name),
            Option.Some(remote.Id),
            remote.ParentId,
            localNames.Contains(remote.Name) ? FolderOriginFactory.CreateBoth() : FolderOriginFactory.CreateRemoteOnly(),
            ResolveState(ChildPath(parentRemotePath, remote.Name), defaultState, ruleStateResolver)));

        var fromLocal = localNames.Where(name => !remoteByName.ContainsKey(name)).Select(name => MergedFolderFactory.Create(
            name,
            ChildPath(parentRemotePath, name),
            Option.None<string>(),
            parentRemoteId,
            FolderOriginFactory.CreateLocalOnly(),
            ResolveState(ChildPath(parentRemotePath, name), FolderSyncState.Included, ruleStateResolver)));

        return [.. fromRemote.Concat(fromLocal).OrderBy(folder => folder.Name, StringComparer.OrdinalIgnoreCase)];
    }

    private static string ChildPath(string parentRemotePath, string name) => $"{parentRemotePath.TrimEnd('/')}/{name}";

    private static FolderSyncState ResolveState(string remotePath, FolderSyncState fallback, Func<string, Option<FolderSyncState>> ruleStateResolver)
        => ruleStateResolver(remotePath).MapOrDefault(state => state, fallback);
}
