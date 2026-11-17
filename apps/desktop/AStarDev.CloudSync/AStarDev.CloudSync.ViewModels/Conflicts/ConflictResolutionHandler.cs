using System.Collections.Concurrent;
using AStarDev.CloudSync.Accounts;
using AStarDev.CloudSync.Sync;
using AStarDev.FunctionalParadigm;
using AStarDev.LoggingExtensions;
using AStarDev.CloudSync.ViewModels.Common;
using Microsoft.Extensions.Logging;

namespace AStarDev.CloudSync.ViewModels.Conflicts;

internal sealed class ConflictResolutionHandler(IConflictResolver resolver, ILogger logger, Func<CancellationToken, Task> reload)
{
    private readonly ConcurrentDictionary<ConflictId, string> errorKeys = new();
    private readonly ConcurrentDictionary<ConflictId, byte> skipped = new();

    internal Option<string> ErrorKeyOf(ConflictId id) => errorKeys.TryGetValue(id, out var key) ? Option.Some(key) : Option.None<string>();

    internal bool IsSkipped(ConflictId id) => skipped.ContainsKey(id);

    internal void Prune(IReadOnlySet<ConflictId> open)
    {
        foreach (var id in errorKeys.Keys.Where(id => !open.Contains(id)))
            _ = errorKeys.TryRemove(id, out _);

        foreach (var id in skipped.Keys.Where(id => !open.Contains(id)))
            _ = skipped.TryRemove(id, out _);
    }

    internal async Task ResolveAsync(ConflictItemViewModel item, ConflictStrategy strategy, CancellationToken cancellationToken) =>
        await await UiResumingTry.RunTreatingTimeoutAsFailureAsync(() => resolver.ResolveAsync(item.AccountId, [item.Id], strategy, cancellationToken), result => result.Match(summary => SettleAsync(item, strategy, summary, cancellationToken), refusal => RefuseAsync(item, refusal)), exception => ThrewAsync(item, exception), cancellationToken);

    internal async Task<Option<string>> ResolveManyAsync(AccountId accountId, IReadOnlyList<ConflictItemViewModel> items, ConflictStrategy strategy, CancellationToken cancellationToken)
    {
        IReadOnlyList<ConflictId> ids = [.. items.Select(item => item.Id)];

        return await await UiResumingTry.RunTreatingTimeoutAsFailureAsync(() => resolver.ResolveAsync(accountId, ids, strategy, cancellationToken), result => result.Match(summary => Recorded(ids, strategy, summary), refusal => RefuseAllAsync(items, refusal)), exception => ThrewAllAsync(items, exception), cancellationToken);
    }

    private Task<Option<string>> Recorded(IReadOnlyList<ConflictId> ids, ConflictStrategy strategy, ResolutionSummary summary)
    {
        Record(ids, strategy, summary);

        return Task.FromResult(Option.None<string>());
    }

    private Task<Option<string>> RefuseAllAsync(IReadOnlyList<ConflictItemViewModel> items, ConflictRefusal refusal)
    {
        ConflictListLog.ResolveRefused(logger, refusal.GetType().Name);

        return Task.FromResult(RememberAll(items, ConflictTexts.RefusedKey(refusal)));
    }

    private Task<Option<string>> ThrewAllAsync(IReadOnlyList<ConflictItemViewModel> items, Exception exception)
    {
        ConflictListLog.ResolveThrew(logger, exception.ErrorType());

        return Task.FromResult(RememberAll(items, ConflictTexts.UnexpectedKey));
    }

    private Option<string> RememberAll(IReadOnlyList<ConflictItemViewModel> items, string key)
    {
        foreach (var item in items)
            Remember(item, key);

        return Option.Some(key);
    }

    private Task SettleAsync(ConflictItemViewModel item, ConflictStrategy strategy, ResolutionSummary summary, CancellationToken cancellationToken)
    {
        Record([item.Id], strategy, summary);

        return reload(cancellationToken);
    }

    private void Record(IReadOnlyList<ConflictId> ids, ConflictStrategy strategy, ResolutionSummary summary)
    {
        foreach (var id in ids)
            _ = errorKeys.TryRemove(id, out _);

        foreach (var failed in summary.Failed)
            errorKeys[failed.Id] = ConflictTexts.FailureKey(failed.Reason);

        if (strategy is not ConflictStrategy.Skip)
            return;

        foreach (var id in ids.Where(id => summary.Failed.All(failed => failed.Id != id)))
            _ = skipped.TryAdd(id, 0);
    }

    private Task RefuseAsync(ConflictItemViewModel item, ConflictRefusal refusal)
    {
        ConflictListLog.ResolveRefused(logger, refusal.GetType().Name);
        Remember(item, ConflictTexts.RefusedKey(refusal));

        return Task.CompletedTask;
    }

    private Task ThrewAsync(ConflictItemViewModel item, Exception exception)
    {
        ConflictListLog.ResolveThrew(logger, exception.ErrorType());
        Remember(item, ConflictTexts.UnexpectedKey);

        return Task.CompletedTask;
    }

    private void Remember(ConflictItemViewModel item, string key)
    {
        errorKeys[item.Id] = key;
        item.ShowError(key);
    }
}
