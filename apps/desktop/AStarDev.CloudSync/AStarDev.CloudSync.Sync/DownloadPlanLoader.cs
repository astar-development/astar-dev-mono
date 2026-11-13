using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <summary>Reads everything the download planner needs for one account: the remote index with its paths, the baseline, the rules and the quarantine (#1400).</summary>
/// <param name="remoteIndex">The remote index.</param>
/// <param name="baselineReader">The sync baseline.</param>
/// <param name="ruleReader">The selection rules.</param>
/// <param name="quarantine">The quarantined paths.</param>
/// <param name="logger">Reports why a read failed, with no paths.</param>
internal sealed class DownloadPlanLoader(IRemoteIndexReader remoteIndex, ISyncBaselineReader baselineReader, ISelectionRuleReader ruleReader, IPathQuarantine quarantine, ILogger<DownloadPlanLoader> logger)
{
    private const string Location = nameof(DownloadPlanLoader);
    private const int PageSize = 5000;
    private const char Separator = '/';

    internal async Task<Result<DownloadPlanRequest, DownloadPassFailure>> LoadAsync(AccountId accountId, IDirectoryInfo root, LocalChangeSet local, CancellationToken cancellationToken)
    {
        var request = await IsCompleteAsync(accountId, cancellationToken)
            .BindAsync(_ => ListRemoteAsync(accountId, Option.None<RemoteItemId>(), string.Empty, cancellationToken))
            .BindAsync(remote => ReadBaselineAsync(accountId, cancellationToken).BindAsync(baseline => ReadRequestAsync(accountId, root, local, remote, baseline, cancellationToken)))
            .ConfigureAwait(false);

        return request.Match(Result.Success<DownloadPlanRequest, DownloadPassFailure>, Failed);
    }

    private static string Join(string parent, string name) => parent.Length == 0 ? name : $"{parent}{Separator}{name}";

    private static IEnumerable<RelativeSyncPath> MissingLocally(IDirectoryInfo root, IReadOnlyList<RemoteIndexEntry> remote, IReadOnlyList<SyncBaselineEntry> baseline, LocalChangeSet local)
    {
        var changed = local.Changes.Select(change => change.Path).ToHashSet();
        var remoteById = remote.GroupBy(entry => entry.Item.Id).ToDictionary(group => group.Key, group => group.First());
        var fileSystem = root.FileSystem;

        return baseline.Where(entry => entry.Kind is SyncBaselineKind.File synced
            && !changed.Contains(entry.Path)
            && remoteById.TryGetValue(entry.ItemId, out var found)
            && found.Path == entry.Path
            && found.Item.Kind is RemoteItemKind.File file
            && file.CTag == synced.CTag
            && !fileSystem.File.Exists(fileSystem.Path.Join(root.FullName, entry.Path.Value))).Select(entry => entry.Path);
    }

    private Result<DownloadPlanRequest, DownloadPassFailure> Failed(ErrorResponse error)
    {
        LogMessage.Warning(logger, Location, "The download pass could not read its inputs.");

        return Result.Failure<DownloadPlanRequest, DownloadPassFailure>(DownloadPassFailureFactory.CreateUnexpected());
    }

    private Task<Result<bool, ErrorResponse>> IsCompleteAsync(AccountId accountId, CancellationToken cancellationToken) =>
        remoteIndex.IsCompleteAsync(accountId, cancellationToken)
            .BindAsync(complete => Task.FromResult(complete ? Result.Success<bool, ErrorResponse>(true) : Result.Failure<bool, ErrorResponse>(new ErrorResponse("The remote index is not complete."))));

    private Task<Result<IReadOnlyList<RemoteIndexEntry>, ErrorResponse>> ListRemoteAsync(AccountId accountId, Option<RemoteItemId> parent, string parentPath, CancellationToken cancellationToken) =>
        remoteIndex.GetChildrenAsync(accountId, parent, cancellationToken).BindAsync(children => ExpandAsync(accountId, children, parentPath, cancellationToken));

    private async Task<Result<IReadOnlyList<RemoteIndexEntry>, ErrorResponse>> ExpandAsync(AccountId accountId, IReadOnlyList<RemoteItemSnapshot> children, string parentPath, CancellationToken cancellationToken)
    {
        List<RemoteIndexEntry> all = [];
        foreach (var child in children.Where(candidate => candidate.Kind is not RemoteItemKind.Deleted))
        {
            var path = Join(parentPath, child.Name);
            var valid = RelativeSyncPathFactory.Create(path).Match(Option.Some, _ => Option.None<RelativeSyncPath>());
            if (valid.Match(found => { all.Add(RemoteIndexEntryFactory.Create(found, child)); return false; }, () => true) || child.Kind is not RemoteItemKind.Folder)
                continue;

            var below = await ListRemoteAsync(accountId, Option.Some(child.Id), path, cancellationToken).ConfigureAwait(false);
            var failure = below.Match(entries => { all.AddRange(entries); return Option.None<ErrorResponse>(); }, Option.Some);
            if (failure.Match(_ => true, () => false))
                return failure.Match(Result.Failure<IReadOnlyList<RemoteIndexEntry>, ErrorResponse>, () => Result.Success<IReadOnlyList<RemoteIndexEntry>, ErrorResponse>(all));
        }

        return Result.Success<IReadOnlyList<RemoteIndexEntry>, ErrorResponse>(all);
    }

    private async Task<Result<IReadOnlyList<SyncBaselineEntry>, ErrorResponse>> ReadBaselineAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        List<SyncBaselineEntry> all = [];
        var after = Option.None<RelativeSyncPath>();
        while (true)
        {
            var page = await baselineReader.GetUnderAsync(accountId, new RelativeSyncPath(string.Empty), after, PageSize, cancellationToken).ConfigureAwait(false);
            var read = page.Match(entries => { all.AddRange(entries); after = entries.Count == 0 ? after : Option.Some(entries[^1].Path); return Result.Success<int, ErrorResponse>(entries.Count); }, Result.Failure<int, ErrorResponse>);
            if (read.Match(count => count < PageSize, _ => true))
                return read.Match(_ => Result.Success<IReadOnlyList<SyncBaselineEntry>, ErrorResponse>(all), Result.Failure<IReadOnlyList<SyncBaselineEntry>, ErrorResponse>);
        }
    }

    private Task<Result<DownloadPlanRequest, ErrorResponse>> ReadRequestAsync(AccountId accountId, IDirectoryInfo root, LocalChangeSet local, IReadOnlyList<RemoteIndexEntry> remote, IReadOnlyList<SyncBaselineEntry> baseline, CancellationToken cancellationToken) =>
        ruleReader.GetAsync(accountId, cancellationToken)
            .BindAsync(rules => quarantine.ReadAsync(accountId, cancellationToken).BindAsync(snapshot => Task.FromResult(Result.Success<DownloadPlanRequest, ErrorResponse>(DownloadPlanRequestFactory.Create(remote, baseline, local, rules, snapshot, MissingLocally(root, remote, baseline, local))))));
}
