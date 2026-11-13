using System.IO.Abstractions;
using AStarDev.CloudSync.Accounts;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.Sync;

/// <summary>Gives new local folders the default rule (#1397 decision 3, #1399): a folder that is on disk, has no covering rule, is not under an Exclude and is not in the remote index gets <c>Include(TwoWay)</c>. A folder that also exists remotely stays excluded. Only the top-most such folder gets a rule, and never one that would replace a rule below it. When the remote index is not complete yet (its first full enumeration has not caught up, #1540), or a read or the save fails, no rule is added and the next scan tries again.</summary>
/// <param name="ruleReader">Reads the account's selection rules.</param>
/// <param name="ruleWriter">Saves the new rules; the write is idempotent.</param>
/// <param name="remoteIndex">Tells whether a folder exists remotely.</param>
/// <param name="evaluator">Judges folders against the rules.</param>
/// <param name="folderFilter">Skips hidden, dot-prefixed, linked and app temporary folders.</param>
/// <param name="logger">Reports counts only, never a path.</param>
internal sealed class NewFolderRules(ISelectionRuleReader ruleReader, ISelectionRuleWriter ruleWriter, IRemoteIndexReader remoteIndex, ISyncSelectionEvaluator evaluator, ILocalFolderFilter folderFilter, ILogger<NewFolderRules> logger)
{
    private const string Location = nameof(NewFolderRules);
    private const char Separator = '/';

    internal async Task<IReadOnlyList<SelectionRule>> PrepareAsync(AccountId accountId, IDirectoryInfo root, CancellationToken cancellationToken)
    {
        var rules = (await ruleReader.GetAsync(accountId, cancellationToken).ConfigureAwait(false)).OrThrow();
        var candidates = Candidates(root, LocalRelativePathFactory.Root, rules, cancellationToken).ToList();
        if (candidates.Count == 0)
            return rules;

        var localOnly = await Try.RunAsync(() => LocalOnlyAsync(accountId, candidates, cancellationToken), cancellationToken)
            .MatchAsync(found => found, exception => NoneAdded($"The remote index could not be read ({exception.ErrorType()}); new local folders keep no rule until the next scan.")).ConfigureAwait(false);

        return await SaveAsync(accountId, localOnly, rules, cancellationToken).ConfigureAwait(false);
    }

    private static string Join(LocalRelativePath parent, string name) => string.IsNullOrEmpty(parent.Value) ? name : $"{parent.Value}{Separator}{name}";

    private IReadOnlyList<LocalRelativePath> NoneAdded(string message) => Warn<IReadOnlyList<LocalRelativePath>>(message, []);

    private T Warn<T>(string message, T result)
    {
        LogMessage.Warning(logger, Location, message);

        return result;
    }

    private async Task<IReadOnlyList<SelectionRule>> SaveAsync(AccountId accountId, IReadOnlyList<LocalRelativePath> localOnly, IReadOnlyList<SelectionRule> rules, CancellationToken cancellationToken)
    {
        if (localOnly.Count == 0)
            return rules;

        IReadOnlyList<SelectionChange> changes = [.. localOnly.Select(path => SelectionChangeFactory.CreateIncludeFolder(path, Option.None<RemoteItemId>(), SyncDirectionFactory.CreateTwoWay()))];
        var saved = await ruleWriter.ApplyAsync(accountId, changes, cancellationToken)
            .MatchAsync(_ => true, _ => Warn("The default rules of new local folders could not be saved; the next scan will try again.", false)).ConfigureAwait(false);

        return saved ? await ReadAgainAsync(accountId, localOnly.Count, cancellationToken).ConfigureAwait(false) : rules;
    }

    private async Task<IReadOnlyList<SelectionRule>> ReadAgainAsync(AccountId accountId, int added, CancellationToken cancellationToken)
    {
        var message = $"{added} new local folders were included by default.";
        LogMessage.Information(logger, Location, message);

        return (await ruleReader.GetAsync(accountId, cancellationToken).ConfigureAwait(false)).OrThrow();
    }

    private IEnumerable<LocalRelativePath> Candidates(IDirectoryInfo folder, LocalRelativePath path, IReadOnlyList<SelectionRule> rules, CancellationToken cancellationToken)
    {
        if (path.Value is { Length: > 0 } && !Descends(path, rules))
            yield break;

        var children = Try.Run(() => folder.EnumerateDirectories().OrderBy(directory => directory.Name, StringComparer.Ordinal).ToList()).Match(found => found, _ => []);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var child in children)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!seen.Add(child.Name) || folderFilter.ShouldSkip(child) is not LocalFolderVisibility.Visible )
                continue;

            foreach (var candidate in LocalRelativePathFactory.Create(Join(path, child.Name)).Match(childPath => Visit(child, childPath, rules, cancellationToken), _ => []))
                yield return candidate;
        }
    }

    private IEnumerable<LocalRelativePath> Visit(IDirectoryInfo child, LocalRelativePath childPath, IReadOnlyList<SelectionRule> rules, CancellationToken cancellationToken)
    {
        var state = evaluator.Evaluate(childPath, rules);
        var hasRulesBelow = rules.Any(rule => SelectionPaths.IsStrictlyBelow(rule.Path, childPath));
        if (state is SelectionState.Uncovered && !hasRulesBelow)
            return [childPath];

        return state is SelectionState.PartiallyIncluded or SelectionState.Uncovered ? Candidates(child, childPath, rules, cancellationToken) : [];
    }

    private bool Descends(LocalRelativePath path, IReadOnlyList<SelectionRule> rules) => evaluator.Evaluate(path, rules) is SelectionState.PartiallyIncluded or SelectionState.Uncovered;

    private async Task<IReadOnlyList<LocalRelativePath>> LocalOnlyAsync(AccountId accountId, IReadOnlyList<LocalRelativePath> candidates, CancellationToken cancellationToken)
    {
        if (!(await remoteIndex.IsCompleteAsync(accountId, cancellationToken).ConfigureAwait(false)).OrThrow())
            return [];

        var cache = new Dictionary<Option<RemoteItemId>, IReadOnlyList<RemoteItemSnapshot>>();
        var localOnly = new List<LocalRelativePath>();
        foreach (var candidate in candidates)
        {
            if (!await ExistsRemotelyAsync(accountId, candidate, cache, cancellationToken).ConfigureAwait(false))
                localOnly.Add(candidate);
        }

        return localOnly;
    }

    private async Task<bool> ExistsRemotelyAsync(AccountId accountId, LocalRelativePath candidate, Dictionary<Option<RemoteItemId>, IReadOnlyList<RemoteItemSnapshot>> cache, CancellationToken cancellationToken)
    {
        var parent = Option.None<RemoteItemId>();
        foreach (var name in SelectionPaths.Segments(candidate))
        {
            if (!cache.TryGetValue(parent, out var children))
            {
                children = (await remoteIndex.GetChildrenAsync(accountId, parent, cancellationToken).ConfigureAwait(false)).OrThrow();
                cache[parent] = children;
            }

            var match = children.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
            if (match is null)
                return false;

            parent = Option.Some(match.Id);
        }

        return true;
    }
}
